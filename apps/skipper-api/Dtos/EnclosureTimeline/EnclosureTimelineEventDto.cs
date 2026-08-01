using System.Text.Json;
using skipper_api.Domain.EnclosureTimeline;

namespace skipper_api.Dtos.EnclosureTimeline;

/// <summary>
/// API response shape for an enclosure timeline event.
/// </summary>
public class EnclosureTimelineEventDto
{
    public long Id { get; set; }

    public int EnclosureId { get; set; }

    public EnclosureTimelineEventType EventType { get; set; }

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
