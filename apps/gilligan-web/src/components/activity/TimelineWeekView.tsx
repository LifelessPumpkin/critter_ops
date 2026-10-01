import { TimelineCompactEvent } from "@/components/activity/TimelineCompactEvent";
import { TimelineEmptyState } from "@/components/activity/TimelineEmptyState";
import { TimelineViewControls } from "@/components/activity/TimelineViewControls";
import { addDays, getWeekDates, localDateKey, startOfWeek, toDateInputValue } from "@/components/activity/timelineDateUtils";
import type { ActivityRecord, ActivitySortDirection } from "@/lib/api/activity";

const weekdayFormatter = new Intl.DateTimeFormat("en", { weekday: "short" });
const dayFormatter = new Intl.DateTimeFormat("en", { day: "numeric" });
const rangeStartFormatter = new Intl.DateTimeFormat("en", { month: "short", day: "numeric" });
const rangeEndFormatter = new Intl.DateTimeFormat("en", { month: "short", day: "numeric", year: "numeric" });
const visibleActivityLimit = 5;

type TimelineWeekViewProps = {
  activities: ActivityRecord[];
  date: string;
  filtered: boolean;
  filteredMessage?: string;
  sort: ActivitySortDirection;
  onClear: () => void;
  onDateChange: (date: string) => void;
  onOpenDay: (date: string) => void;
  onSelectActivity: (activity: ActivityRecord, trigger: HTMLElement) => void;
};

export function TimelineWeekView({ activities, date, filtered, filteredMessage, sort, onClear, onDateChange, onOpenDay, onSelectActivity }: TimelineWeekViewProps) {
  const weekDates = getWeekDates(date);
  const firstDate = weekDates[0];
  const lastDate = weekDates[6];

  return (
    <div className="timeline-alternate-view timeline-week-view">
      <TimelineViewControls
        label={`${rangeStartFormatter.format(new Date(`${firstDate}T12:00:00`))} – ${rangeEndFormatter.format(new Date(`${lastDate}T12:00:00`))}`}
        jumpLabel="This week"
        onPrevious={() => onDateChange(addDays(date, -7))}
        onNext={() => onDateChange(addDays(date, 7))}
        onJump={() => onDateChange(toDateInputValue(new Date()))}
      />
      {activities.length === 0 ? (
        <TimelineEmptyState message="No activity recorded this week." filtered={filtered} filteredMessage={filteredMessage} onClear={onClear} />
      ) : (
        <div className="timeline-week-scroll">
          <div className="timeline-week-grid" role="grid" aria-label="Weekly activity">
            {weekDates.map((weekDate) => {
              const dayActivities = activities
                .filter((activity) => localDateKey(activity.occurredAt) === weekDate)
                .sort((first, second) => compareActivities(first, second, sort));
              const overflowCount = Math.max(0, dayActivities.length - visibleActivityLimit);
              const dateObject = new Date(`${weekDate}T12:00:00`);

              return (
                <section className="timeline-week-day" key={weekDate} role="gridcell" aria-label={`${weekdayFormatter.format(dateObject)}, ${dayActivities.length} activities`}>
                  <header className={weekDate === toDateInputValue(new Date()) ? "timeline-week-day-today" : undefined}>
                    <span>{weekdayFormatter.format(dateObject)}</span>
                    <strong>{dayFormatter.format(dateObject)}</strong>
                    <small>{dayActivities.length} {dayActivities.length === 1 ? "activity" : "activities"}</small>
                  </header>
                  <div className="timeline-week-events">
                    {dayActivities.slice(0, visibleActivityLimit).map((activity) => (
                      <TimelineCompactEvent activity={activity} key={activity.id} onSelectActivity={onSelectActivity} />
                    ))}
                    {overflowCount > 0 ? (
                      <button type="button" className="timeline-week-more" onClick={() => onOpenDay(weekDate)}>
                        + {overflowCount} more
                      </button>
                    ) : null}
                    {dayActivities.length === 0 ? <span className="timeline-week-none">No activity</span> : null}
                  </div>
                </section>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}

function compareActivities(first: ActivityRecord, second: ActivityRecord, sort: ActivitySortDirection) {
  const difference = new Date(first.occurredAt).getTime() - new Date(second.occurredAt).getTime() || first.id - second.id;
  return sort === "Oldest" ? difference : -difference;
}

export function getWeekWindow(date: string) {
  const from = startOfWeek(date);
  return { from, to: addDays(from, 6) };
}
