namespace skipper_api.Domain.Activity;

/// <summary>
/// Structured details for an animal or enclosure inspection activity.
/// </summary>
public class InspectionActivity
{
    /// <summary>Activity event primary and foreign key.</summary>
    public long ActivityEventId { get; set; }

    /// <summary>Parent activity event.</summary>
    public ActivityEvent ActivityEvent { get; set; } = null!;

    /// <summary>Result of the inspection.</summary>
    public required InspectionResult Result { get; set; }
}
