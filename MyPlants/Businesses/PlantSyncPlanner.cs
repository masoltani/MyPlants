using MyPlants.Models;

namespace MyPlants.Businesses;

/// <summary>Résultat d'une comparaison entre les plantes locales et celles du cloud.</summary>
public sealed record PlantSyncPlan(
    IReadOnlyList<Plant> ToPush,
    IReadOnlyList<Plant> ToPull);

/// <summary>
/// Décide, plante par plante, ce qu'il faut envoyer au cloud et ce qu'il faut récupérer.
/// Classe pure (aucun accès réseau ni stockage) : simple à lire et à tester.
///
/// Règle : la version modifiée le plus récemment l'emporte (UpdatedAt).
/// </summary>
public static class PlantSyncPlanner
{
    public static PlantSyncPlan Plan(
        IEnumerable<Plant> localPlants,
        IEnumerable<Plant> cloudPlants)
    {
        var localById = localPlants.ToDictionary(plant => plant.Id);
        var cloudById = cloudPlants.ToDictionary(plant => plant.Id);

        var toPush = new List<Plant>();
        var toPull = new List<Plant>();

        foreach (var local in localById.Values)
        {
            if (!cloudById.TryGetValue(local.Id, out var cloud))
            {
                // Inconnue du cloud : on l'envoie si elle a des changements en attente.
                if (!local.IsSynced)
                    toPush.Add(local);

                continue;
            }

            if (IsNewer(cloud, local))
                toPull.Add(cloud);
            else if (!local.IsSynced)
                toPush.Add(local);
        }

        foreach (var cloud in cloudById.Values)
        {
            // Plante créée sur un autre appareil. Inutile de récupérer une plante
            // déjà supprimée et que ce téléphone ne connaît pas.
            if (!localById.ContainsKey(cloud.Id) && cloud.DeletedAt is null)
                toPull.Add(cloud);
        }

        return new PlantSyncPlan(toPush, toPull);
    }

    private static bool IsNewer(Plant candidate, Plant reference)
    {
        return candidate.UpdatedAt.ToUniversalTime() > reference.UpdatedAt.ToUniversalTime();
    }
}