using MyPlants.Models;

namespace MyPlants.Interfaces.IServices;

public interface IUserSessionService
{
    Task InitializeAsync();
    Task<bool> LoginAsync(string username, string password);
    Task LogoutAsync();
    bool IsLoggedIn { get; }
    string? UserId { get; }
    string? Email { get; }
    string? ProfileId { get; }
    string? LastError { get; }
    event Action? OnSessionChanged;
    AuthLinkStatus AuthLink { get; }
    Task<bool> RequestPasswordResetAsync(string email);
    Task<bool> UpdatePasswordAsync(string newPassword);
}
