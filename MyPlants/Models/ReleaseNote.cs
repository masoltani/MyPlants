namespace MyPlants.Models;

/// <summary>Ce que apporte une version de l'application (une entrée du journal des versions).</summary>
public sealed class ReleaseNote
{
    private const string FallbackLanguage = "en";

    public string Version { get; set; } = string.Empty;

    public DateOnly? Date { get; set; }

    /// <summary>Nouveautés par langue : "fr", "en", "ar"…</summary>
    public Dictionary<string, List<string>> Changes { get; set; } = new();

    /// <summary>
    /// Nouveautés dans la langue demandée. Si elle n'est pas disponible,
    /// on se rabat sur l'anglais, puis sur la première langue renseignée.
    /// </summary>
    public IReadOnlyList<string> GetChanges(string language)
    {
        if (Changes.TryGetValue(language, out var changes))
            return changes;

        if (Changes.TryGetValue(FallbackLanguage, out var fallback))
            return fallback;

        return Changes.Values.FirstOrDefault() ?? [];
    }
}