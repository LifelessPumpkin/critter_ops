using skipper_api.Domain.Enclosures;

namespace skipper_api.Domain.Activity;

/// <summary>
/// Associates an activity ledger event with an enclosure.
/// </summary>
public class ActivityEventEnclosure
{
    /// <summary>Activity event foreign key.</summary>
    public long ActivityEventId { get; set; }

    /// <summary>Activity event this association belongs to.</summary>
    public ActivityEvent ActivityEvent { get; set; } = null!;

    /// <summary>Enclosure foreign key.</summary>
    public int EnclosureId { get; set; }

    /// <summary>Enclosure associated with the event.</summary>
    public Enclosure Enclosure { get; set; } = null!;

    /// <summary>How the enclosure relates to the event.</summary>
    public required ActivityEventEnclosureRelationshipType RelationshipType { get; set; }
}
