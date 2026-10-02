using MyPlants.Interfaces.IRepositories;
using MyPlants.Interfaces.IServices;
using MyPlants.Models;

namespace MyPlants.Services;

public sealed class PlantService : IPlantService
{
    private readonly IPlantRepository _repository;

    public PlantService(IPlantRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<Plant>> GetPlantsAsync()
    {
        return _repository.GetAllAsync();
    }

    public Task<Plant?> GetPlantAsync(Guid id)
    {
        return _repository.GetByIdAsync(id);
    }

    public Task AddPlantAsync(Plant plant)
    {
        if (plant.Id == Guid.Empty)
        {
            plant.Id = Guid.NewGuid();
        }

        return _repository.AddAsync(plant);
    }

    public Task UpdatePlantAsync(Plant plant)
    {
        return _repository.UpdateAsync(plant);
    }

    public async Task ArchivePlantAsync(Guid id)
    {
        var plant = await _repository.GetByIdAsync(id);

        if (plant is null)
        {
            return;
        }

        plant.IsArchived = true;

        await _repository.UpdateAsync(plant);
    }
    
    public async Task<bool> WaterPlantAsync(Guid id)
    {
        var plant = await _repository.GetByIdAsync(id);

        if (plant is null || plant.IsArchived)
            return false;

        var watering = new Watering
        {
            PlantId = plant.Id,
            Date = DateTime.Now
        };

        await _repository.AddWateringAsync(watering);

        plant.LastWatered = watering.Date;

        await _repository.UpdateAsync(plant);

        return true;
    }

    public Task<IReadOnlyList<Watering>> GetWateringHistoryAsync(Guid plantId)
    {
        return _repository.GetWateringHistoryAsync(plantId);
    }
}
