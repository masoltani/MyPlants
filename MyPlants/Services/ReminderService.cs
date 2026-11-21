using Microsoft.JSInterop;

using MyPlants.Businesses;
using MyPlants.Interfaces.IRepositories;
using MyPlants.Interfaces.IServices;
using MyPlants.Models;
using MyPlants.Resources;

namespace MyPlants.Services;

/// <summary>
/// Rappels d'arrosage locaux : une notification par jour sur cet appareil, sans serveur.
/// Le navigateur réveille le service worker (Periodic Background Sync) ; celui-ci lit les réglages et les dates
/// que ce service y enregistre (reminders.js / remindersStore.js).
/// Ne lève jamais d'exception : les rappels sont un confort, ils ne doivent pas gêner le reste de l'application.
/// </summary>
public sealed class ReminderService : IAsyncDisposable
{
    private const string ENABLED_KEY = "reminders-local-enabled";
    private const string HOUR_KEY = "reminders-local-hour";
    private const string VALUE_ON = "1";
    private const string VALUE_OFF = "0";

    public const int DefaultHour = 9;

    private readonly IPreferencesService _preferences;
    private readonly IPlantRepository _plants;
    private readonly IUserSessionService _session;
    private readonly IJSRuntime _js;
    private readonly LocalizationService _localization;

    public bool Enabled { get; private set; }

    public int Hour { get; private set; } = DefaultHour;

    public ReminderService(
        IPreferencesService preferences,
        IPlantRepository plants,
        IUserSessionService session,
        IJSRuntime js,
        LocalizationService localization)
    {
        _preferences = preferences;
        _plants = plants;
        _session = session;
        _js = js;
        _localization = localization;
    }

    // À appeler au démarrage, avant DueCountService.InitializeAsync().
    public async Task InitializeAsync()
    {
        Enabled = await _preferences.GetAsync(ENABLED_KEY) == VALUE_ON;

        if (int.TryParse(await _preferences.GetAsync(HOUR_KEY), out var hour) && hour is >= 0 and <= 23)
            Hour = hour;

        // Les textes suivent la langue courante : on les réécrit à chaque démarrage.
        if (Enabled)
            //await SaveSettingsAsync();
            _ = SaveSettingsDeferredAsync();

        // S'abonner aux changements de langue
        _localization.OnCultureChanged += OnCultureChangedAsync;
    }

    private async void OnCultureChangedAsync()
    {
        // Si les rappels sont activés, re-sauvegarder les textes avec la nouvelle langue
        if (Enabled)
        {
            try
            {
                await SaveSettingsAsync();
            }
            catch (Exception)
            {
                // Les rappels sont un confort, ne pas laisser une erreur bloquer le changement de langue
            }
        }
    }

    public async Task<ReminderStatus> GetStatusAsync()
    {
        try
        {
            return await _js.InvokeAsync<ReminderStatus>("myPlantsReminders.getStatus");
        }
        catch (Exception)
        {
            return new ReminderStatus { Supported = false, Permission = "denied" };
        }
    }

    // À appeler directement depuis un clic (la demande d'autorisation du navigateur l'exige) :
    // le premier appel asynchrone doit être celui du JavaScript.
    public async Task<ReminderEnableResult> EnableAsync()
    {
        string result;

        try
        {
            result = await _js.InvokeAsync<string>("myPlantsReminders.enable");
        }
        catch (Exception)
        {
            return ReminderEnableResult.Failed;
        }

        if (result != "ok")
        {
            return result switch
            {
                "unsupported" => ReminderEnableResult.Unsupported,
                "denied" => ReminderEnableResult.Denied,
                _ => ReminderEnableResult.Failed
            };
        }

        Enabled = true;
        await _preferences.SetAsync(ENABLED_KEY, VALUE_ON);

        // lastNotifiedOn remis à zéro : réactiver permet de recevoir de nouveau le rappel du jour.
        await SaveSettingsAsync(resetLastNotified: true);
        await RefreshSnapshotAsync(await _plants.GetAllAsync());

        return ReminderEnableResult.Ok;
    }

    public async Task DisableAsync()
    {
        Enabled = false;
        await _preferences.SetAsync(ENABLED_KEY, VALUE_OFF);

        await SaveSettingsAsync();

        try
        {
            await _js.InvokeVoidAsync("myPlantsReminders.disable");
        }
        catch (Exception)
        {
            // Le service worker n'enverra plus rien : le réglage est désactivé.
        }
    }

    public async Task SetHourAsync(int hour)
    {
        if (hour is < 0 or > 23)
            return;

        Hour = hour;
        await _preferences.SetAsync(HOUR_KEY, hour.ToString());

        if (Enabled)
            await SaveSettingsAsync();
    }

    // Renvoie "ok", "denied" ou "failed".
    public async Task<string> SendTestAsync()
    {
        try
        {
            // Les textes peuvent ne pas être enregistrés si les rappels sont encore désactivés.
            await SaveSettingsAsync();

            return await _js.InvokeAsync<string>("myPlantsReminders.test");
        }
        catch (Exception)
        {
            return "failed";
        }
    }

    // Appelé par DueCountService à chaque changement de plantes ou de profil.
    public async Task RefreshSnapshotAsync(IEnumerable<Plant> plants)
    {
        if (!Enabled)
            return;

        try
        {
            var dates = ReminderSnapshot.BuildDates(plants, DateTime.Today);

            await _js.InvokeVoidAsync("myPlantsReminders.saveDue", _session.ProfileId, dates);
        }
        catch (Exception)
        {
            // Stockage indisponible : le dernier instantané reste utilisé.
        }
    }

    private async Task SaveSettingsAsync(bool resetLastNotified = false)
    {
        try
        {
            var settings = new Dictionary<string, object?>
            {
                ["enabled"] = Enabled,
                ["hour"] = Hour,
                ["title"] = SharedResource.Reminders_Notification_Title,
                ["bodyOne"] = SharedResource.Reminders_Notification_One,
                ["bodyMany"] = SharedResource.Reminders_Notification_Many,
                ["testBody"] = SharedResource.Reminders_Test_Body
            };

            if (resetLastNotified)
                settings["lastNotifiedOn"] = null;

            await _js.InvokeVoidAsync("myPlantsReminders.saveSettings", settings);
        }
        catch (Exception)
        {
            // Voir RefreshSnapshotAsync.
        }
    }

    private async Task SaveSettingsDeferredAsync()
    {
        try
        {
            await Task.Delay(1500); // laisse l'app démarrer et la culture se stabiliser
            await SaveSettingsAsync();
        }
        catch { /* best effort */ }
    }

    public ValueTask DisposeAsync()
    {
        _localization.OnCultureChanged -= OnCultureChangedAsync;
        return ValueTask.CompletedTask;
    }
}