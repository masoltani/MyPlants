namespace MyPlants.Models;

public class Plant
{
    public string ProfileId { get; set; } = "guest";
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Name { get; set; }
    public string? Species { get; set; }
    public DateTime? DatePlanted { get; set; }
    public DateTime? LastWatered { get; set; }
    public bool IsArchived { get; set; }
    public int? WateringInterval { get; set; } // in days
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
    public bool IsSynced { get; set; } = false;
}
