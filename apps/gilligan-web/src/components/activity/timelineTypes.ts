import type { ActivityEventType, ActivitySortDirection } from "@/lib/api/activity";

export type TimelineView = "list" | "day" | "week" | "range";
export type RangeGroupBy = "type" | "animal" | "enclosure";

export type TimelineInitialState = {
  view?: TimelineView;
  date?: string;
  search?: string;
  sort?: ActivitySortDirection;
  groupBy?: RangeGroupBy;
  filters?: Partial<TimelineFilters>;
  page?: number;
};

export type TimelineContext =
  | { kind: "animal"; id: number; name: string }
  | { kind: "enclosure"; id: number; name: string }
  | { kind: "global" };

export type TimelineColumnId =
  | "time"
  | "type"
  | "animal"
  | "enclosure"
  | "details"
  | "performer"
  | "activityId"
  | "createdAt";

export type TimelineFilters = {
  eventTypes: ActivityEventType[];
  animalId?: number;
  enclosureId?: number;
  performer: string;
  from: string;
  to: string;
};

export type TimelinePreset = {
  id: string;
  name: string;
  filters: TimelineFilters;
  sort: ActivitySortDirection;
  groupBy: RangeGroupBy;
  view: TimelineView;
};

export type TimelineQueryState = {
  search: string;
  filters: TimelineFilters;
  sort: ActivitySortDirection;
};

export const timelineColumns: Array<{ id: TimelineColumnId; label: string }> = [
  { id: "time", label: "Time" },
  { id: "type", label: "Type" },
  { id: "animal", label: "Animal" },
  { id: "enclosure", label: "Enclosure" },
  { id: "details", label: "Details" },
  { id: "performer", label: "Performer" },
  { id: "activityId", label: "Activity ID" },
  { id: "createdAt", label: "Created date" },
];

export const defaultTimelineFilters: TimelineFilters = {
  eventTypes: [],
  performer: "",
  from: "",
  to: "",
};

export function getDefaultColumns(context: TimelineContext): TimelineColumnId[] {
  return timelineColumns
    .map((column) => column.id)
    .filter((column) => column !== "activityId" && column !== "createdAt")
    .filter((column) => !(context.kind === "animal" && column === "animal"))
    .filter((column) => !(context.kind === "enclosure" && column === "enclosure"));
}

export function hasActiveFilters(filters: TimelineFilters) {
  return Boolean(
    filters.eventTypes.length ||
      filters.animalId ||
      filters.enclosureId ||
      filters.performer ||
      filters.from ||
      filters.to,
  );
}
