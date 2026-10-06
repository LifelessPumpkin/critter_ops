namespace skipper_api.Domain.Activity;

/// <summary>
/// Structured details for an enclosure water change activity.
/// </summary>
public class EnclosureWaterChangeActivity
{
    /// <summary>Activity event primary and foreign key.</summary>
    public long ActivityEventId { get; set; }

    /// <summary>Parent activity event.</summary>
    public ActivityEvent ActivityEvent { get; set; } = null!;

    /// <summary>Optional percentage of enclosure water replaced, from 0 to 100.</summary>
    public decimal? WaterChangePercent { get; set; }
}
