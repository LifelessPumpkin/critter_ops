namespace skipper_api.Domain.Activity;

/// <summary>
/// Structured details for enclosure or operational maintenance.
/// </summary>
public class MaintenanceActivity
{
    /// <summary>Activity event primary and foreign key.</summary>
    public long ActivityEventId { get; set; }

    /// <summary>Parent activity event.</summary>
    public ActivityEvent ActivityEvent { get; set; } = null!;

    /// <summary>Description of the maintenance work performed.</summary>
    public required string Description { get; set; }
}
