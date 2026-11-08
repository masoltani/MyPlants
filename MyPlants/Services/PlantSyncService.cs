using MyPlants.Businesses;
using MyPlants.Interfaces.IRepositories;
using MyPlants.Interfaces.IServices;
using MyPlants.Models;

namespace MyPlants.Services;

/// <summary>Synchronise les plantes, puis leurs arrosages, entre IndexedDB et Supabase.</summary>
public sealed class PlantSyncService : IPlantSyncService
{
    private readonly IPlantRepository _localPlants;
    private readonly ISupabasePlantRepository _cloudPlants;
    private readonly ILocalWateringRepository _localWaterings;
    private readonly ISupabaseWateringRepository _cloudWaterings;
    private readonly IUserSessionService _userSessionService;

    // Une seule synchronisation à la fois : le bouton manuel et la synchronisation automatique
    // peuvent se déclencher en même temps, la seconde attend alors la fin de la première.
    private readonly SemaphoreSlim _gate = new(1, 1);

    public PlantSyncService(
        IPlantRepository localPlants,
        ISupabasePlantRepository cloudPlants,
        ILocalWateringRepository localWaterings,
        ISupabaseWateringRepository cloudWaterings,
        IUserSessionService userSessionService)
    {
        _localPlants = localPlants;
        _cloudPlants = cloudPlants;
        _localWaterings = localWaterings;
        _cloudWaterings = cloudWaterings;
        _userSessionService = userSessionService;
    }

    public async Task<SyncResult> SyncAsync()
    {
        await _gate.WaitAsync();

        try
        {
            await _userSessionService.InitializeAsync();

            if (!_userSessionService.IsLoggedIn)
            {
                throw new InvalidOperationException(
                    "Une connexion est nécessaire pour synchroniser les plantes.");
            }

            // Les plantes d'abord : le serveur refuse un arrosage dont la plante n'existe pas.
            var (cloudPlantIds, plantsPulled) = await SyncPlantsAsync();

            var wateringsPulled = await SyncWateringsAsync(cloudPlantIds);

            return new SyncResult(plantsPulled || wateringsPulled);
        }
        finally
        {
            _gate.Release();
        }
    }

    // Renvoie les identifiants des plantes présentes dans le cloud après synchronisation,
    // et si des plantes ont été enregistrées localement.
    private async Task<(IReadOnlySet<Guid> CloudPlantIds, bool Pulled)> SyncPlantsAsync()
    {
        // 1. Lire les deux côtés avant de modifier quoi que ce soit.
        var cloudPlants = await _cloudPlants.GetAllAsync();
        var localPlants = await GetLocalPlantsAsync();

        // 2. Décider : la version modifiée le plus récemment l'emporte.
        var plan = PlantSyncPlanner.Plan(localPlants, cloudPlants);

        // 3. Appliquer.
        foreach (var plant in plan.ToPush)
        {
            await _cloudPlants.UpsertAsync(plant);
            await _localPlants.MarkAsSyncedAsync(plant.Id);
        }

        foreach (var plant in plan.ToPull)
        {
            await _localPlants.UpsertFromCloudAsync(plant);
        }

        var cloudIds = cloudPlants
            .Select(plant => plant.Id)
            .Concat(plan.ToPush.Select(plant => plant.Id))
            .ToHashSet();

        return (cloudIds, plan.ToPull.Any());
    }

    // Renvoie vrai si des arrosages ont été enregistrés localement.
    private async Task<bool> SyncWateringsAsync(IReadOnlySet<Guid> cloudPlantIds)
    {
        var cloudWaterings = await _cloudWaterings.GetAllAsync();
        var localWaterings = await _localWaterings.GetAllAsync();

        var plan = WateringSyncPlanner.Plan(localWaterings, cloudWaterings, cloudPlantIds);

        await _cloudWaterings.UpsertManyAsync(plan.ToPush);

        var syncedIds = plan.ToPush
            .Concat(plan.ToMarkSynced)
            .Select(watering => watering.Id)
            .ToList();

        if (syncedIds.Count > 0)
            await _localWaterings.MarkAsSyncedAsync(syncedIds);

        if (plan.ToPull.Count > 0)
        {
            await _localWaterings.UpsertManyFromCloudAsync(plan.ToPull);
            return true;
        }

        return false;
    }

    // Plantes visibles + modifications en attente (y compris les suppressions
    // locales, qui ne sont plus visibles mais doivent encore être envoyées).
    private async Task<IReadOnlyList<Plant>> GetLocalPlantsAsync()
    {
        var visible = await _localPlants.GetAllAsync();
        var unsynced = await _localPlants.GetUnsyncedAsync();

        return visible
            .Concat(unsynced)
            .GroupBy(plant => plant.Id)
            .Select(group => group.Last())
            .ToList();
    }
}