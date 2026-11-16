using Microsoft.JSInterop;

namespace MyPlants.Services;

/// <summary>
/// Service de détection de l'état réseau (online/offline).
/// Émet des événements quand l'état change, permettant à l'application de s'adapter.
/// </summary>
public class OnlineOfflineService
{
    private readonly IJSRuntime _js;
    private bool _isOnline = true;
    private DotNetObjectReference<OnlineOfflineService>? _objectRef;

    /// <summary>Indique si l'application est en ligne.</summary>
    public bool IsOnline => _isOnline;

    /// <summary>Événement déclenché quand l'état réseau change (online/offline).</summary>
    public event Action<bool>? OnStatusChanged;

    public OnlineOfflineService(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>Initialise l'écoute des changements d'état réseau.</summary>
    public async Task InitializeAsync()
    {
        _objectRef = DotNetObjectReference.Create(this);

        // Demande au JavaScript de configurer les événements online/offline
        await _js.InvokeVoidAsync("myPlantsOnlineOffline.initialize", _objectRef);

        // Synchronise l'état initial depuis le navigateur
        _isOnline = await _js.InvokeAsync<bool>("myPlantsOnlineOffline.getStatus");
    }

    /// <summary>Appelé par le JavaScript quand le statut réseau change.</summary>
    [JSInvokable]
    public void OnNetworkStatusChanged(bool isOnline)
    {
        _isOnline = isOnline;
        OnStatusChanged?.Invoke(_isOnline);
    }

    public async ValueTask DisposeAsync()
    {
        _objectRef?.Dispose();

        if (_objectRef != null)
        {
            await _js.InvokeVoidAsync("myPlantsOnlineOffline.dispose");
        }
    }
}
