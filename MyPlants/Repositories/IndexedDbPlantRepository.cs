using Microsoft.JSInterop;

using MyPlants.Interfaces.IRepositories;
using MyPlants.Models;

using System.Text.Json;

namespace MyPlants.Repositories;

public class IndexedDbPlantRepository : IPlantRepository
{
    private readonly IJSRuntime _jsRuntime;
    private static readonly JsonSerializerOptions JsonOptions = new() 
    { 
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
    };
    
    public IndexedDbPlantRepository(IJSRuntime jsRuntime) 
    {
        _jsRuntime = jsRuntime; 
    }
    public async Task<IReadOnlyList<Plant>> GetAllAsync() 
    { 
        var plants = await _jsRuntime.InvokeAsync<Plant[]>("myPlantsIndexedDb.getAll"); 
        return plants; 
    }

    public async Task<Plant?> GetByIdAsync(Guid id) 
    { 
        return await _jsRuntime.InvokeAsync<Plant?>("myPlantsIndexedDb.getById", id.ToString()); 
    }

    public async Task AddAsync(Plant plant) 
    { 
        await _jsRuntime.InvokeVoidAsync("myPlantsIndexedDb.add", plant); 
    }

    public async Task UpdateAsync(Plant plant)
    {
        await _jsRuntime.InvokeVoidAsync("myPlantsIndexedDb.update", plant); 
    }

    public async Task DeleteAsync(Guid id)
    {
        await _jsRuntime.InvokeVoidAsync("myPlantsIndexedDb.delete", id.ToString()); 
    }

    public async Task AddWateringAsync(Watering watering)
    {
        await _jsRuntime.InvokeVoidAsync(
            "myPlantsIndexedDb.addWatering",
            watering);
    }

    public async Task<IReadOnlyList<Watering>> GetWateringHistoryAsync(Guid plantId)
    {
        var result = await _jsRuntime.InvokeAsync<Watering[]>(
            "myPlantsIndexedDb.getWateringHistory",
            plantId.ToString());

        return result;
    }
}
