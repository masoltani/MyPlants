using Microsoft.JSInterop;

using MyPlants.Dtos;
using MyPlants.Interfaces.IRepositories;
using MyPlants.Models;

namespace MyPlants.Repositories;

public class SupabasePlantRepository : ISupabasePlantRepository, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _jsModule;

    public SupabasePlantRepository(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    private async Task<IJSObjectReference> GetModuleAsync()
    {
        _jsModule ??= await _jsRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            "./js/supabasePlants.js");

        return _jsModule;
    }

    public async Task<IReadOnlyList<Plant>> GetAllAsync()
    {
        var module = await GetModuleAsync();

        var rows = await module.InvokeAsync<List<SupabasePlantDto>>(
            "getAll");

        return rows.Select(MapToPlant).ToList();
    }

    public async Task<Plant> SaveAsync(Plant plant)
    {
        ArgumentNullException.ThrowIfNull(plant);

        var module = await GetModuleAsync();

        var row = await module.InvokeAsync<SupabasePlantDto>(
            "save",
            new
            {
                id = plant.Id,
                name = plant.Name,
                species = plant.Species,
                datePlanted = plant.DatePlanted,
                lastWatered = plant.LastWatered,
                isArchived = plant.IsArchived,
                wateringInterval = plant.WateringInterval
            });

        return MapToPlant(row);
    }

    public async Task DeleteAsync(Guid id)
    {
        var module = await GetModuleAsync();

        await module.InvokeVoidAsync("remove", id);
    }

    private static Plant MapToPlant(SupabasePlantDto row)
    {
        return new Plant
        {
            Id = row.Id,
            Name = row.Name,
            Species = row.Species,
            DatePlanted = row.DatePlanted,
            LastWatered = row.LastWatered,
            IsArchived = row.IsArchived,
            WateringInterval = row.WateringInterval
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (_jsModule is not null)
        {
            try
            {
                await _jsModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Le runtime JavaScript peut déjà être déconnecté.
            }
        }
    }

    public async Task UpsertAsync(Plant plant)
    {
        var module = await GetModuleAsync();

        await module.InvokeVoidAsync("upsert", plant);
    }
}
