import { ActivityTypeMark } from "@/components/activity/TimelineToolbar";
import { TimelineEmptyState } from "@/components/activity/TimelineEmptyState";
import { TimelineViewControls } from "@/components/activity/TimelineViewControls";
import { addDays, daysBetween, fromDateInputValue, localDateKey } from "@/components/activity/timelineDateUtils";
import { formatEnumLabel, type ActivityRecord } from "@/lib/api/activity";
import type { RangeGroupBy } from "@/components/activity/timelineTypes";

const rangeStartFormatter = new Intl.DateTimeFormat("en", { month: "short", day: "numeric" });
const rangeEndFormatter = new Intl.DateTimeFormat("en", { month: "short", day: "numeric", year: "numeric" });
const bucketFormatter = new Intl.DateTimeFormat("en", { month: "short", day: "numeric" });
const timeFormatter = new Intl.DateTimeFormat("en", { month: "short", day: "numeric", hour: "numeric", minute: "2-digit" });

type TimelineRangeViewProps = {
  activities: ActivityRecord[];
  filtered: boolean;
  from: string;
  groupBy: RangeGroupBy;
  rangeDays: number;
  to: string;
  customRange: boolean;
  onClear: () => void;
  onNavigate: (direction: -1 | 1) => void;
  onRangeDaysChange: (days: number) => void;
  onUseCustomRange: () => void;
  onJumpToCurrent: () => void;
};

type RangeGroup = {
  id: string;
  label: string;
  activities: ActivityRecord[];
};

type DateBucket = {
  id: string;
  label: string;
  from: string;
  to: string;
};

