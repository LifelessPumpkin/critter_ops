namespace skipper_api.Domain.Activity;

/// <summary>
/// Type of disposition that moved an animal out of active care or marked end of life.
/// </summary>
public enum AnimalDispositionType
{
    Sold,
    Surrendered,
    Transferred,
    Released,
    Deceased,
    Other,
}
