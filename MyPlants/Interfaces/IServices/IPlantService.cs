using MyPlants.Models;

namespace MyPlants.Interfaces.IServices;

public interface IPlantService
{
    Plant GetPlantDetails(string plantName);
    List<Plant> GetPlants();
    Water GetWateringInfo(string plantName);
    void WaterPlant(Plant plant);
}
