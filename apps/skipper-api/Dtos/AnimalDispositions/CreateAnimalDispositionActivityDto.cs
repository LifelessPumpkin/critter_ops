using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using skipper_api.Domain.Activity;

namespace skipper_api.Dtos.AnimalDispositions;

/// <summary>
/// Request shape for creating an animal disposition activity.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class CreateAnimalDispositionActivityDto
{
    [Required]
    public AnimalDispositionType? DispositionType { get; set; }

    [Required]
    public DateTime? OccurredAt { get; set; }

    [MaxLength(150)]
    public string? Reason { get; set; }

    [MaxLength(150)]
    public string? RecipientOrDestination { get; set; }

    public string? Notes { get; set; }

    [MaxLength(100)]
    public string? PerformedBy { get; set; }
}
