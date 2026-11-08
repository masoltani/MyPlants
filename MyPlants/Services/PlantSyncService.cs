using MyPlants.Businesses;
using MyPlants.Interfaces.IRepositories;
using MyPlants.Interfaces.IServices;
using MyPlants.Models;

namespace MyPlants.Services;

/// <summary>
/// Synchronise les plantes, puis leurs arrosages, entre IndexedDB et Supabase.
/// Incrémentale : seules les lignes du cloud modifiées depuis la dernière synchronisation sont téléchargées.
/// </summary>
public sealed class PlantSyncService : IPlantSyncService
{
    // Recul appliqué au point de reprise : une transaction du serveur peut être validée après une
    // plus récente. Relire un peu de données déjà vues est sans danger (la comparaison les ignore).
    private static readonly TimeSpan CursorOverlap = TimeSpan.FromMinutes(5);

    private readonly IPlantRepository _localPlants;
    private readonly ISupabasePlantRepository _cloudPlants;
    private readonly ILocalWateringRepository _localWaterings;
    private readonly ISupabaseWateringRepository _cloudWaterings;
    private readonly IUserSessionService _userSessionService;
    private readonly SyncCursorStore _cursors;

    // Une seule synchronisation à la fois : le bouton manuel et la synchronisation automatique
    // peuvent se déclencher en même temps, la seconde attend alors la fin de la première.
    private readonly SemaphoreSlim _gate = new(1, 1);

    public PlantSyncService(
        IPlantRepository localPlants,
        ISupabasePlantRepository cloudPlants,
        ILocalWateringRepository localWaterings,
        ISupabaseWateringRepository cloudWaterings,
        IUserSessionService userSessionService,
        SyncCursorStore cursors)
    {
        _localPlants = localPlants;
        _cloudPlants = cloudPlants;
        _localWaterings = localWaterings;
        _cloudWaterings = cloudWaterings;
        _userSessionService = userSessionService;
        _cursors = cursors;
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

            var localPlants = await GetLocalPlantsAsync();

            // Les plantes d'abord : le serveur refuse un arrosage dont la plante n'existe pas.
            var (cloudPlantIds, plantsPulled) = await SyncPlantsAsync(localPlants);

            var wateringsPulled = await SyncWateringsAsync(cloudPlantIds);

            return new SyncResult(plantsPulled || wateringsPulled);
        }
        finally
        {
            _gate.Release();
        }
    }

    // Renvoie les identifiants des plantes présentes dans le cloud, et si des plantes ont été enregistrées localement.
    private async Task<(IReadOnlySet<Guid> CloudPlantIds, bool Pulled)> SyncPlantsAsync(IReadOnlyList<Plant> localPlants)
    {
        // 1. Lire avant de modifier quoi que ce soit : seulement ce qui a changé dans le cloud.
        var since = await GetSinceAsync(SyncCursorStore.Plants, hasLocalData: localPlants.Count > 0);
        var changes = await _cloudPlants.GetChangesAsync(since);

        // 2. Décider : la version modifiée le plus récemment l'emporte.
        var plan = PlantSyncPlanner.Plan(localPlants, changes.Items);

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

        // 4. Seulement une fois tout appliqué : mémoriser le point de reprise.
        await SaveCursorAsync(SyncCursorStore.Plants, changes.Cursor);

        // Le cloud ne renvoie plus toutes les plantes : on le déduit. Une plante locale déjà synchronisée
        // existe dans le cloud (même supprimée), en plus de celles reçues ou envoyées maintenant.
        var cloudIds = changes.Items.Select(plant => plant.Id)
            .Concat(plan.ToPush.Select(plant => plant.Id))
            .Concat(localPlants.Where(plant => plant.IsSynced).Select(plant => plant.Id))
            .ToHashSet();

        return (cloudIds, plan.ToPull.Any());
    }

    // Renvoie vrai si des arrosages ont été enregistrés localement.
    private async Task<bool> SyncWateringsAsync(IReadOnlySet<Guid> cloudPlantIds)
    {
        var localWaterings = await _localWaterings.GetAllAsync();

        var since = await GetSinceAsync(SyncCursorStore.Waterings, hasLocalData: localWaterings.Count > 0);
        var changes = await _cloudWaterings.GetChangesAsync(since);

        var plan = WateringSyncPlanner.Plan(localWaterings, changes.Items, cloudPlantIds);

        await _cloudWaterings.UpsertManyAsync(plan.ToPush);

        var syncedIds = plan.ToPush
            .Concat(plan.ToMarkSynced)
            .Select(watering => watering.Id)
            .ToList();

        if (syncedIds.Count > 0)
            await _localWaterings.MarkAsSyncedAsync(syncedIds);

        if (plan.ToPull.Count > 0)
            await _localWaterings.UpsertManyFromCloudAsync(plan.ToPull);

        await SaveCursorAsync(SyncCursorStore.Waterings, changes.Cursor);

        return plan.ToPull.Count > 0;
    }

    // Point de départ de la lecture dans le cloud ; null = tout relire.
    private async Task<DateTime?> GetSinceAsync(string table, bool hasLocalData)
    {
        // Rien en local : on ne se fie pas au point de reprise (données locales effacées, par exemple).
        if (!hasLocalData)
            return null;

        var cursor = await _cursors.GetAsync(table);

        return cursor is null ? null : cursor.Value - CursorOverlap;
    }

    private async Task SaveCursorAsync(string table, DateTime? newCursor)
    {
        if (newCursor is null)
            return;

        var current = await _cursors.GetAsync(table);

        if (current is null || newCursor > current)
            await _cursors.SetAsync(table, newCursor.Value);
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