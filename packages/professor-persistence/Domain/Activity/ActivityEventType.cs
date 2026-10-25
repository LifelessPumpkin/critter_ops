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
    WaterChange,
    Inspection,
    Maintenance,
    WaterTest,
    Task,
    Note,
    General,
    Other,
}
