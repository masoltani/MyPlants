using System.Reflection;

namespace MyPlants.Services;

public class AppInfoService
{
    public string Version =>
        Assembly.GetExecutingAssembly()
            .GetName()
            .Version?
            .ToString(3)
        ?? "0.0.0";
}