using MyPlants.Interfaces.IServices;
using MyPlants.Models;

namespace MyPlants.Services;

public class PlantService : IPlantService
{
    public Plant GetPlantDetails(string plantName)
    {
        // For demonstration purposes, returning a hardcoded plant.
        // In a real application, this would fetch data from a database or an API.
        return new Plant
        {
            Name = plantName,
            Species = "Ficus lyrata",
            DatePlanted = new DateTime(2022, 5, 15),
            IsArchived = false
        };
    }

    public List<Plant> GetPlants()
    {
        // For demonstration purposes, returning a hardcoded list of plants.
        // In a real application, this would fetch data from a database or an API.
        return new List<Plant>
        {
            new Plant { Name = "Fiddle Leaf Fig", Species = "Ficus lyrata", DatePlanted = new DateTime(2022, 5, 15), IsArchived = false },
            new Plant { Name = "Snake Plant", Species = "Sansevieria trifasciata", DatePlanted = new DateTime(2021, 3, 10), IsArchived = false },
            new Plant { Name = "Peace Lily", Species = "Spathiphyllum", DatePlanted = new DateTime(2020, 8, 20), IsArchived = true }
        };
    }

    public Water GetWateringInfo(string plantName)
    {
        // For demonstration purposes, returning hardcoded watering info.
        // In a real application, this would fetch data from a database or an API.
        return new Water
        {
            LastWatered = new DateTime(2024, 6, 1),
            NextWater = new DateTime(2024, 6, 8)
        };
    }

    public void WaterPlant(Plant plant)
    {
        // For demonstration purposes, this method does not perform any action.
        // In a real application, this would update the watering information in a database or an API.
        Console.WriteLine($"Watering plant: {plant.Name}");
        plant.WateringHistory.Add(new Water
        {
            LastWatered = DateTime.Now,
            NextWater = DateTime.Now.AddDays(plant.WateringInterval ?? 7) // Example interval
        });
    }
}
