using MyPlants.Interfaces.IRepositories;
using MyPlants.Interfaces.IServices;
using MyPlants.Models;

namespace MyPlants.Services;

/// <summary>
/// Sauvegarde et restauration des plantes et arrosages du profil courant.
/// La restauration ne fait qu'ajouter : rien de ce qui existe déjà n'est modifié ni supprimé.
/// </summary>
public sealed class BackupService
{
    // Garde-fous contre un fichier démesuré ou fabriqué.
    public const int MaxPlants = 5_000;
    public const int MaxWaterings = 100_000;

    private readonly IPlantRepository _plants;
    private readonly IDueCountRefresher _dueCount;

    public BackupService(IPlantRepository plants, IDueCountRefresher dueCount)
    {
        _plants = plants;
        _dueCount = dueCount;
    }

    public async Task<BackupFile> ExportAsync()
    {
        // Plantes non supprimées (archivées comprises) et leur historique d'arrosage.
        var plants = await _plants.GetAllAsync();

        var waterings = new List<Watering>();

        foreach (var plant in plants)
            waterings.AddRange(await _plants.GetWateringHistoryAsync(plant.Id));

        return new BackupFile
        {
            Plants = plants.ToList(),
            Waterings = waterings
        };
    }

    public static bool IsValid(BackupFile? backup) =>
        backup is not null
        && backup.App == BackupFile.AppName
        && backup.FormatVersion is >= 1 and <= BackupFile.CurrentFormatVersion
        && backup.Plants is not null
        && backup.Waterings is not null
        && backup.Plants.Count <= MaxPlants
        && backup.Waterings.Count <= MaxWaterings;

    public async Task<BackupImportResult> ImportAsync(BackupFile backup)
    {
        if (!IsValid(backup))
            throw new ArgumentException("Fichier de sauvegarde invalide.", nameof(backup));

        // Identifiant de la plante dans le fichier -> identifiant de la plante dans cet appareil.
        var plantIds = new Dictionary<Guid, Guid>();
        var plantsAdded = 0;
        var plantsPresent = 0;

        foreach (var plant in backup.Plants.Where(plant => !string.IsNullOrWhiteSpace(plant.Name)))
        {
            var originalId = plant.Id;

            if (originalId != Guid.Empty && await _plants.GetByIdAsync(originalId) is not null)
            {
                // Déjà là : on la laisse telle quelle, ses arrosages manquants seront tout de même ajoutés.
                plantIds[originalId] = originalId;
                plantsPresent++;
                continue;
            }

            plant.Name = plant.Name!.Trim();

            var newId = await AddPlantAsync(plant);

            if (originalId != Guid.Empty)
                plantIds[originalId] = newId;

            plantsAdded++;
        }

        var wateringsAdded = await ImportWateringsAsync(backup.Waterings, plantIds);

        _dueCount.RequestRefresh();

        return new BackupImportResult(plantsAdded, plantsPresent, wateringsAdded);
    }

    private async Task<int> ImportWateringsAsync(IEnumerable<Watering> waterings, Dictionary<Guid, Guid> plantIds)
    {
        // Arrosages déjà connus de chaque plante, chargés une seule fois par plante.
        var knownByPlant = new Dictionary<Guid, HashSet<Guid>>();
        var added = 0;

        foreach (var watering in waterings)
        {
            if (!plantIds.TryGetValue(watering.PlantId, out var plantId))
                continue;

            if (!knownByPlant.TryGetValue(plantId, out var knownIds))
            {
                var history = await _plants.GetWateringHistoryAsync(plantId);

                knownIds = history.Select(existing => existing.Id).ToHashSet();
                knownByPlant[plantId] = knownIds;
            }

            if (watering.Id != Guid.Empty && knownIds.Contains(watering.Id))
                continue;

            watering.PlantId = plantId;
            watering.IsSynced = false; // à envoyer au cloud lors de la prochaine synchronisation

            var id = await AddWateringAsync(watering);

            knownIds.Add(id);
            added++;
        }

        return added;
    }

    // Les identifiants sont uniques dans tout le stockage de l'appareil, tous profils confondus :
    // si celui du fichier est déjà pris (autre profil, plante supprimée), on en attribue un nouveau.
    private async Task<Guid> AddPlantAsync(Plant plant)
    {
        if (plant.Id == Guid.Empty)
            plant.Id = Guid.NewGuid();

        try
        {
            await _plants.AddAsync(plant);
        }
        catch (Exception)
        {
            plant.Id = Guid.NewGuid();
            await _plants.AddAsync(plant);
        }

        return plant.Id;
    }

    private async Task<Guid> AddWateringAsync(Watering watering)
    {
        if (watering.Id == Guid.Empty)
            watering.Id = Guid.NewGuid();

        try
        {
            await _plants.AddWateringAsync(watering);
        }
        catch (Exception)
        {
            watering.Id = Guid.NewGuid();
            await _plants.AddWateringAsync(watering);
        }

        return watering.Id;
    }
}