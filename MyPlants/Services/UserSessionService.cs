using Microsoft.JSInterop;

using MyPlants.Interfaces.IServices;

namespace MyPlants.Services;

/// <summary>
/// Gère la session utilisateur : invité ou compte Supabase.
/// Règle d'or : rien ici ne doit empêcher l'application de démarrer,
/// même sans réseau ou sans configuration Supabase.
/// </summary>
public sealed class UserSessionService : IUserSessionService, IAsyncDisposable
{
    private const string GuestProfileId = "guest";
    private const string AccountProfilePrefix = "account:";
    private const string AuthModulePath = "./js/supabaseAuth.js";

    private const string SessionAuthenticated = "authenticated";
    private const string SessionAnonymous = "anonymous";

    private readonly IJSRuntime _js;
    private readonly IConfiguration _configuration;

    private Task? _initialization;
    private IJSObjectReference? _authModule;

    public string? UserId { get; private set; }
    public string? Email { get; private set; }
    public string ProfileId => UserId is null ? GuestProfileId : $"{AccountProfilePrefix}{UserId}";
    public bool IsLoggedIn => UserId is not null;
    public string? LastError { get; private set; }

    public event Action? OnSessionChanged;

    public UserSessionService(IJSRuntime js, IConfiguration configuration)
    {
        _js = js;
        _configuration = configuration;
    }

    // Idempotent et sûr en cas d'appels simultanés : une seule initialisation a lieu.
    public Task InitializeAsync() => _initialization ??= InitializeCoreAsync();

    public async Task<bool> LoginAsync(string username, string password)
    {
        await InitializeAsync();
        LastError = null;

        try
        {
            var module = await GetAuthModuleAsync();

            if (module is null)
                return false;

            var result = await module.InvokeAsync<LoginResult>("signIn", username, password);

            if (!result.Success || string.IsNullOrWhiteSpace(result.UserId))
            {
                LastError = result.Error ?? "La connexion a échoué.";
                return false;
            }

            await SetUserAsync(result.UserId, result.Email);
            return true;
        }
        catch (JSException ex)
        {
            LastError = "Impossible de contacter Supabase. Vérifie ta connexion Internet.";
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        await InitializeAsync();
        LastError = null;

        try
        {
            var module = await GetAuthModuleAsync();

            if (module is not null)
                await module.InvokeVoidAsync("signOut");
        }
        catch (JSException)
        {
            // Une panne réseau ne doit jamais empêcher de se déconnecter localement.
        }

        await SetUserAsync(null, null);
    }

    public async ValueTask DisposeAsync()
    {
        if (_authModule is null)
            return;

        try
        {
            await _authModule.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // Le runtime JavaScript peut déjà être déconnecté.
        }
    }

    private async Task InitializeCoreAsync()
    {
        // 1. Restauration locale : fonctionne toujours, même hors ligne.
        UserId = await ReadLastUserIdAsync();

        // 2. Confirmation auprès de Supabase, seulement si un compte était connecté.
        //    Un invité ne déclenche aucun accès réseau au démarrage.
        if (UserId is not null)
            await ConfirmSessionAsync();

        await SaveProfileAsync();
        OnSessionChanged?.Invoke();
    }

    private async Task<string?> ReadLastUserIdAsync()
    {
        try
        {
            var profileId = await _js.InvokeAsync<string?>("myPlantsStorage.getProfile");

            return profileId is not null
                && profileId.StartsWith(AccountProfilePrefix, StringComparison.Ordinal)
                    ? profileId[AccountProfilePrefix.Length..]
                    : null;
        }
        catch (JSException)
        {
            return null;
        }
    }

    private async Task ConfirmSessionAsync()
    {
        try
        {
            var module = await GetAuthModuleAsync();

            if (module is null)
                return;

            var session = await module.InvokeAsync<SessionInfo>("getSession");

            switch (session.Status)
            {
                case SessionAuthenticated:
                    UserId = session.UserId;
                    Email = session.Email;
                    break;

                case SessionAnonymous:
                    // Le serveur confirme qu'il n'y a plus de session.
                    UserId = null;
                    Email = null;
                    break;

                    // Statut "unavailable" : on conserve le compte restauré.
            }
        }
        catch (JSException)
        {
            // Hors ligne : on conserve le compte restauré et ses données locales.
        }
    }

    // Charge Supabase à la demande ; renvoie null si la configuration est absente.
    private async Task<IJSObjectReference?> GetAuthModuleAsync()
    {
        if (_authModule is not null)
            return _authModule;

        var url = _configuration["Supabase:Url"];
        var key = _configuration["Supabase:PublishableKey"];

        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(key))
        {
            LastError = "La configuration Supabase est manquante.";
            return null;
        }

        var module = await _js.InvokeAsync<IJSObjectReference>("import", AuthModulePath);
        await module.InvokeVoidAsync("initialize", url, key);

        _authModule = module;
        return module;
    }

    private async Task SetUserAsync(string? userId, string? email)
    {
        UserId = userId;
        Email = email;

        await SaveProfileAsync();
        OnSessionChanged?.Invoke();
    }

    private async Task SaveProfileAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("myPlantsStorage.setProfile", ProfileId);
        }
        catch (JSException)
        {
            // Le profil sera réécrit à la prochaine connexion ou déconnexion.
        }
    }

    private sealed class SessionInfo
    {
        public string? Status { get; set; }
        public string? UserId { get; set; }
        public string? Email { get; set; }
    }

    private sealed class LoginResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? UserId { get; set; }
        public string? Email { get; set; }
    }
}