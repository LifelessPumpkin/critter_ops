namespace skipper_api.Domain.Activity;

/// <summary>
/// Structured details for an enclosure cleaning activity.
/// </summary>
public class EnclosureCleaningActivity
{
    /// <summary>Activity event primary and foreign key.</summary>
    public long ActivityEventId { get; set; }

    /// <summary>Parent activity event.</summary>
    public ActivityEvent ActivityEvent { get; set; } = null!;

    /// <summary>Type of cleaning performed.</summary>
    public required EnclosureCleaningType CleaningType { get; set; }

    /// <summary>Optional water change percentage, from 0 to 100.</summary>
    public decimal? WaterChangePercent { get; set; }

    /// <summary>Whether substrate was changed during cleaning.</summary>
    public bool SubstrateChanged { get; set; }

    /// <summary>Optional simple description of equipment cleaned.</summary>
    public string? EquipmentCleaned { get; set; }
}
