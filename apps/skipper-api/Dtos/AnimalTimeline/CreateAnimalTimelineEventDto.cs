using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using skipper_api.Domain.AnimalTimeline;

namespace skipper_api.Dtos.AnimalTimeline;

/// <summary>
/// Request shape for creating an animal timeline event.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class CreateAnimalTimelineEventDto
{
    [Required]
    public int? EnclosureId { get; set; }

    [Required]
    public AnimalTimelineEventType? EventType { get; set; }

    [Required]
    public DateTime? OccurredAt { get; set; }

    [Required]
    [MaxLength(150)]
    public string? Title { get; set; }

    public string? Description { get; set; }

    [MaxLength(100)]
    public string? PerformedBy { get; set; }

    public Guid? SourceReferenceId { get; set; }

    [MaxLength(100)]
    public string? SourceType { get; set; }

    public JsonElement? Metadata { get; set; }
}
