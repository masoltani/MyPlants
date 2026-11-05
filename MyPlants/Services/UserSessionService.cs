using Microsoft.JSInterop;

using MyPlants.Interfaces.IServices;
using MyPlants.Models;
using MyPlants.Resources;

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

    private const string LinkRecovery = "recovery";
    private const string LinkExpired = "expired";

    private readonly IJSRuntime _js;
    private readonly IConfiguration _configuration;

    private Task? _initialization;
    private IJSObjectReference? _authModule;

    public string? UserId { get; private set; }
    public string? Email { get; private set; }
    public string ProfileId => UserId is null ? GuestProfileId : $"{AccountProfilePrefix}{UserId}";
    public bool IsLoggedIn => UserId is not null;
    public string? LastError { get; private set; }

    /// <summary>Renseigné si la page a été ouverte depuis un lien de réinitialisation reçu par e-mail.</summary>
    public AuthLinkStatus AuthLink { get; private set; } = AuthLinkStatus.None;

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

            var result = await module.InvokeAsync<AuthResult>("signIn", username, password);

            if (!result.Success || string.IsNullOrWhiteSpace(result.UserId))
            {
                LastError = result.Error ?? "La connexion a échoué.";
                return false;
            }

            await SetUserAsync(result.UserId, result.Email);
            return true;
        }
        catch (JSException)
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

    /// <summary>Demande l'envoi d'un e-mail contenant un lien pour choisir un nouveau mot de passe.</summary>
    public async Task<bool> RequestPasswordResetAsync(string email)
    {
        await InitializeAsync();
        LastError = null;

        var result = await InvokeAuthAsync("requestPasswordReset", email);

        return result.Success;
    }

    /// <summary>Enregistre le nouveau mot de passe de l'utilisateur arrivé par un lien de réinitialisation.</summary>
    public async Task<bool> UpdatePasswordAsync(string newPassword)
    {
        await InitializeAsync();
        LastError = null;

        var result = await InvokeAuthAsync("updatePassword", newPassword);

        if (!result.Success)
            return false;

        AuthLink = AuthLinkStatus.None;

        if (!string.IsNullOrWhiteSpace(result.UserId))
            await SetUserAsync(result.UserId, result.Email);

        return true;
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
        // L'adresse de la page doit être lue en premier : le client Supabase efface
        // ensuite les informations qu'elle contient.
        AuthLink = await ReadAuthLinkAsync();

        // 1. Restauration locale : fonctionne toujours, même hors ligne.
        UserId = await ReadLastUserIdAsync();

        // 2. Confirmation auprès de Supabase, seulement si un compte était connecté, ou si
        //    un lien de réinitialisation est à traiter (même pour un invité : le client
        //    Supabase en tire alors une session). Un invité ordinaire ne déclenche aucun
        //    accès réseau au démarrage.
        if (UserId is not null || AuthLink == AuthLinkStatus.Recovery)
            await ConfirmSessionAsync();

        await SaveProfileAsync();
        OnSessionChanged?.Invoke();
    }

    private async Task<AuthLinkStatus> ReadAuthLinkAsync()
    {
        try
        {
            var status = await _js.InvokeAsync<string?>("myPlantsAuthLink.getStatus");

            return status switch
            {
                LinkRecovery => AuthLinkStatus.Recovery,
                LinkExpired => AuthLinkStatus.Expired,
                _ => AuthLinkStatus.None
            };
        }
        catch (JSException)
        {
            return AuthLinkStatus.None;
        }
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

    // Appelle une fonction de supabaseAuth.js. En cas d'échec, LastError reçoit un message
    // déjà traduit, prêt à être affiché.
    private async Task<AuthResult> InvokeAuthAsync(string function, params object?[] arguments)
    {
        try
        {
            var module = await GetAuthModuleAsync();

            if (module is null)
                return new AuthResult();

            var result = await module.InvokeAsync<AuthResult>(function, arguments);

            if (!result.Success)
                LastError = ToMessage(result);

            return result;
        }
        catch (JSException)
        {
            LastError = SharedResource.Auth_Error_Network;
            return new AuthResult();
        }
    }

    private static string ToMessage(AuthResult result)
    {
        return result.Code switch
        {
            "network" => SharedResource.Auth_Error_Network,
            "weak_password" => SharedResource.Auth_Error_WeakPassword,
            "same_password" => SharedResource.Auth_Error_SamePassword,
            "over_email_send_rate_limit" or "over_request_rate_limit" => SharedResource.Auth_Error_RateLimit,
            "no_session" => SharedResource.Auth_LinkInvalid,
            _ => SharedResource.Error_Generic
        };
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

    // Résultat commun des opérations d'authentification (voir supabaseAuth.js).
    private sealed class AuthResult
    {
        public bool Success { get; set; }
        public string? Code { get; set; }
        public string? Error { get; set; }
        public string? UserId { get; set; }
        public string? Email { get; set; }
    }
}