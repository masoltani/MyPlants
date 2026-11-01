using Microsoft.JSInterop;

using MyPlants.Interfaces.IServices;

using System.Reflection;

namespace MyPlants.Services;

public class UserSessionService : IUserSessionService
{
    private readonly IJSRuntime _js;
    private readonly IConfiguration _configuration;

    private IJSObjectReference? _jsModule;
    private bool? _isInitialized;

    public string? UserId { get; private set; }
    public string? Email { get; private set; }
    public string ProfileId => UserId is null ? "guest" : $"account:{UserId}";
    public bool IsLoggedIn => UserId is not null;
    public string? LastError { get; private set; }

    public event Action? OnSessionChanged;

    public UserSessionService(IJSRuntime js, IConfiguration configuration)
    {
        _js = js;
        _configuration = configuration;
    }

    public async Task InitializeAsync()
    {
        if(_isInitialized is true)
            return;

        var url = _configuration["Supabase:Url"];
        var key = _configuration["Supabase:PublishableKey"];

        if(string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(key))
        {
            LastError = "Supabase configuration is missing.";
            return;
        }

        _jsModule ??= await _js.InvokeAsync<IJSObjectReference>("import", "./js/supabaseAuth.js");
        await _jsModule.InvokeVoidAsync("initialize", url, key);

        var session = await _jsModule.InvokeAsync<SessionInfo>("getSession");

        UserId = session?.UserId;
        Email = session?.Email;

        await UpdateLocalProfileAsync();

        _isInitialized = true;
        OnSessionChanged?.Invoke();
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        await InitializeAsync();
        LastError = null;
        try
        {
            var result = await _jsModule!.InvokeAsync<LoginResult>("signIn", username, password);
            if(!result.Success || string.IsNullOrWhiteSpace(result.UserId))
            {
                LastError = result.Error ?? "La connexion a échoué.";
                return false;
            }
            UserId = result.UserId;
            Email = result.Email;
            await UpdateLocalProfileAsync();
            OnSessionChanged?.Invoke();
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
        await _jsModule!.InvokeVoidAsync("signOut");
        UserId = null;
        Email = null;
        LastError = null;
        await UpdateLocalProfileAsync();
        OnSessionChanged?.Invoke();
    }

    private async Task UpdateLocalProfileAsync()
    {
        await _js.InvokeVoidAsync(
            "myPlantsStorage.setProfile",
            ProfileId);
    }

    public async ValueTask DisposeAsync()
    {
        if (_jsModule is not null)
        {
            await _jsModule.DisposeAsync();
        }
    }

    private sealed class SessionInfo
    {
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
