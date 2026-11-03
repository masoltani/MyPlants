namespace MyPlants.Models;

public class Watering
{
    public string ProfileId { get; set; } = "guest";
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlantId { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public string? Note { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsSynced { get; set; } = false;
}
