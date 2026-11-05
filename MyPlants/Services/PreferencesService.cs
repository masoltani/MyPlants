using Microsoft.JSInterop;

using MyPlants.Interfaces.IServices;

namespace MyPlants.Services;

public sealed class PreferencesService : IPreferencesService
{
    private readonly IJSRuntime _js;

    public PreferencesService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<string?> GetAsync(string key)
    {
        try
        {
            return await _js.InvokeAsync<string?>("myPlantsStorage.getPreference", key);
        }
        catch (JSException)
        {
            return null;
        }
    }

    public async Task SetAsync(string key, string value)
    {
        try
        {
            await _js.InvokeVoidAsync("myPlantsStorage.setPreference", key, value);
        }
        catch (JSException)
        {
            // Préférence non conservée : l'application continue de fonctionner.
        }
    }
}