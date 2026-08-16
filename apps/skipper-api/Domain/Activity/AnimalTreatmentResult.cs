namespace skipper_api.Domain.Activity;

/// <summary>
/// Result observed for a treatment or procedure.
/// </summary>
public enum AnimalTreatmentResult
{
    Completed,
    PartiallyCompleted,
    NotCompleted,
    NotObserved,
    Other,
}
