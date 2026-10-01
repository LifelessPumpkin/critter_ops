import { activityEventTypes, activitySortOptions, type ActivityEventType } from "@/lib/api/activity";
import type { RangeGroupBy, TimelineInitialState, TimelineView } from "@/components/activity/timelineTypes";

export type TimelineSearchParams = Record<string, string | string[] | undefined>;

export function parseTimelineInitialState(searchParams: TimelineSearchParams): TimelineInitialState {
  const view = parseView(getValue(searchParams.view));
  const sortValue = getValue(searchParams.sort)?.toLowerCase();
  const groupValue = getValue(searchParams.group)?.toLowerCase();
  const eventTypes = (getValue(searchParams.type) ?? "")
    .split(",")
    .map((value) => activityEventTypes.find((eventType) => eventType.toLowerCase() === value.toLowerCase()))
    .filter((value): value is ActivityEventType => Boolean(value));

  return {
    view,
    date: parseDate(getValue(searchParams.date)),
    search: getValue(searchParams.search),
    sort: activitySortOptions.find((option) => option.value.toLowerCase() === sortValue)?.value,
    groupBy: groupValue === "animal" || groupValue === "enclosure" || groupValue === "type"
      ? groupValue as RangeGroupBy
      : undefined,
    filters: {
      eventTypes,
      animalId: parsePositiveInteger(getValue(searchParams.animal)),
      enclosureId: parsePositiveInteger(getValue(searchParams.enclosure)),
      performer: getValue(searchParams.performer) ?? "",
      from: parseDate(getValue(searchParams.from)) ?? "",
      to: parseDate(getValue(searchParams.to)) ?? "",
    },
    page: parsePositiveInteger(getValue(searchParams.page)),
  };
}

function parseView(value?: string): TimelineView | undefined {
  return value && ["list", "day", "week", "range"].includes(value) ? value as TimelineView : undefined;
}

function parseDate(value?: string) {
  return value && /^\d{4}-\d{2}-\d{2}$/.test(value) ? value : undefined;
}

function parsePositiveInteger(value?: string) {
  if (!value) return undefined;
  const number = Number(value);
  return Number.isInteger(number) && number > 0 ? number : undefined;
}

function getValue(value: string | string[] | undefined) {
  return Array.isArray(value) ? value[0] : value;
}
