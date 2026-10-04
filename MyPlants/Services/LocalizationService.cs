using System.Globalization;

namespace MyPlants.Services;

public class LocalizationService
{
    public CultureInfo CurrentCulture { get; private set; }
        = new CultureInfo("en-US");

    public event Action? OnCultureChanged;

    public void SetCulture(string cultureName)
    {
        var culture = new CultureInfo(cultureName);

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        CurrentCulture = culture;

        OnCultureChanged?.Invoke();
    }
}