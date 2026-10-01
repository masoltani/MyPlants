namespace MyPlants.Models;

public class Plant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Name { get; set; }
    public string? Species { get; set; }
    public DateTime? DatePlanted { get; set; }
    public bool IsArchived { get; set; }
    public int? WateringInterval { get; set; } // in days
    public List<Water>? WateringHistory { get; set; } = [];
}
