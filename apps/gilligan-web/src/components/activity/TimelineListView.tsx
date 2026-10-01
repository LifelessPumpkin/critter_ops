import { ActivityTypeMark } from "@/components/activity/TimelineToolbar";
import { timelineColumns, type TimelineColumnId } from "@/components/activity/timelineTypes";
import { formatEnumLabel, type ActivityRecord, type ActivitySortDirection } from "@/lib/api/activity";

type TimelineListViewProps = {
  activities: ActivityRecord[];
  columns: TimelineColumnId[];
  sort: ActivitySortDirection;
};

type ActivityGroup = {
  dateKey: string;
  label: string;
  activities: ActivityRecord[];
};

const timeFormatter = new Intl.DateTimeFormat("en", { hour: "numeric", minute: "2-digit" });
const dateFormatter = new Intl.DateTimeFormat("en", { month: "long", day: "numeric", year: "numeric" });
const createdFormatter = new Intl.DateTimeFormat("en", {
  month: "short",
  day: "numeric",
  year: "numeric",
  hour: "numeric",
  minute: "2-digit",
});

export function TimelineListView({ activities, columns, sort }: TimelineListViewProps) {
  const groups = groupActivitiesByDate(activities, sort);
  const columnLabels = new Map(timelineColumns.map((column) => [column.id, column.label]));

  return (
    <div className="timeline-list" aria-label="Activity timeline">
      {groups.map((group) => (
        <section className="timeline-date-group" key={group.dateKey} aria-labelledby={`timeline-date-${group.dateKey}`}>
          <h2 id={`timeline-date-${group.dateKey}`} className="timeline-date-heading">{group.label}</h2>
          <div className="timeline-table-scroll">
            <table className="timeline-table">
              <thead>
                <tr>
                  {columns.map((column) => <th key={column} scope="col" className={`timeline-column-${column}`}>{columnLabels.get(column)}</th>)}
                </tr>
              </thead>
              <tbody>
                {group.activities.map((activity) => (
                  <tr key={activity.id} tabIndex={0} aria-label={`${formatEnumLabel(activity.type)} at ${timeFormatter.format(new Date(activity.occurredAt))}`}>
                    {columns.map((column) => (
                      <td key={column} className={`timeline-column-${column}`}>{renderCell(activity, column)}</td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      ))}
    </div>
  );
}

function renderCell(activity: ActivityRecord, column: TimelineColumnId) {
  switch (column) {
    case "time":
      return <time dateTime={activity.occurredAt}>{timeFormatter.format(new Date(activity.occurredAt))}</time>;
    case "type":
      return (
        <span className="timeline-event-type">
          <ActivityTypeMark type={activity.type} />
          <span>{formatEnumLabel(activity.type)}</span>
        </span>
      );
    case "animal":
      return <TruncatedValue value={formatAssociations(activity.animals)} />;
    case "enclosure":
      return <TruncatedValue value={formatAssociations(activity.enclosures)} />;
    case "details":
      return <TruncatedValue value={activity.description} primary />;
    case "performer":
      return <TruncatedValue value={activity.performer || "—"} />;
    case "activityId":
      return <span className="timeline-mono">{activity.id}</span>;
    case "createdAt":
      return <time dateTime={activity.createdAt}>{createdFormatter.format(new Date(activity.createdAt))}</time>;
  }
}

function TruncatedValue({ value, primary = false }: { value: string; primary?: boolean }) {
  return <span className={primary ? "timeline-truncate timeline-primary-value" : "timeline-truncate"} title={value}>{value}</span>;
}

function formatAssociations(associations: ActivityRecord["animals"] | ActivityRecord["enclosures"]) {
  return associations.length ? associations.map((association) => association.name).join(", ") : "—";
}

export function groupActivitiesByDate(activities: ActivityRecord[], sort: ActivitySortDirection): ActivityGroup[] {
  const sortedActivities = [...activities].sort((first, second) => {
    const dateDifference = new Date(first.occurredAt).getTime() - new Date(second.occurredAt).getTime();
    const idDifference = first.id - second.id;
    return sort === "Oldest" ? dateDifference || idDifference : -(dateDifference || idDifference);
  });
  const groups = new Map<string, ActivityRecord[]>();

  sortedActivities.forEach((activity) => {
    const date = new Date(activity.occurredAt);
    const dateKey = toLocalDateKey(date);
    groups.set(dateKey, [...(groups.get(dateKey) ?? []), activity]);
  });

  return Array.from(groups.entries()).map(([dateKey, groupedActivities]) => ({
    dateKey,
    label: formatDateGroupLabel(new Date(groupedActivities[0].occurredAt)),
    activities: groupedActivities,
  }));
}

function formatDateGroupLabel(date: Date) {
  const today = new Date();
  const yesterday = new Date();
  yesterday.setDate(today.getDate() - 1);

  if (toLocalDateKey(date) === toLocalDateKey(today)) {
    return "Today";
  }

  if (toLocalDateKey(date) === toLocalDateKey(yesterday)) {
    return "Yesterday";
  }

  return dateFormatter.format(date);
}

function toLocalDateKey(date: Date) {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
}
