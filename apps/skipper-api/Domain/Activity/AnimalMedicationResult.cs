namespace skipper_api.Domain.Activity;

/// <summary>
/// Result observed for a medication administration.
/// </summary>
public enum AnimalMedicationResult
{
    Administered,
    PartiallyAdministered,
    Refused,
    Vomited,
    AdverseReaction,
    Other,
}
