namespace MyPlants.Interfaces.IServices;

/// <summary>
/// Petites préférences d'affichage mémorisées sur l'appareil (ordre de tri, par exemple).
/// Si le stockage du navigateur est indisponible, elles ne sont simplement pas conservées.
/// </summary>
public interface IPreferencesService
{
    Task<string?> GetAsync(string key);

    Task SetAsync(string key, string value);
}