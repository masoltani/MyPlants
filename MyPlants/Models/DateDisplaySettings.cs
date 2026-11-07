namespace MyPlants.Models;

public enum DateCalendar
{
    Gregorian,
    Hijri
}

/// <summary>
/// Choix d'affichage des dates, indépendants de la langue de l'application.
/// </summary>
/// <param name="Calendar">Calendrier utilisé pour afficher les dates.</param>
/// <param name="Pattern">Format numérique : combinaison de yyyy, MM et dd.</param>
public sealed record DateDisplaySettings(DateCalendar Calendar, string Pattern)
{
    public static readonly DateDisplaySettings Default = new(DateCalendar.Gregorian, "dd/MM/yyyy");

    // Uniquement des formats numériques : les noms de mois hégiriens seraient faux
    // avec les cultures française ou anglaise.
    public static readonly IReadOnlyList<string> SupportedPatterns =
        ["dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "dd.MM.yyyy"];
}