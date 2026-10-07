namespace skipper_api.Domain.Activity;

/// <summary>
/// Result observed for an animal feeding.
/// </summary>
public enum AnimalFeedingResult
{
    Accepted,
    PartiallyAccepted,
    Refused,
    NotObserved,
    Other,
}
