using MyPlants.Models;

namespace MyPlants.Interfaces.IRepositories;

public interface IPlantRepository
{
    Task<IReadOnlyList<Plant>> GetAllAsync();
    Task<Plant?> GetByIdAsync(Guid id);
    Task AddAsync(Plant plant);
    Task UpdateAsync(Plant plant);
    Task DeleteAsync(Guid id);
    Task<IReadOnlyList<Plant>> GetUnsyncedAsync();
    Task MarkAsSyncedAsync(Guid id, DateTime? expectedUpdatedAt = null);
    Task UpsertFromCloudAsync(Plant plant, DateTime? expectedLocalUpdatedAt = null);
    Task<IReadOnlyList<Watering>> GetWateringHistoryAsync(Guid plantId);
    Task AddWateringAsync(Watering watering);
    Task<int> ImportGuestPlantsAsync(string targetProfileId);
    Task<bool> HasGuestPlantsAsync();
}
