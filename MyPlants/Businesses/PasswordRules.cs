using MyPlants.Resources;

namespace MyPlants.Businesses;

/// <summary>
/// Règle de saisie d'un mot de passe, partagée par la création de compte et la réinitialisation.
/// Supabase peut imposer une règle plus stricte (Authentication > Sign In / Providers > Email) :
/// son refus est alors traduit en message par UserSessionService.
/// </summary>
public static class PasswordRules
{
    public const int MinLength = 8;

    /// <summary>Renvoie le message d'erreur à afficher, ou null si la saisie est correcte.</summary>
    public static string? Validate(string password, string confirmation)
    {
        if (password.Length < MinLength)
            return string.Format(SharedResource.Auth_Error_PasswordTooShort, MinLength);

        if (password != confirmation)
            return SharedResource.Auth_Error_PasswordsDoNotMatch;

        return null;
    }
}