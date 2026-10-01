import { ActivityTypeMark } from "@/components/activity/TimelineToolbar";
import { formatAssociationNames } from "@/components/activity/timelineDateUtils";
import { formatEnumLabel, type ActivityRecord } from "@/lib/api/activity";

const timeFormatter = new Intl.DateTimeFormat("en", { hour: "numeric", minute: "2-digit" });

export function TimelineCompactEvent({ activity, showTime = true }: { activity: ActivityRecord; showTime?: boolean }) {
  const metadata = [
    formatAssociationNames(activity.animals),
    formatAssociationNames(activity.enclosures),
    activity.performer,
  ].filter((value) => value && value !== "—").join(" · ");

  return (
    <div
      className="timeline-compact-event"
      tabIndex={0}
      aria-label={`${formatEnumLabel(activity.type)} at ${timeFormatter.format(new Date(activity.occurredAt))}`}
    >
      {showTime ? <time dateTime={activity.occurredAt}>{timeFormatter.format(new Date(activity.occurredAt))}</time> : null}
      <ActivityTypeMark type={activity.type} />
      <div className="timeline-compact-event-content">
        <strong>{formatEnumLabel(activity.type)}</strong>
        <span className="timeline-truncate" title={activity.description}>{activity.description}</span>
        {metadata ? <small className="timeline-truncate" title={metadata}>{metadata}</small> : null}
      </div>
    </div>
  );
}
