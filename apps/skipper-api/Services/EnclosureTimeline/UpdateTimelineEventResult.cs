using skipper_api.Dtos.EnclosureTimeline;

namespace skipper_api.Services.EnclosureTimeline;

public enum UpdateTimelineEventResultStatus
{
    Updated,
    NotFound,
}

public class UpdateTimelineEventResult
{
    public UpdateTimelineEventResultStatus Status { get; private init; }

    public EnclosureTimelineEventDto? TimelineEvent { get; private init; }

    public static UpdateTimelineEventResult Updated(EnclosureTimelineEventDto timelineEvent)
    {
        return new UpdateTimelineEventResult
        {
            Status = UpdateTimelineEventResultStatus.Updated,
            TimelineEvent = timelineEvent,
        };
    }

    public static UpdateTimelineEventResult NotFound()
    {
        return new UpdateTimelineEventResult
        {
            Status = UpdateTimelineEventResultStatus.NotFound,
        };
    }
}
