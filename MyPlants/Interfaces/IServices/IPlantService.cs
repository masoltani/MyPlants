using MyPlants.Models;

namespace MyPlants.Interfaces.IServices;

public interface IPlantService
{
    Task<IReadOnlyList<Plant>> GetPlantsAsync();

    Task<Plant?> GetPlantAsync(Guid id);

    Task AddPlantAsync(Plant plant);

    Task UpdatePlantAsync(Plant plant);

    Task ArchivePlantAsync(Guid id);

    Task<bool> WaterPlantAsync(Guid id);

    Task<IReadOnlyList<Watering>> GetWateringHistoryAsync(Guid plantId);
}
