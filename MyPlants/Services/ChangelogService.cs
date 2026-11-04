using System.Net.Http.Json;
using System.Text.Json;

using MyPlants.Interfaces.IServices;
using MyPlants.Models;

namespace MyPlants.Services;

/// <summary>Lit le journal des versions dans wwwroot/data/changelog.json.</summary>
public sealed class ChangelogService : IChangelogService
{
    private const string ChangelogPath = "data/changelog.json";

    private readonly HttpClient _httpClient;
    private IReadOnlyList<ReleaseNote>? _releaseNotes;

    public ChangelogService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<ReleaseNote>> GetReleaseNotesAsync()
    {
        if (_releaseNotes is not null)
            return _releaseNotes;

        try
        {
            _releaseNotes = await _httpClient.GetFromJsonAsync<List<ReleaseNote>>(ChangelogPath) ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or NotSupportedException)
        {
            // Fichier introuvable ou illisible : pas de journal, mais la page reste utilisable.
            // Rien n'est mis en cache, la lecture sera retentée à la prochaine visite.
            return [];
        }

        return _releaseNotes;
    }
}