namespace MyPlants.Models;

/// <summary>Résultat d'une demande de création de compte.</summary>
public enum SignUpResult
{
    /// <summary>La demande a échoué (le motif est dans LastError).</summary>
    Failed,

    /// <summary>Compte créé : il reste à le faire valider.</summary>
    Created,

    /// <summary>Compte créé : l'adresse e-mail doit d'abord être confirmée, puis le compte validé.</summary>
    CreatedNeedsEmailConfirmation
}