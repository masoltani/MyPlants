using MyPlants.Models;

namespace MyPlants.Interfaces.IServices;

public interface IPlantSyncService
{
    Task<SyncResult> SyncAsync();
}
