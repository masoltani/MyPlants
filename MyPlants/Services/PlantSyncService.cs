using MyPlants.Businesses;
using MyPlants.Interfaces.IRepositories;
using MyPlants.Interfaces.IServices;
using MyPlants.Models;

namespace MyPlants.Services;

public sealed class PlantSyncService : IPlantSyncService
{
    private readonly IPlantRepository _localRepository;
    private readonly ISupabasePlantRepository _cloudRepository;
    private readonly IUserSessionService _userSessionService;

    public PlantSyncService(
        IPlantRepository localRepository,
        ISupabasePlantRepository cloudRepository,
        IUserSessionService userSessionService)
    {
        _localRepository = localRepository;
        _cloudRepository = cloudRepository;
        _userSessionService = userSessionService;
    }

    public async Task SyncAsync()
    {
        await _userSessionService.InitializeAsync();

        if (!_userSessionService.IsLoggedIn)
        {
            throw new InvalidOperationException(
                "Une connexion est nécessaire pour synchroniser les plantes.");
        }

        // 1. Lire les deux côtés avant de modifier quoi que ce soit.
        var cloudPlants = await _cloudRepository.GetAllAsync();
        var localPlants = await GetLocalPlantsAsync();

        // 2. Décider : la version modifiée le plus récemment l'emporte.
        var plan = PlantSyncPlanner.Plan(localPlants, cloudPlants);

        // 3. Appliquer.
        foreach (var plant in plan.ToPush)
        {
            await _cloudRepository.UpsertAsync(plant);
            await _localRepository.MarkAsSyncedAsync(plant.Id);
        }

        foreach (var plant in plan.ToPull)
        {
            await _localRepository.UpsertFromCloudAsync(plant);
        }
    }

    // Plantes visibles + modifications en attente (y compris les suppressions
    // locales, qui ne sont plus visibles mais doivent encore être envoyées).
    private async Task<IReadOnlyList<Plant>> GetLocalPlantsAsync()
    {
        var visible = await _localRepository.GetAllAsync();
        var unsynced = await _localRepository.GetUnsyncedAsync();

        return visible
            .Concat(unsynced)
            .GroupBy(plant => plant.Id)
            .Select(group => group.Last())
            .ToList();
    }
}