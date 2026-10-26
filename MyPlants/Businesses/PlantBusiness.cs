using MyPlants.Interfaces.IBusinesses;
using MyPlants.Models;

namespace MyPlants.Businesses;

public class PlantBusiness : IPlantBusiness
{
    public void SetLastWateringDate(Plant plant, DateTime date)
    {
        if (plant is null)
            return;

        if(plant.LastWatered is null || plant.LastWatered < date)
            plant.LastWatered = date;
    }
}
