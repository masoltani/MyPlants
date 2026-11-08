using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using MyPlants;
using MyPlants.Businesses;
using MyPlants.Interfaces.IBusinesses;
using MyPlants.Interfaces.IRepositories;
using MyPlants.Interfaces.IServices;
using MyPlants.Repositories;
using MyPlants.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Repositories
builder.Services
    .AddScoped<IPlantRepository, IndexedDbPlantRepository>()
    .AddScoped<ISupabasePlantRepository, SupabasePlantRepository>()
    .AddScoped<ISupabaseWateringRepository, SupabaseWateringRepository>()
    .AddScoped<ILocalWateringRepository, IndexedDbWateringRepository>();

// Businesses
builder.Services.AddScoped<IPlantBusiness, PlantBusiness>();

// Services
builder.Services
    .AddScoped<IPlantService, PlantService>()
    .AddSingleton<AppInfoService>()
    .AddSingleton<LocalizationService>()
    .AddScoped<IUserSessionService, UserSessionService>()
    .AddScoped<IPlantSyncService, PlantSyncService>()
    .AddScoped<IChangelogService, ChangelogService>()
    .AddScoped<IPreferencesService, PreferencesService>()
    .AddScoped<DateDisplayService>()
    .AddScoped<AutoSyncService>(); ;

var app = builder.Build();

var localizationService = app.Services.GetRequiredService<LocalizationService>();
await localizationService.InitializeAsync();
await app.Services.GetRequiredService<DateDisplayService>().InitializeAsync();

var userSessionService = app.Services.GetRequiredService<IUserSessionService>();
await userSessionService.InitializeAsync();

await app.Services.GetRequiredService<AutoSyncService>().InitializeAsync();

await app.RunAsync();
