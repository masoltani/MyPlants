namespace MyPlants.Models;

/// <summary>Contenu d'un fichier de sauvegarde : les plantes du profil et leurs arrosages.</summary>
public sealed class BackupFile
{
    public const string AppName = "MyPlants";
    public const int CurrentFormatVersion = 1;

    public string App { get; set; } = AppName;

    public int FormatVersion { get; set; } = CurrentFormatVersion;

    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;

    public List<Plant> Plants { get; set; } = [];

    public List<Watering> Waterings { get; set; } = [];
}

/// <summary>Bilan d'une restauration.</summary>
/// <param name="PlantsAdded">Plantes ajoutées.</param>
/// <param name="PlantsAlreadyPresent">Plantes déjà présentes (non modifiées).</param>
/// <param name="WateringsAdded">Arrosages ajoutés.</param>
public sealed record BackupImportResult(int PlantsAdded, int PlantsAlreadyPresent, int WateringsAdded);