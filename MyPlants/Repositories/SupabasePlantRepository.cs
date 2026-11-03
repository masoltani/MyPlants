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

    // Toutes les plantes du compte, y compris les plantes supprimées (DeletedAt renseigné).
    public async Task<IReadOnlyList<Plant>> GetAllAsync()
    {
        var module = await GetModuleAsync();

        var rows = await module.InvokeAsync<List<SupabasePlantDto>>("getAll");

        return rows.Select(MapToPlant).ToList();
    }

    public async Task UpsertAsync(Plant plant)
    {
        ArgumentNullException.ThrowIfNull(plant);

        var module = await GetModuleAsync();

        await module.InvokeVoidAsync("upsert", plant);
    }

    public async ValueTask DisposeAsync()
    {
        if (_jsModule is null)
            return;

        try
        {
            await _jsModule.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // Le runtime JavaScript peut déjà être déconnecté.
        }
    }

    private async Task<IJSObjectReference> GetModuleAsync()
    {
        _jsModule ??= await _jsRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            "./js/supabasePlants.js");

        return _jsModule;
    }

    // Plant.UpdatedAt représente toujours l'heure de modification faite par l'utilisateur,
    // en local comme dans le cloud : les deux versions restent comparables.
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
            WateringInterval = row.WateringInterval,
            UpdatedAt = row.ClientUpdatedAt ?? row.UpdatedAt,
            DeletedAt = row.DeletedAt
        };
    }
}