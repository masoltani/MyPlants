using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using MyPlants;
using MyPlants.Interfaces.IRepositories;
using MyPlants.Interfaces.IServices;
using MyPlants.Repositories;
using MyPlants.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<IPlantRepository, IndexedDbPlantRepository>();
builder.Services.AddScoped<IPlantService, PlantService>();
builder.Services.AddSingleton<AppInfoService>();
builder.Services.AddSingleton<LocalizationService>();

await builder.Build().RunAsync();
