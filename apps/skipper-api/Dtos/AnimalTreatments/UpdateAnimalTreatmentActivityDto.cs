using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using skipper_api.Domain.Activity;

namespace skipper_api.Dtos.AnimalTreatments;

/// <summary>
/// Request shape for updating an animal treatment activity.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class UpdateAnimalTreatmentActivityDto
{
    [MaxLength(100)]
    public string? TreatmentType { get; set; }

    [Required]
    [MaxLength(150)]
    public string? TreatmentName { get; set; }

    public AnimalTreatmentResult? Result { get; set; }

    [Required]
    public DateTime? OccurredAt { get; set; }

    public string? Notes { get; set; }

    [MaxLength(100)]
    public string? PerformedBy { get; set; }
}
