namespace skipper_api.Domain.Activity;

/// <summary>
/// Structured details for an animal feeding activity.
/// </summary>
public class AnimalFeedingActivity
{
    /// <summary>Activity event primary and foreign key.</summary>
    public long ActivityEventId { get; set; }

    /// <summary>Parent activity event.</summary>
    public ActivityEvent ActivityEvent { get; set; } = null!;

    /// <summary>Required bounded description of food offered.</summary>
    public required string Food { get; set; }

    /// <summary>Quantity of food offered.</summary>
    public required decimal Quantity { get; set; }

    /// <summary>Unit for the quantity offered.</summary>
    public required AnimalFeedingQuantityUnit Unit { get; set; }

    /// <summary>Observed feeding result.</summary>
    public required AnimalFeedingResult Result { get; set; }
}
