namespace skipper_api.Domain.Activity;

/// <summary>
/// Structured details for an animal disposition activity.
/// </summary>
public class AnimalDispositionActivity
{
    /// <summary>Activity event primary and foreign key.</summary>
    public long ActivityEventId { get; set; }

    /// <summary>Parent activity event.</summary>
    public ActivityEvent ActivityEvent { get; set; } = null!;

    /// <summary>Type of disposition recorded.</summary>
    public required AnimalDispositionType DispositionType { get; set; }

    /// <summary>Optional reason for the disposition.</summary>
    public string? Reason { get; set; }

    /// <summary>Optional recipient, destination, release location, or related party.</summary>
    public string? RecipientOrDestination { get; set; }
}
