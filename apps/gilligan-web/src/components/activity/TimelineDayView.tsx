import { TimelineCompactEvent } from "@/components/activity/TimelineCompactEvent";
import { TimelineEmptyState } from "@/components/activity/TimelineEmptyState";
import { TimelineViewControls } from "@/components/activity/TimelineViewControls";
import { addDays, localDateKey, toDateInputValue } from "@/components/activity/timelineDateUtils";
import type { ActivityRecord } from "@/lib/api/activity";

const dayFormatter = new Intl.DateTimeFormat("en", {
  weekday: "long",
  month: "long",
  day: "numeric",
  year: "numeric",
});
const hourFormatter = new Intl.DateTimeFormat("en", { hour: "numeric" });

type TimelineDayViewProps = {
  activities: ActivityRecord[];
  date: string;
  filtered: boolean;
  filteredMessage?: string;
  resultsComplete: boolean;
  onClear: () => void;
  onDateChange: (date: string) => void;
  onSelectActivity: (activity: ActivityRecord, trigger: HTMLElement) => void;
};

export function TimelineDayView({ activities, date, filtered, filteredMessage, resultsComplete, onClear, onDateChange, onSelectActivity }: TimelineDayViewProps) {
  const selectedActivities = activities
    .filter((activity) => localDateKey(activity.occurredAt) === date)
    .sort(compareActivitiesAscending);
  const hourGroups = groupByHour(selectedActivities);

  return (
    <div className="timeline-alternate-view timeline-day-view">
      <TimelineViewControls
        label={dayFormatter.format(new Date(`${date}T12:00:00`))}
        jumpLabel="Today"
        dateValue={date}
        onDateChange={onDateChange}
        onPrevious={() => onDateChange(addDays(date, -1))}
        onNext={() => onDateChange(addDays(date, 1))}
        onJump={() => onDateChange(toDateInputValue(new Date()))}
      />
      {!resultsComplete && selectedActivities.length ? <p className="timeline-partial-results" role="status">Additional activity for this day is loading as you scroll.</p> : null}
      {selectedActivities.length === 0 ? (
        <TimelineEmptyState
          message={`No activity recorded on ${dayFormatter.format(new Date(`${date}T12:00:00`))}.`}
          filtered={filtered}
          filteredMessage={filteredMessage}
          onClear={onClear}
        />
      ) : (
        <div className="timeline-day-axis" aria-label={`Activity on ${dayFormatter.format(new Date(`${date}T12:00:00`))}`}>
          {hourGroups.map((group) => (
            <section className="timeline-hour-group" key={group.hour} aria-label={hourFormatter.format(group.date)}>
              <time dateTime={`${date}T${String(group.hour).padStart(2, "0")}:00:00`}>{hourFormatter.format(group.date)}</time>
              <div className="timeline-hour-line" />
              <div className="timeline-hour-events">
                {group.activities.map((activity) => <TimelineCompactEvent activity={activity} key={activity.id} onSelectActivity={onSelectActivity} />)}
              </div>
            </section>
          ))}
        </div>
      )}
    </div>
  );
}

function groupByHour(activities: ActivityRecord[]) {
  const groups = new Map<number, ActivityRecord[]>();
  activities.forEach((activity) => {
    const hour = new Date(activity.occurredAt).getHours();
    groups.set(hour, [...(groups.get(hour) ?? []), activity]);
  });
  return Array.from(groups.entries()).map(([hour, groupedActivities]) => ({
    hour,
    date: new Date(2000, 0, 1, hour),
    activities: groupedActivities,
  }));
}

function compareActivitiesAscending(first: ActivityRecord, second: ActivityRecord) {
  return new Date(first.occurredAt).getTime() - new Date(second.occurredAt).getTime() || first.id - second.id;
}
