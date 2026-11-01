using System.Text.Json.Serialization;

namespace MyPlants.Dtos;

public class SupabasePlantDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("species")]
    public string? Species { get; set; }

    [JsonPropertyName("date_planted")]
    public DateTime? DatePlanted { get; set; }

    [JsonPropertyName("last_watered")]
    public DateTime? LastWatered { get; set; }

    [JsonPropertyName("is_archived")]
    public bool IsArchived { get; set; }

    [JsonPropertyName("watering_interval")]
    public int? WateringInterval { get; set; }
}
