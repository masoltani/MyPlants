using MyPlants.Models;

namespace MyPlants.Interfaces.IRepositories;

public interface ISupabaseWateringRepository
{
    Task<IReadOnlyList<Watering>> GetAllAsync();
    Task UpsertAsync(Watering watering);
}
