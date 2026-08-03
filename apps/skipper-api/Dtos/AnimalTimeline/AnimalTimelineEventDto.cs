using System.Text.Json;
using skipper_api.Domain.AnimalTimeline;

namespace skipper_api.Dtos.AnimalTimeline;

/// <summary>
/// API response shape for an animal timeline event.
/// </summary>
public class AnimalTimelineEventDto
{
    public long Id { get; set; }

    public int AnimalId { get; set; }

    public required string AnimalName { get; set; }

    public int EnclosureId { get; set; }

    public required string EnclosureName { get; set; }

    public AnimalTimelineEventType EventType { get; set; }

    public DateTime OccurredAt { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public string? PerformedBy { get; set; }

    public Guid? SourceReferenceId { get; set; }

    public string? SourceType { get; set; }

    public JsonElement? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
