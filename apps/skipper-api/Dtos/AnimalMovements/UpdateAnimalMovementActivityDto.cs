using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace skipper_api.Dtos.AnimalMovements;

/// <summary>
/// Request shape for updating movement descriptive fields.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class UpdateAnimalMovementActivityDto
{
    [Required]
    public DateTime? OccurredAt { get; set; }

    [MaxLength(100)]
    public string? Reason { get; set; }

    public string? Notes { get; set; }

    [MaxLength(100)]
    public string? PerformedBy { get; set; }
}
