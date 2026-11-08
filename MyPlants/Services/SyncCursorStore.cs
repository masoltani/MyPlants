using System.Globalization;

using MyPlants.Interfaces.IServices;

namespace MyPlants.Services;

/// <summary>
/// Mémorise, pour chaque profil de cet appareil, jusqu'où les données du cloud ont déjà été récupérées.
/// La synchronisation ne télécharge ensuite que ce qui a changé depuis ce point.
/// </summary>
public sealed class SyncCursorStore
{
    public const string Plants = "plants";
    public const string Waterings = "waterings";

    private readonly IPreferencesService _preferences;
    private readonly IUserSessionService _session;

    public SyncCursorStore(IPreferencesService preferences, IUserSessionService session)
    {
        _preferences = preferences;
        _session = session;
    }

    public async Task<DateTime?> GetAsync(string table)
    {
        var saved = await _preferences.GetAsync(Key(table));

        return DateTime.TryParse(saved, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var cursor)
            ? cursor.ToUniversalTime()
            : null;
    }

    public Task SetAsync(string table, DateTime cursor) =>
        _preferences.SetAsync(Key(table), cursor.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));

    // Oublie les points de reprise du profil courant : la prochaine synchronisation relit tout.
    public async Task ResetAsync()
    {
        await _preferences.SetAsync(Key(Plants), string.Empty);
        await _preferences.SetAsync(Key(Waterings), string.Empty);
    }

    // Un point de reprise par table et par profil (un autre compte sur le même appareil repart de zéro).
    private string Key(string table) => $"sync-cursor-{table}-{_session.ProfileId}";
}