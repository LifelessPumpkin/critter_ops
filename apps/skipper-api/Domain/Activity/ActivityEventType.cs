namespace skipper_api.Domain.Activity;

/// <summary>
/// Identifies the broad category of activity represented by a ledger event.
/// </summary>
public enum ActivityEventType
{
    Feeding,
    AnimalMovement,
    Medication,
    Treatment,
    AnimalDisposition,
    Cleaning,
    WaterTest,
    Task,
    Note,
    Other,
}
