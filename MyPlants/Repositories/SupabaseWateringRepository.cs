using Microsoft.JSInterop;

using MyPlants.Interfaces.IRepositories;
using MyPlants.Models;

namespace MyPlants.Repositories;

public sealed class SupabaseWateringRepository
    : ISupabaseWateringRepository, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;

    public SupabaseWateringRepository(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<IReadOnlyList<Watering>> GetAllAsync()
    {
        var module = await GetModuleAsync();

        return await module.InvokeAsync<Watering[]>("getAll");
    }

    public async Task UpsertAsync(Watering watering)
    {
        var module = await GetModuleAsync();

        await module.InvokeVoidAsync("upsert", watering);
    }

    // Envoi groupé : une requête pour plusieurs arrosages.
    public async Task UpsertManyAsync(IReadOnlyList<Watering> waterings)
    {
        if (waterings.Count == 0)
            return;

        var module = await GetModuleAsync();

        await module.InvokeVoidAsync("upsertMany", waterings);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is null)
            return;

        try
        {
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // Le runtime JavaScript peut déjà être déconnecté.
        }
    }

    private async Task<IJSObjectReference> GetModuleAsync()
    {
        _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            "./js/supabaseWaterings.js");

        return _module;
    }
}