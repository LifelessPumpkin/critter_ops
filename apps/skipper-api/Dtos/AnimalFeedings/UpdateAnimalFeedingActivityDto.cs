using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using skipper_api.Domain.Activity;

namespace skipper_api.Dtos.AnimalFeedings;

/// <summary>
/// Request shape for updating an animal feeding activity.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class UpdateAnimalFeedingActivityDto
{
    [Required]
    [MaxLength(150)]
    public string? Food { get; set; }

    [Required]
    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335")]
    public decimal? Quantity { get; set; }

    [Required]
    public AnimalFeedingQuantityUnit? Unit { get; set; }

    [Required]
    public AnimalFeedingResult? Result { get; set; }

    [Required]
    public DateTime? OccurredAt { get; set; }

    public string? Notes { get; set; }

    [MaxLength(100)]
    public string? PerformedBy { get; set; }
}
