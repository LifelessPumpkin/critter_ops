namespace skipper_api.Domain.EnclosureTimeline;

/// <summary>
/// Identifies the kind of operational history represented by a timeline event.
/// </summary>
public enum EnclosureTimelineEventType
{
    WaterTest,
    Cleaning,
    WaterChange,
    Inspection,
    Maintenance,
    Feeding,
    AnimalMovement,
    AnimalDisposition,
    Medication,
    Treatment,
    Note,
    General,
    Task,
    Other,
}
