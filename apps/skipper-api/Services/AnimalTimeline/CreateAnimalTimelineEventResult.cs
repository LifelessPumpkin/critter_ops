using skipper_api.Dtos.AnimalTimeline;

namespace skipper_api.Services.AnimalTimeline;

public enum CreateAnimalTimelineEventResultStatus
{
    Created,
    AnimalNotFound,
    EnclosureNotFound,
}

public class CreateAnimalTimelineEventResult
{
    public CreateAnimalTimelineEventResultStatus Status { get; private init; }

    public AnimalTimelineEventDto? TimelineEvent { get; private init; }

    public static CreateAnimalTimelineEventResult Created(AnimalTimelineEventDto timelineEvent)
    {
        return new CreateAnimalTimelineEventResult
        {
            Status = CreateAnimalTimelineEventResultStatus.Created,
            TimelineEvent = timelineEvent,
        };
    }

    public static CreateAnimalTimelineEventResult AnimalNotFound()
    {
        return new CreateAnimalTimelineEventResult
        {
            Status = CreateAnimalTimelineEventResultStatus.AnimalNotFound,
        };
    }

    public static CreateAnimalTimelineEventResult EnclosureNotFound()
    {
        return new CreateAnimalTimelineEventResult
        {
            Status = CreateAnimalTimelineEventResultStatus.EnclosureNotFound,
        };
    }
}
