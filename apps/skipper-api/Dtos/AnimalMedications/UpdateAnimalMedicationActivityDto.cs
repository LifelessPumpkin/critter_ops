using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using skipper_api.Domain.Activity;

namespace skipper_api.Dtos.AnimalMedications;

/// <summary>
/// Request shape for updating an animal medication activity.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class UpdateAnimalMedicationActivityDto
{
    [Required]
    [MaxLength(150)]
    public string? MedicationName { get; set; }

    [Required]
    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal? Dose { get; set; }

    [Required]
    [MaxLength(50)]
    public string? DoseUnit { get; set; }

    [Required]
    public AnimalMedicationRoute? Route { get; set; }

    public AnimalMedicationResult? Result { get; set; }

    [Required]
    public DateTime? OccurredAt { get; set; }

    public string? Notes { get; set; }

    [MaxLength(100)]
    public string? PerformedBy { get; set; }
}
