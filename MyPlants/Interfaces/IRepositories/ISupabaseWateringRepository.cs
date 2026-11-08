using MyPlants.Models;

namespace MyPlants.Interfaces.IRepositories;

public interface ISupabaseWateringRepository
{
    Task<IReadOnlyList<Watering>> GetAllAsync();
    Task UpsertAsync(Watering watering);
    Task UpsertManyAsync(IReadOnlyList<Watering> waterings);
    Task<CloudChanges<Watering>> GetChangesAsync(DateTime? since);
}
