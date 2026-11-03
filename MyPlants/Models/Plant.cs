using System.ComponentModel.DataAnnotations;

using MyPlants.Resources;

namespace MyPlants.Models;

public class Plant : IValidatableObject
{
    private const int MinWateringInterval = 1;
    private const int MaxWateringInterval = 365;

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

    // Règles de saisie des formulaires. Les messages viennent des ressources de langue
    // (les attributs [Required] / [Range] ne peuvent pas être traduits dynamiquement).
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult(
                SharedResource.Validation_NameRequired,
                [nameof(Name)]);
        }

        if (WateringInterval is < MinWateringInterval or > MaxWateringInterval)
        {
            yield return new ValidationResult(
                SharedResource.Validation_WateringIntervalRange,
                [nameof(WateringInterval)]);
        }
    }
}