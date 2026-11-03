using Microsoft.JSInterop;

using MyPlants.Interfaces.IRepositories;
using MyPlants.Interfaces.IServices;
using MyPlants.Models;

namespace MyPlants.Repositories;

public class IndexedDbPlantRepository : IPlantRepository
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IUserSessionService _userSessionService;
    
    public IndexedDbPlantRepository(IJSRuntime jsRuntime, IUserSessionService userSessionService) 
    {
        _jsRuntime = jsRuntime; 
        _userSessionService = userSessionService;
    }

    public async Task<IReadOnlyList<Plant>> GetAllAsync()
    {
        var profileId = _userSessionService.ProfileId;

        return await _jsRuntime.InvokeAsync<Plant[]>(
            "myPlantsIndexedDb.getAll",
            profileId);
    }

    public async Task<Plant?> GetByIdAsync(Guid id)
    {
        var profileId = _userSessionService.ProfileId;

        return await _jsRuntime.InvokeAsync<Plant?>(
            "myPlantsIndexedDb.getById",
            id.ToString(),
            profileId);
    }

    public async Task<IReadOnlyList<Plant>> GetUnsyncedAsync()
    {
        var profileId = _userSessionService.ProfileId;

        return await _jsRuntime.InvokeAsync<Plant[]>(
            "myPlantsIndexedDb.getUnsynced",
            profileId);
    }

    public async Task MarkAsSyncedAsync(Guid id)
    {
        var profileId = _userSessionService.ProfileId;

        await _jsRuntime.InvokeVoidAsync(
            "myPlantsIndexedDb.markAsSynced",
            id.ToString(),
            profileId);
    }

    public async Task UpsertFromCloudAsync(Plant plant)
    {
        var profileId = _userSessionService.ProfileId;

        plant.ProfileId = profileId;
        plant.IsSynced = true;

        await _jsRuntime.InvokeVoidAsync(
            "myPlantsIndexedDb.upsertFromCloud",
            plant,
            profileId);
    }

    public async Task AddAsync(Plant plant)
    {
        var profileId = _userSessionService.ProfileId;

        plant.ProfileId = profileId;

        await _jsRuntime.InvokeVoidAsync(
            "myPlantsIndexedDb.add",
            plant,
            profileId);
    }

    public async Task UpdateAsync(Plant plant)
    {
        var profileId = _userSessionService.ProfileId;

        plant.ProfileId = profileId;

        await _jsRuntime.InvokeVoidAsync(
            "myPlantsIndexedDb.update",
            plant,
            profileId);
    }

    public async Task DeleteAsync(Guid id)
    {
        var profileId = _userSessionService.ProfileId;

        await _jsRuntime.InvokeVoidAsync(
            "myPlantsIndexedDb.delete",
            id.ToString(),
            profileId);
    }

    public async Task AddWateringAsync(Watering watering)
    {
        var profileId = _userSessionService.ProfileId;

        watering.ProfileId = profileId;

        await _jsRuntime.InvokeVoidAsync(
            "myPlantsIndexedDb.addWatering",
            watering,
            profileId);
    }

    public async Task<IReadOnlyList<Watering>> GetWateringHistoryAsync(
        Guid plantId)
    {
        var profileId = _userSessionService.ProfileId;

        return await _jsRuntime.InvokeAsync<Watering[]>(
            "myPlantsIndexedDb.getWateringHistory",
            plantId.ToString(),
            profileId);
    }

    public async Task<int> ImportGuestPlantsAsync(
        string targetProfileId)
    {
        var profileId = _userSessionService.ProfileId;

        if (targetProfileId != profileId || targetProfileId == "guest")
        {
            throw new InvalidOperationException(
                "La cible de l'import doit être un compte connecté.");
        }

        return await _jsRuntime.InvokeAsync<int>(
            "myPlantsIndexedDb.importGuestPlants",
            targetProfileId);
    }

    public async Task<bool> HasGuestPlantsAsync()
    {
        //await GetProfileIdAsync();

        return await _jsRuntime.InvokeAsync<bool>(
            "myPlantsIndexedDb.hasGuestPlants");
    }
}
