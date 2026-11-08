namespace MyPlants.Models;

/// <summary>Lignes du cloud modifiées depuis un instant donné, et le nouveau point de reprise correspondant.</summary>
/// <param name="Items">Lignes reçues.</param>
/// <param name="Cursor">
/// Plus récente heure serveur (updated_at) parmi les lignes reçues ; null si aucune ligne n'a été reçue.
/// C'est l'heure du serveur, jamais celle de l'appareil : une horloge d'appareil décalée ne peut donc pas faire manquer de lignes.
/// </param>
public sealed record CloudChanges<T>(IReadOnlyList<T> Items, DateTime? Cursor);