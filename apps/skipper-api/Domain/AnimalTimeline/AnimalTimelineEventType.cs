namespace skipper_api.Domain.AnimalTimeline;

/// <summary>
/// Identifies the kind of animal history represented by a timeline event.
/// </summary>
public enum AnimalTimelineEventType
{
    Feeding,
    AnimalMovement,
    AnimalDisposition,
    Treatment,
    Note,
    Task,
    Other,
}
