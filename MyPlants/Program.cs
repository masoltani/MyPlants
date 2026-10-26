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

builder.Services.AddScoped<IPlantRepository, IndexedDbPlantRepository>();
builder.Services.AddScoped<IPlantBusiness, PlantBusiness>();
builder.Services.AddScoped<IPlantService, PlantService>();
builder.Services.AddSingleton<AppInfoService>();
builder.Services.AddSingleton<LocalizationService>();

var app = builder.Build();

var localizationService = app.Services.GetRequiredService<LocalizationService>();
await localizationService.InitializeAsync();

await app.RunAsync();
