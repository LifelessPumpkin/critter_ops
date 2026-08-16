using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace skipper_api.Dtos.AnimalMovements;

/// <summary>
/// Request shape for creating an animal movement activity.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class CreateAnimalMovementActivityDto
{
    [Required]
    public int? FromEnclosureId { get; set; }

    [Required]
    public int? ToEnclosureId { get; set; }

    [Required]
    public DateTime? OccurredAt { get; set; }

    [MaxLength(100)]
    public string? Reason { get; set; }

    public string? Notes { get; set; }

    [MaxLength(100)]
    public string? PerformedBy { get; set; }
}
