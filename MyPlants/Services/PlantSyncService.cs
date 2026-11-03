using MyPlants.Interfaces.IRepositories;
using MyPlants.Interfaces.IServices;

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

        // 1. Envoyer les modifications locales vers Supabase.
        var unsyncedPlants = await _localRepository.GetUnsyncedAsync();

        foreach (var plant in unsyncedPlants)
        {
            await _cloudRepository.UpsertAsync(plant);
            await _localRepository.MarkAsSyncedAsync(plant.Id);
        }

        // 2. Récupérer les plantes présentes dans Supabase.
        var cloudPlants = await _cloudRepository.GetAllAsync();

        // 3. Intégrer les plantes distantes dans IndexedDB.
        var localPlants = await _localRepository.GetAllAsync();

        var localById = localPlants.ToDictionary(p => p.Id);

        foreach (var cloudPlant in cloudPlants)
        {
            if (localById.TryGetValue(cloudPlant.Id, out var localPlant))
            {
                // Ne jamais écraser une modification locale en attente.
                if (!localPlant.IsSynced)
                    continue;

                // Ne remplacer que si la version distante est plus récente.
                if (cloudPlant.UpdatedAt > localPlant.UpdatedAt)
                {
                    await _localRepository.UpsertFromCloudAsync(cloudPlant);
                }
            }
            else
            {
                await _localRepository.UpsertFromCloudAsync(cloudPlant);
            }
        }
    }
}