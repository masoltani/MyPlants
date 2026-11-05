namespace MyPlants.Models;

/// <summary>Indique si l'utilisateur est arrivé dans l'application depuis un lien reçu par e-mail.</summary>
public enum AuthLinkStatus
{
    /// <summary>Visite normale.</summary>
    None,

    /// <summary>Lien valide de réinitialisation de mot de passe : un nouveau mot de passe peut être choisi.</summary>
    Recovery,

    /// <summary>Lien refusé (expiré ou déjà utilisé) : il faut en demander un nouveau.</summary>
    Expired
}