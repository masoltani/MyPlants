namespace MyPlants.Models;

public class Watering
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PlantId { get; set; }

    public DateTime Date { get; set; } = DateTime.Now;

    public string? Note { get; set; }
}
