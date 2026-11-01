using MyPlants.Models;

namespace MyPlants.Interfaces.IRepositories;

public interface ISupabasePlantRepository
{
    Task<IReadOnlyList<Plant>> GetAllAsync();
    Task UpsertAsync(Plant plant);
}
