using MyPlants.Interfaces.IBusinesses;
using MyPlants.Interfaces.IRepositories;
using MyPlants.Interfaces.IServices;
using MyPlants.Models;

namespace MyPlants.Services;

public sealed class PlantService : IPlantService
{
    private readonly IPlantRepository _repository;
    private readonly IPlantBusiness _business;
    private readonly IUserSessionService _userSessionService;
    private readonly IDueCountRefresher _dueCount;

    public PlantService(
        IPlantRepository repository,
        IPlantBusiness business,
        IUserSessionService userSessionService,
        IDueCountRefresher dueCount)
    {
        _repository = repository;
        _business = business;
        _userSessionService = userSessionService;
        _dueCount = dueCount;
    }

    public Task<IReadOnlyList<Plant>> GetPlantsAsync()
    {
        return _repository.GetAllAsync();
    }

    public Task<Plant?> GetPlantAsync(Guid id)
    {
        return _repository.GetByIdAsync(id);
    }

    public async Task AddPlantAsync(Plant plant)
    {
        if (plant.Id == Guid.Empty)
        {
            plant.Id = Guid.NewGuid();
        }

        await _repository.AddAsync(plant);

        _dueCount.RequestRefresh();
    }

    public async Task UpdatePlantAsync(Plant plant)
    {
        await _repository.UpdateAsync(plant);

        _dueCount.RequestRefresh();
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

        _dueCount.RequestRefresh();
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

        _dueCount.RequestRefresh();

        return true;
    }

    public async Task<bool> WaterPlantAsync(Guid id, DateTime date)
    {
        var plant = await _repository.GetByIdAsync(id);
        if (plant is null || plant.IsArchived)
            return false;
        var watering = new Watering
        {
            PlantId = plant.Id,
            Date = date
        };
        await _repository.AddWateringAsync(watering);
        _business.SetLastWateringDate(plant, date);
        await _repository.UpdateAsync(plant);

        _dueCount.RequestRefresh();

        return true;
    }

    public Task<IReadOnlyList<Watering>> GetWateringHistoryAsync(Guid plantId)
    {
        return _repository.GetWateringHistoryAsync(plantId);
    }

    public async Task<int> ImportGuestPlantsAsync()
    {
        await _userSessionService.InitializeAsync();

        if (!_userSessionService.IsLoggedIn)
        {
            throw new InvalidOperationException(
                "Il faut être connecté pour importer les plantes invitées.");
        }

        var imported = await _repository.ImportGuestPlantsAsync(_userSessionService.ProfileId);

        if (imported > 0)
            _dueCount.RequestRefresh();

        return imported;
    }

    public async Task<bool> HasGuestPlantsAsync()
    {
        await _userSessionService.InitializeAsync();

        if (!_userSessionService.IsLoggedIn)
            return false;

        return await _repository.HasGuestPlantsAsync();
    }
}