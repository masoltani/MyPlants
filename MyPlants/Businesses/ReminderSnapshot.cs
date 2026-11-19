using System.Globalization;

using MyPlants.Models;

namespace MyPlants.Businesses;

/// <summary>
/// Prépare, pour le service worker, la liste des prochaines dates d'arrosage.
/// Le service worker n'a plus qu'à compter les dates arrivées : la règle de calcul reste ici, en un seul endroit
/// (PlantSorter.GetDaysUntilWatering). Classe pure, simple à tester.
/// </summary>
public static class ReminderSnapshot
{
    private const string DateFormat = "yyyy-MM-dd";

    /// <summary>Une date par plante dont l'échéance est connue (en retard, aujourd'hui ou à venir), au format aaaa-mm-jj.</summary>
    public static IReadOnlyList<string> BuildDates(IEnumerable<Plant> plants, DateTime today)
    {
        return plants
            .Select(plant => PlantSorter.GetDaysUntilWatering(plant, today))
            .Where(days => days is not null)
            .Select(days => today.Date.AddDays(days!.Value).ToString(DateFormat, CultureInfo.InvariantCulture))
            .Order(StringComparer.Ordinal)
            .ToList();
    }
}