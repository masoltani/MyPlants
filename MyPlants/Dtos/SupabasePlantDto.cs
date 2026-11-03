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

    /// <summary>Heure de la dernière modification faite par l'utilisateur.</summary>
    [JsonPropertyName("client_updated_at")]
    public DateTime? ClientUpdatedAt { get; set; }

    /// <summary>Heure du serveur, posée par le trigger.</summary>
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}