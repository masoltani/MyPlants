using Microsoft.JSInterop;

using MyPlants.Businesses;
using MyPlants.Interfaces.IRepositories;
using MyPlants.Interfaces.IServices;

namespace MyPlants.Services;

/// <summary>
/// Nombre de plantes à arroser aujourd'hui (ou en retard), et badge de l'icône de l'application.
/// Ne lève jamais d'exception : le badge est un confort, il ne doit pas gêner le reste de l'application.
/// </summary>
public sealed class DueCountService : IDueCountRefresher, IDisposable
{
    private const string JS_SET_BADGE = "myPlantsBadge.set";

    private readonly IPlantRepository _plants;
    private readonly IJSRuntime _js;
    private readonly AutoSyncService _autoSync;
    private readonly IUserSessionService _session;

    public int Count { get; private set; }

    public DueCountService(
        IPlantRepository plants,
        IJSRuntime js,
        AutoSyncService autoSync,
        IUserSessionService session)
    {
        _plants = plants;
        _js = js;
        _autoSync = autoSync;
        _session = session;
    }

    // À appeler au démarrage, après l'initialisation de la session et de la synchronisation automatique.
    public async Task InitializeAsync()
    {
        // Des plantes arrivées d'un autre appareil, ou un autre profil, changent le décompte.
        _autoSync.OnLocalDataChanged += RequestRefresh;
        _session.OnSessionChanged += RequestRefresh;

        await RefreshAsync();
    }

    public void RequestRefresh() => _ = RefreshAsync();

    public async Task RefreshAsync()
    {
        try
        {
            var today = DateTime.Today;
            var plants = await _plants.GetAllAsync();

            // Les plantes archivées ou sans échéance connue sont ignorées par GetDaysUntilWatering.
            Count = plants.Count(plant => PlantSorter.GetDaysUntilWatering(plant, today) is <= 0);

            await _js.InvokeVoidAsync(JS_SET_BADGE, Count);
        }
        catch (Exception)
        {
            // Stockage ou navigateur indisponible : on garde le dernier décompte.
        }
    }

    public void Dispose()
    {
        _autoSync.OnLocalDataChanged -= RequestRefresh;
        _session.OnSessionChanged -= RequestRefresh;
    }
}