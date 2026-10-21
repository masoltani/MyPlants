using System.Globalization;

using Microsoft.JSInterop;

using MyPlants.Resources;

namespace MyPlants.Services;

public class LocalizationService
{
    private const string DEFAULT_CULTURE_NAME = "fr-FR";
    private const string LANGUAGE_STORAGE_KEY = "myPlantsStorage";
    private const string DOCUMENT_LOCALIZATION = "myPlantsLocalization";
    private const string GET_LANGUAGE_METHOD = "getLanguage";
    private const string SET_LANGUAGE_METHOD = "setLanguage";
    private const string SET_DOCUMENT_LANGUAGE = "setDocumentLanguage";
    private const string LANGUAGE_DIRECTION_LEFT = "ltr";
    private const string LANGUAGE_DIRECTION_RIGHT = "rtl";

    private const string CULTURE_ARABIC = "ar";

    private readonly IJSRuntime _js;

    public CultureInfo CurrentCulture { get; private set; }

    public bool IsRtl =>
        CurrentCulture.Name.StartsWith(CULTURE_ARABIC, StringComparison.OrdinalIgnoreCase);

    public string Direction => IsRtl 
        ? LANGUAGE_DIRECTION_RIGHT 
        : LANGUAGE_DIRECTION_LEFT;

    public event Action? OnCultureChanged;

    public LocalizationService(IJSRuntime js)
    {
        _js = js;

        CurrentCulture = CultureInfo.GetCultureInfo(DEFAULT_CULTURE_NAME);
    }

    public async Task InitializeAsync()
    {
        var savedLanguage = await _js.InvokeAsync<string?>($"{LANGUAGE_STORAGE_KEY}.{GET_LANGUAGE_METHOD}");

        var cultureName = string.IsNullOrWhiteSpace(savedLanguage)
            ? DEFAULT_CULTURE_NAME
            : savedLanguage;

        await ApplyCultureAsync(cultureName);
    }

    public async Task SetCultureAsync(string cultureName)
    {
        await ApplyCultureAsync(cultureName);

        await _js.InvokeVoidAsync($"{LANGUAGE_STORAGE_KEY}.{SET_LANGUAGE_METHOD}",
                                  cultureName,
                                  Direction);

        OnCultureChanged?.Invoke();
    }

    private async Task ApplyCultureAsync(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        CurrentCulture = culture;
        SharedResource.Culture = culture;

        await _js.InvokeVoidAsync($"{DOCUMENT_LOCALIZATION}.{SET_DOCUMENT_LANGUAGE}",
                                  culture.Name,
                                  Direction);
    }
}
