using MyPlants.Interfaces.IServices;
using MyPlants.Models;

namespace MyPlants.Services;

/// <summary>
/// Calendrier et format de date choisis par l'utilisateur (Réglages), mémorisés sur l'appareil.
/// </summary>
public sealed class DateDisplayService
{
    private const string CALENDAR_KEY = "date-calendar";
    private const string PATTERN_KEY = "date-pattern";
    private const string CULTURE_ARABIC = "ar";

    private readonly IPreferencesService _preferences;
    private readonly LocalizationService _localization;

    // Lu par l'extension ToDisplayDate(), qui n'a pas accès à l'injection de dépendances.
    public static DateDisplaySettings Current { get; private set; } = DateDisplaySettings.Default;

    public DateDisplayService(IPreferencesService preferences, LocalizationService localization)
    {
        _preferences = preferences;
        _localization = localization;
    }

    public DateDisplaySettings Settings => Current;

    // Ne doit jamais empêcher l'application de démarrer : en cas de souci, on garde les valeurs par défaut.
    public async Task InitializeAsync()
    {
        try
        {
            var savedCalendar = await _preferences.GetAsync(CALENDAR_KEY);
            var savedPattern = await _preferences.GetAsync(PATTERN_KEY);

            var calendar = Enum.TryParse<DateCalendar>(savedCalendar, out var parsed) && Enum.IsDefined(parsed)
                ? parsed
                : DefaultCalendarForLanguage();

            var pattern = savedPattern is not null && DateDisplaySettings.SupportedPatterns.Contains(savedPattern)
                ? savedPattern
                : DateDisplaySettings.Default.Pattern;

            Current = new DateDisplaySettings(calendar, pattern);
        }
        catch (Exception)
        {
            Current = DateDisplaySettings.Default;
        }
    }

    public async Task UpdateAsync(DateDisplaySettings settings)
    {
        Current = settings;

        await _preferences.SetAsync(CALENDAR_KEY, settings.Calendar.ToString());
        await _preferences.SetAsync(PATTERN_KEY, settings.Pattern);
    }

    // Tant que rien n'est choisi : hégirien pour l'arabe, grégorien sinon.
    private DateCalendar DefaultCalendarForLanguage() =>
        _localization.CurrentCulture.TwoLetterISOLanguageName == CULTURE_ARABIC
            ? DateCalendar.Hijri
            : DateCalendar.Gregorian;
}