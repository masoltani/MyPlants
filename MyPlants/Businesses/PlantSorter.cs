using MyPlants.Models;

namespace MyPlants.Businesses;

public enum PlantSortOrder
{
    /// <summary>Les plantes à arroser en premier (les plus en retard d'abord).</summary>
    Urgency,

    /// <summary>Par nom, de A à Z.</summary>
    Name,

    /// <summary>Les plantes ajoutées ou plantées le plus récemment d'abord.</summary>
    DatePlanted
}

/// <summary>
/// Trie les plantes et calcule leur urgence d'arrosage.
/// Classe pure (aucun accès réseau ni stockage) : simple à lire et à tester.
/// </summary>
public static class PlantSorter
{
    public static IReadOnlyList<Plant> Sort(
        IEnumerable<Plant> plants,
        PlantSortOrder order,
        DateTime today)
    {
        var byName = StringComparer.CurrentCultureIgnoreCase;

        return order switch
        {
            PlantSortOrder.Name => plants
                .OrderBy(plant => plant.Name ?? string.Empty, byName)
                .ToList(),

            PlantSortOrder.DatePlanted => plants
                .OrderByDescending(plant => plant.DatePlanted ?? DateTime.MinValue)
                .ThenBy(plant => plant.Name ?? string.Empty, byName)
                .ToList(),

            _ => plants
                .OrderBy(plant => GetDaysUntilWatering(plant, today) ?? int.MaxValue)
                .ThenBy(plant => plant.Name ?? string.Empty, byName)
                .ToList()
        };
    }

    /// <summary>
    /// Nombre de jours avant le prochain arrosage : négatif si la plante est en retard,
    /// 0 si elle est à arroser aujourd'hui. Renvoie null quand on ne peut pas le savoir
    /// (plante archivée, pas d'intervalle, ni arrosage ni date de plantation).
    /// </summary>
    public static int? GetDaysUntilWatering(Plant plant, DateTime today)
    {
        if (plant.IsArchived || plant.WateringInterval is not { } interval)
            return null;

        // Une plante jamais arrosée est comptée à partir de sa date de plantation.
        var reference = plant.LastWatered ?? plant.DatePlanted;

        if (reference is null)
            return null;

        var nextWatering = reference.Value.Date.AddDays(interval);

        return (nextWatering - today.Date).Days;
    }
}