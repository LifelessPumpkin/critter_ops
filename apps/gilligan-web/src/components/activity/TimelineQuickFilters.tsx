import { toDateInputValue } from "@/components/activity/timelineDateUtils";
import type { TimelineFilters } from "@/components/activity/timelineTypes";
import type { ActivityEventType } from "@/lib/api/activity";

type TimelineQuickFiltersProps = {
  filters: TimelineFilters;
  onChange: (filters: TimelineFilters) => void;
};

const typeFilters: Array<{ label: string; value: ActivityEventType }> = [
  { label: "Feedings", value: "Feeding" },
  { label: "Treatments", value: "Treatment" },
  { label: "Cleaning", value: "Cleaning" },
  { label: "Tasks", value: "Task" },
];

export function TimelineQuickFilters({ filters, onChange }: TimelineQuickFiltersProps) {
  function setDateRange(daysAgo: number, length: number) {
    const to = new Date();
    to.setDate(to.getDate() - daysAgo);
    const from = new Date(to);
    from.setDate(from.getDate() - (length - 1));
    onChange({ ...filters, from: toDateInputValue(from), to: toDateInputValue(to) });
  }

  function toggleType(eventType: ActivityEventType) {
    const eventTypes = filters.eventTypes.includes(eventType)
      ? filters.eventTypes.filter((value) => value !== eventType)
      : [...filters.eventTypes, eventType];
    onChange({ ...filters, eventTypes });
  }

  const today = toDateInputValue(new Date());
  const yesterdayDate = new Date();
  yesterdayDate.setDate(yesterdayDate.getDate() - 1);
  const yesterday = toDateInputValue(yesterdayDate);
  const weekStartDate = new Date();
  weekStartDate.setDate(weekStartDate.getDate() - 6);
  const weekStart = toDateInputValue(weekStartDate);

  return (
    <div className="timeline-quick-filters" aria-label="Quick filters">
      <span>Quick filters</span>
      <button type="button" aria-pressed={filters.from === today && filters.to === today} onClick={() => setDateRange(0, 1)}>Today</button>
      <button type="button" aria-pressed={filters.from === yesterday && filters.to === yesterday} onClick={() => setDateRange(1, 1)}>Yesterday</button>
      <button type="button" aria-pressed={filters.from === weekStart && filters.to === today} onClick={() => setDateRange(0, 7)}>Last 7 days</button>
      {typeFilters.map((filter) => (
        <button
          type="button"
          key={filter.value}
          aria-pressed={filters.eventTypes.includes(filter.value)}
          onClick={() => toggleType(filter.value)}
        >
          {filter.label}
        </button>
      ))}
    </div>
  );
}
