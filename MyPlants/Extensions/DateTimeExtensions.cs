using MyPlants.Businesses;
using MyPlants.Services;

namespace MyPlants.Extensions;

public static class DateTimeExtensions
{
    /// <summary>
    /// Date courte selon les réglages de l'utilisateur (calendrier et format), quelle que soit la langue.
    /// </summary>
    public static string ToDisplayDate(this DateTime date) =>
        DateDisplayFormatter.Format(date, DateDisplayService.Current);
}