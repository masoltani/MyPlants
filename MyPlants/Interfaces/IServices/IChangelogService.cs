using MyPlants.Models;

namespace MyPlants.Interfaces.IServices;

public interface IChangelogService
{
    /// <summary>
    /// Journal des versions, de la plus récente à la plus ancienne.
    /// Renvoie une liste vide si le journal est indisponible : il ne doit jamais bloquer la page.
    /// </summary>
    Task<IReadOnlyList<ReleaseNote>> GetReleaseNotesAsync();
}