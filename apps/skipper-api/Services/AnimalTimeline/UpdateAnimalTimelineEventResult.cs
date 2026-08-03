using skipper_api.Dtos.AnimalTimeline;

namespace skipper_api.Services.AnimalTimeline;

public enum UpdateAnimalTimelineEventResultStatus
{
    Updated,
    NotFound,
    EnclosureNotFound,
}

public class UpdateAnimalTimelineEventResult
{
    public UpdateAnimalTimelineEventResultStatus Status { get; private init; }

    public AnimalTimelineEventDto? TimelineEvent { get; private init; }

    public static UpdateAnimalTimelineEventResult Updated(AnimalTimelineEventDto timelineEvent)
    {
        return new UpdateAnimalTimelineEventResult
        {
            Status = UpdateAnimalTimelineEventResultStatus.Updated,
            TimelineEvent = timelineEvent,
        };
    }

    public static UpdateAnimalTimelineEventResult NotFound()
    {
        return new UpdateAnimalTimelineEventResult
        {
            Status = UpdateAnimalTimelineEventResultStatus.NotFound,
        };
    }

    public static UpdateAnimalTimelineEventResult EnclosureNotFound()
    {
        return new UpdateAnimalTimelineEventResult
        {
            Status = UpdateAnimalTimelineEventResultStatus.EnclosureNotFound,
        };
    }
}