export function TimelineRangeView({
  activities,
  filtered,
  from,
  groupBy,
  rangeDays,
  to,
  customRange,
  onClear,
  onNavigate,
  onRangeDaysChange,
  onUseCustomRange,
  onJumpToCurrent,
}: TimelineRangeViewProps) {
  const groups = groupActivities(activities, groupBy);
  const buckets = createBuckets(from, to);
  const label = `${rangeStartFormatter.format(fromDateInputValue(from))} – ${rangeEndFormatter.format(fromDateInputValue(to))}`;

  return (
    <div className="timeline-alternate-view timeline-range-view">
      <TimelineViewControls
        label={label}
        jumpLabel="Current range"
        onPrevious={() => onNavigate(-1)}
        onNext={() => onNavigate(1)}
        onJump={onJumpToCurrent}
      />
      <div className="timeline-range-presets" aria-label="Range window">
        {[7, 30, 90].map((days) => (
          <button
            type="button"
            key={days}
            className={!customRange && rangeDays === days ? "timeline-range-preset-active" : undefined}
            aria-pressed={!customRange && rangeDays === days}
            onClick={() => onRangeDaysChange(days)}
          >
            {days} days
          </button>
        ))}
        <button
          type="button"
          className={customRange ? "timeline-range-preset-active" : undefined}
          aria-pressed={customRange}
          onClick={onUseCustomRange}
        >
          Custom
        </button>
      </div>
      {activities.length === 0 ? (
        <TimelineEmptyState message="No activity found in this date range." filtered={filtered} onClear={onClear} />
      ) : (
        <div className="timeline-range-scroll">
          <div
            className="timeline-range-grid"
            style={{ gridTemplateColumns: `minmax(9rem, 12rem) repeat(${buckets.length}, minmax(3.25rem, 1fr))` }}
            role="grid"
            aria-label={`Activity grouped by ${groupBy}`}
          >
            <div className="timeline-range-corner" role="columnheader">{getGroupLabel(groupBy)}</div>
            {buckets.map((bucket) => <div className="timeline-range-axis-label" role="columnheader" key={bucket.id}>{bucket.label}</div>)}
            {groups.map((group) => (
              <RangeRow group={group} buckets={buckets} key={group.id} />
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

function RangeRow({ group, buckets }: { group: RangeGroup; buckets: DateBucket[] }) {
  return (
    <>
      <div className="timeline-range-group-label" role="rowheader" title={group.label}>{group.label}</div>
      {buckets.map((bucket) => {
        const bucketActivities = group.activities.filter((activity) => {
          const date = localDateKey(activity.occurredAt);
          return date >= bucket.from && date <= bucket.to;
        });
        return (
          <div className="timeline-range-cell" role="gridcell" key={bucket.id}>
            {bucketActivities.length ? <ActivityCluster activities={bucketActivities} label={`${group.label}, ${bucket.label}`} /> : null}
          </div>
        );
      })}
    </>
  );
}

function ActivityCluster({ activities, label }: { activities: ActivityRecord[]; label: string }) {
  return (
    <details className="timeline-cluster">
      <summary aria-label={`${activities.length} activities for ${label}`}>
        <span aria-hidden="true">{activities.length === 1 ? "" : activities.length}</span>
      </summary>
      <div className="timeline-cluster-panel">
        <strong>{activities.length} {activities.length === 1 ? "activity" : "activities"}</strong>
        <div className="timeline-cluster-list">
          {activities.slice(0, 8).map((activity) => (
            <div className="timeline-cluster-event" key={activity.id}>
              <ActivityTypeMark type={activity.type} />
              <div>
                <strong>{formatEnumLabel(activity.type)}</strong>
                <time dateTime={activity.occurredAt}>{timeFormatter.format(new Date(activity.occurredAt))}</time>
                <span title={activity.description}>{activity.description}</span>
              </div>
            </div>
          ))}
          {activities.length > 8 ? <small>+ {activities.length - 8} additional activities</small> : null}
        </div>
      </div>
    </details>
  );
}

function groupActivities(activities: ActivityRecord[], groupBy: RangeGroupBy) {
  const groups = new Map<string, RangeGroup>();
  activities.forEach((activity) => {
    const entries = getGroupEntries(activity, groupBy);
    entries.forEach(({ id, label }) => {
      const current = groups.get(id) ?? { id, label, activities: [] };
      current.activities.push(activity);
      groups.set(id, current);
    });
  });
  return Array.from(groups.values()).sort((first, second) => first.label.localeCompare(second.label));
}

function getGroupEntries(activity: ActivityRecord, groupBy: RangeGroupBy) {
  if (groupBy === "animal") {
    return activity.animals.length
      ? activity.animals.map((animal) => ({ id: `animal-${animal.id}`, label: animal.name }))
      : [{ id: "animal-none", label: "No animal" }];
  }
  if (groupBy === "enclosure") {
    return activity.enclosures.length
      ? activity.enclosures.map((enclosure) => ({ id: `enclosure-${enclosure.id}`, label: enclosure.name }))
      : [{ id: "enclosure-none", label: "No enclosure" }];
  }
  return [{ id: `type-${activity.type}`, label: formatEnumLabel(activity.type) }];
}

function createBuckets(from: string, to: string) {
  const duration = daysBetween(from, to);
  const bucketSize = duration > 45 ? 7 : 1;
  const buckets: DateBucket[] = [];
  for (let offset = 0; offset < duration; offset += bucketSize) {
    const bucketFrom = addDays(from, offset);
    const bucketTo = addDays(from, Math.min(duration - 1, offset + bucketSize - 1));
    const label = bucketSize === 1
      ? bucketFormatter.format(fromDateInputValue(bucketFrom))
      : `${bucketFormatter.format(fromDateInputValue(bucketFrom))}–${bucketFormatter.format(fromDateInputValue(bucketTo))}`;
    buckets.push({ id: bucketFrom, label, from: bucketFrom, to: bucketTo });
  }
  return buckets;
}

function getGroupLabel(groupBy: RangeGroupBy) {
  if (groupBy === "animal") return "Animal";
  if (groupBy === "enclosure") return "Enclosure";
  return "Event type";
}
