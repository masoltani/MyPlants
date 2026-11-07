using System.Globalization;

using MyPlants.Models;

namespace MyPlants.Businesses;

/// <summary>
/// Met une date en forme selon le calendrier et le format choisis. Aucune dépendance à la culture.
/// </summary>
public static class DateDisplayFormatter
{
    private const string FALLBACK_PATTERN = "yyyy-MM-dd";

    public static readonly UmAlQuraCalendar HijriCalendar = new();

    public static string Format(DateTime date, DateDisplaySettings settings)
    {
        int year, month, day;

        try
        {
            if (settings.Calendar == DateCalendar.Hijri)
            {
                year = HijriCalendar.GetYear(date);
                month = HijriCalendar.GetMonth(date);
                day = HijriCalendar.GetDayOfMonth(date);
            }
            else
            {
                (year, month, day) = (date.Year, date.Month, date.Day);
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            // Date hors de la plage du calendrier hégirien (environ 1900-2077) : format international.
            return date.ToString(FALLBACK_PATTERN, CultureInfo.InvariantCulture);
        }

        return settings.Pattern
            .Replace("yyyy", year.ToString("0000", CultureInfo.InvariantCulture))
            .Replace("MM", month.ToString("00", CultureInfo.InvariantCulture))
            .Replace("dd", day.ToString("00", CultureInfo.InvariantCulture));
    }
}