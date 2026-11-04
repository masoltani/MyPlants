using Microsoft.JSInterop;

using MyPlants.Interfaces.IRepositories;
using MyPlants.Interfaces.IServices;
using MyPlants.Models;

namespace MyPlants.Repositories;

public sealed class IndexedDbWateringRepository : ILocalWateringRepository
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IUserSessionService _userSessionService;

    public IndexedDbWateringRepository(IJSRuntime jsRuntime, IUserSessionService userSessionService)
    {
        _jsRuntime = jsRuntime;
        _userSessionService = userSessionService;
    }

    public async Task<IReadOnlyList<Watering>> GetAllAsync()
    {
        return await _jsRuntime.InvokeAsync<Watering[]>(
            "myPlantsIndexedDb.getAllWaterings",
            _userSessionService.ProfileId);
    }

    public async Task MarkAsSyncedAsync(IEnumerable<Guid> ids)
    {
        await _jsRuntime.InvokeVoidAsync(
            "myPlantsIndexedDb.markWateringsAsSynced",
            ids.Select(id => id.ToString()).ToArray(),
            _userSessionService.ProfileId);
    }

    public async Task UpsertManyFromCloudAsync(IEnumerable<Watering> waterings)
    {
        await _jsRuntime.InvokeVoidAsync(
            "myPlantsIndexedDb.upsertWateringsFromCloud",
            waterings.ToArray(),
            _userSessionService.ProfileId);
    }
}