using MyPlants.Models;

namespace MyPlants.Interfaces.IRepositories;

/// <summary>Accès local (IndexedDB) aux arrosages, pour la synchronisation.</summary>
public interface ILocalWateringRepository
{
    /// <summary>Tous les arrosages du profil courant, avec leur état de synchronisation.</summary>
    Task<IReadOnlyList<Watering>> GetAllAsync();

    Task MarkAsSyncedAsync(IEnumerable<Guid> ids);

    /// <summary>Enregistre des arrosages venant du cloud, déjà considérés comme synchronisés.</summary>
    Task UpsertManyFromCloudAsync(IEnumerable<Watering> waterings);
}