using skipper_api.Dtos.EnclosureTimeline;

namespace skipper_api.Services.EnclosureTimeline;

public enum CreateTimelineEventResultStatus
{
    Created,
    EnclosureNotFound,
    UnsupportedEventType,
}

public class CreateTimelineEventResult
{
    public CreateTimelineEventResultStatus Status { get; private init; }

    public EnclosureTimelineEventDto? TimelineEvent { get; private init; }

    public static CreateTimelineEventResult Created(EnclosureTimelineEventDto timelineEvent)
    {
        return new CreateTimelineEventResult
        {
            Status = CreateTimelineEventResultStatus.Created,
            TimelineEvent = timelineEvent,
        };
    }

    public static CreateTimelineEventResult EnclosureNotFound()
    {
        return new CreateTimelineEventResult
        {
            Status = CreateTimelineEventResultStatus.EnclosureNotFound,
        };
    }

    public static CreateTimelineEventResult UnsupportedEventType()
    {
        return new CreateTimelineEventResult
        {
            Status = CreateTimelineEventResultStatus.UnsupportedEventType,
        };
    }
}
