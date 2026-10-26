using MyPlants.Models;

namespace MyPlants.Interfaces.IBusinesses;

public interface IPlantBusiness
{
    void SetLastWateringDate(Plant plant, DateTime date);
}
