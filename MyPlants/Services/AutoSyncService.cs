using Microsoft.JSInterop;

using MyPlants.Interfaces.IServices;

namespace MyPlants.Services;

/// <summary>
/// Synchronisation automatique, réservée aux comptes connectés et désactivable dans les Réglages.
/// Se déclenche au démarrage, à la connexion, au retour du réseau, au retour de l'application
/// au premier plan et à intervalle régulier. Ne lève jamais d'exception : en cas d'échec,
/// le déclencheur suivant réessaie, et les modifications locales restent en attente d'envoi.
/// </summary>
public sealed class AutoSyncService : IDisposable
{
    private const string ENABLED_KEY = "auto-sync";
    private const string VALUE_ON = "1";
    private const string VALUE_OFF = "0";

    private const string JS_START = "myPlantsAutoSync.start";
    private const string JS_STOP = "myPlantsAutoSync.stop";

    // Évite d'enchaîner les synchronisations quand plusieurs déclencheurs se suivent de près.
    private static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(30);

    private readonly IJSRuntime _js;
    private readonly IPlantSyncService _sync;
    private readonly IUserSessionService _session;
    private readonly IPreferencesService _preferences;

    private DotNetObjectReference<AutoSyncService>? _self;
    private DateTime _lastAttemptUtc = DateTime.MinValue;
    private bool _running;
    private bool _initialized;

    // Activée par défaut : sans effet pour un invité, qui n'a rien à synchroniser.
    public bool Enabled { get; private set; } = false;

    /// <summary>Levé quand une synchronisation automatique a modifié les données locales.</summary>
    public event Action? OnLocalDataChanged;

    public AutoSyncService(
        IJSRuntime js,
        IPlantSyncService sync,
        IUserSessionService session,
        IPreferencesService preferences)
    {
        _js = js;
        _sync = sync;
        _session = session;
        _preferences = preferences;
    }

    // À appeler au démarrage, après l'initialisation de la session. N'attend pas le réseau.
    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        _initialized = true;

        Enabled = await _preferences.GetAsync(ENABLED_KEY) == VALUE_ON;

        _session.OnSessionChanged += OnSessionChanged;

        if (Enabled)
        {
            await StartListeningAsync();
            _ = RunAsync(force: true);
        }
    }

    public async Task SetEnabledAsync(bool enabled)
    {
        if (enabled == Enabled)
            return;

        Enabled = enabled;

        await _preferences.SetAsync(ENABLED_KEY, enabled ? VALUE_ON : VALUE_OFF);

        if (enabled)
        {
            await StartListeningAsync();
            _ = RunAsync(force: true);
        }
        else
        {
            await StopListeningAsync();
        }
    }

    // Appelée par autoSync.js.
    [JSInvokable]
    public Task OnTriggerAsync(string reason) => RunAsync(force: false);

    private void OnSessionChanged()
    {
        // Après une connexion, on récupère tout de suite les données du compte.
        if (_session.IsLoggedIn)
            _ = RunAsync(force: true);
    }

    private async Task RunAsync(bool force)
    {
        if (!Enabled || !_session.IsLoggedIn || _running)
            return;

        var now = DateTime.UtcNow;

        if (!force && now - _lastAttemptUtc < MinInterval)
            return;

        _running = true;
        _lastAttemptUtc = now;

        try
        {
            var result = await _sync.SyncAsync();

            if (result.LocalDataChanged)
                OnLocalDataChanged?.Invoke();
        }
        catch (Exception)
        {
            // Réseau coupé, session expirée, erreur serveur… : silencieux, le prochain déclencheur réessaie.
        }
        finally
        {
            _running = false;
        }
    }

    private async Task StartListeningAsync()
    {
        try
        {
            _self ??= DotNetObjectReference.Create(this);
            await _js.InvokeVoidAsync(JS_START, _self);
        }
        catch (JSException)
        {
            // Sans déclencheurs, il reste la synchronisation manuelle.
        }
    }

    private async Task StopListeningAsync()
    {
        try
        {
            await _js.InvokeVoidAsync(JS_STOP);
        }
        catch (JSException)
        {
        }
    }

    public void Dispose()
    {
        _session.OnSessionChanged -= OnSessionChanged;
        _self?.Dispose();
    }
}