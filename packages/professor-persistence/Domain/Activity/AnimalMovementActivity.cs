using skipper_api.Domain.Enclosures;

namespace skipper_api.Domain.Activity;

/// <summary>
/// Structured details for an animal movement activity.
/// </summary>
public class AnimalMovementActivity
{
    /// <summary>Activity event primary and foreign key.</summary>
    public long ActivityEventId { get; set; }

    /// <summary>Parent activity event.</summary>
    public ActivityEvent ActivityEvent { get; set; } = null!;

    /// <summary>Source enclosure foreign key.</summary>
    public int FromEnclosureId { get; set; }

    /// <summary>Source enclosure.</summary>
    public Enclosure FromEnclosure { get; set; } = null!;

    /// <summary>Destination enclosure foreign key.</summary>
    public int ToEnclosureId { get; set; }

    /// <summary>Destination enclosure.</summary>
    public Enclosure ToEnclosure { get; set; } = null!;

    /// <summary>Reason for the movement.</summary>
    public string? Reason { get; set; }
}
