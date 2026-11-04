using MyPlants.Models;

namespace MyPlants.Businesses;

/// <summary>Résultat d'une comparaison entre les arrosages locaux et ceux du cloud.</summary>
public sealed record WateringSyncPlan(
    IReadOnlyList<Watering> ToPush,
    IReadOnlyList<Watering> ToPull,
    IReadOnlyList<Watering> ToMarkSynced);

/// <summary>
/// Décide quels arrosages envoyer au cloud et lesquels récupérer.
/// Classe pure (aucun accès réseau ni stockage), comme <see cref="PlantSyncPlanner"/>.
///
/// Un arrosage n'est jamais modifié après sa création : il suffit de comparer
/// les identifiants, il n'y a aucun conflit à résoudre.
/// </summary>
public static class WateringSyncPlanner
{
    public static WateringSyncPlan Plan(
        IEnumerable<Watering> localWaterings,
        IEnumerable<Watering> cloudWaterings,
        IReadOnlySet<Guid> cloudPlantIds)
    {
        var local = localWaterings.ToList();
        var cloud = cloudWaterings.ToList();

        var localIds = local.Select(watering => watering.Id).ToHashSet();
        var cloudIds = cloud.Select(watering => watering.Id).ToHashSet();

        var toPush = new List<Watering>();
        var toMarkSynced = new List<Watering>();

        foreach (var watering in local.Where(watering => !watering.IsSynced))
        {
            if (cloudIds.Contains(watering.Id))
            {
                // Déjà présent dans le cloud (envoi réussi mais non enregistré localement).
                toMarkSynced.Add(watering);
            }
            else if (cloudPlantIds.Contains(watering.PlantId))
            {
                toPush.Add(watering);
            }

            // Sinon, la plante n'existe pas dans le cloud (par exemple, supprimée avant
            // d'avoir été envoyée) : l'arrosage reste local, il serait refusé par le serveur.
        }

        var toPull = cloud
            .Where(watering => !localIds.Contains(watering.Id))
            .ToList();

        return new WateringSyncPlan(toPush, toPull, toMarkSynced);
    }
}