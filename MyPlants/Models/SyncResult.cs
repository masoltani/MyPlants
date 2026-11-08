namespace MyPlants.Models;

/// <summary>Ce qu'a changé une synchronisation sur l'appareil.</summary>
/// <param name="LocalDataChanged">Vrai si des plantes ou des arrosages venus du cloud ont été enregistrés localement.</param>
public sealed record SyncResult(bool LocalDataChanged);