using System.Text.Json;
using skipper_api.Domain.Animals;
using skipper_api.Domain.Enclosures;

namespace skipper_api.Domain.AnimalTimeline;

/// <summary>
/// Historical activity associated with an animal.
/// </summary>
public class AnimalTimelineEvent
{
    /// <summary>Primary key.</summary>
    public long Id { get; set; }

    /// <summary>Required animal foreign key.</summary>
    public int AnimalId { get; set; }

    /// <summary>Animal this event belongs to.</summary>
    public Animal Animal { get; set; } = null!;

    /// <summary>Required enclosure context for the event at the time it occurred.</summary>
    public int EnclosureId { get; set; }

    /// <summary>Enclosure associated with this event.</summary>
    public Enclosure Enclosure { get; set; } = null!;

    /// <summary>Kind of timeline event.</summary>
    public required AnimalTimelineEventType EventType { get; set; }

    /// <summary>UTC timestamp when the activity actually occurred.</summary>
    public required DateTime OccurredAt { get; set; }

    /// <summary>Short human-readable description.</summary>
    public required string Title { get; set; }

    /// <summary>Additional notes or context.</summary>
    public string? Description { get; set; }

    /// <summary>Human-readable actor name until user accounts are introduced.</summary>
    public string? PerformedBy { get; set; }

    /// <summary>Optional reference to the originating source record.</summary>
    public Guid? SourceReferenceId { get; set; }

    /// <summary>Optional source entity type name.</summary>
    public string? SourceType { get; set; }

    /// <summary>Event-specific values stored as PostgreSQL jsonb.</summary>
    public JsonDocument? Metadata { get; set; }

    /// <summary>UTC timestamp when this record was created.</summary>
    public required DateTime CreatedAt { get; set; }

    /// <summary>UTC timestamp when this record was last modified.</summary>
    public required DateTime UpdatedAt { get; set; }
}
