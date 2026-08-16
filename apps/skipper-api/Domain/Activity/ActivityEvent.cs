using System.Text.Json;

namespace skipper_api.Domain.Activity;

/// <summary>
/// Authoritative historical ledger entry for one real-world CritterOps activity.
/// </summary>
public class ActivityEvent
{
    /// <summary>Primary key.</summary>
    public long Id { get; set; }

    /// <summary>Broad category of activity.</summary>
    public required ActivityEventType EventType { get; set; }

    /// <summary>UTC timestamp when the activity actually occurred.</summary>
    public required DateTime OccurredAt { get; set; }

    /// <summary>Short human-readable description.</summary>
    public required string Title { get; set; }

    /// <summary>Additional notes or context.</summary>
    public string? Notes { get; set; }

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

    /// <summary>Animals associated with this event.</summary>
    public ICollection<ActivityEventAnimal> Animals { get; set; } = [];

    /// <summary>Enclosures associated with this event.</summary>
    public ICollection<ActivityEventEnclosure> Enclosures { get; set; } = [];

    /// <summary>Structured animal movement detail, when this event is an animal movement.</summary>
    public AnimalMovementActivity? AnimalMovement { get; set; }

    /// <summary>Structured animal feeding detail, when this event is a feeding.</summary>
    public AnimalFeedingActivity? AnimalFeeding { get; set; }

    /// <summary>Structured animal disposition detail, when this event is a disposition.</summary>
    public AnimalDispositionActivity? AnimalDisposition { get; set; }
}
