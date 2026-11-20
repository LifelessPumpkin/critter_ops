import { isTimelineDate } from "./timelineDateUtils";
import { activityEventTypes, activitySortOptions, type ActivityEventType, type ActivitySortDirection } from "@/lib/api/activity";
import { getDefaultColumns, type TimelineContext, type TimelineColumnId, type TimelineFilters, type TimelineSavedView, type TimelineView, type RangeGroupBy } from "./timelineTypes";

const preferencesKey = "critterops.activityTimeline.preferences.v1";
const savedViewsKey = "critterops.activityTimeline.savedViews.v1";
const legacyPresetsKey = "critterops.activityTimeline.filterPresets.v1";

type TimelinePreferences = {
  columns?: Partial<Record<TimelineContext["kind"], TimelineColumnId[]>>;
  view?: TimelineView;
  sort?: ActivitySortDirection;
  groupBy?: RangeGroupBy;
};

export function readPreferences(): TimelinePreferences {
  try {
    const value = JSON.parse(window.localStorage.getItem(preferencesKey) ?? "{}") as TimelinePreferences;
    const validColumnIds = ["time", "type", "animal", "enclosure", "details", "performer", "activityId", "createdAt"];
    const columns = Object.fromEntries(
      Object.entries(value.columns ?? {}).map(([key, columnIds]) => [
        key,
        Array.isArray(columnIds) ? columnIds.filter((column): column is TimelineColumnId => validColumnIds.includes(column)) : undefined,
      ]),
    ) as TimelinePreferences["columns"];
    return {
      columns,
      view: value.view && ["list", "day", "week", "range"].includes(value.view) ? value.view : undefined,
      sort: isActivitySort(value.sort) ? value.sort : undefined,
      groupBy: value.groupBy && ["type", "animal", "enclosure"].includes(value.groupBy) ? value.groupBy : undefined,
    };
  } catch {
    return {};
  }
}

function isActivitySort(value: unknown): value is ActivitySortDirection {
  return typeof value === "string" && activitySortOptions.some((option) => option.value === value);
}

function isTimelineView(value: unknown): value is TimelineView {
  return typeof value === "string" && ["list", "day", "week", "range"].includes(value);
}

function isRangeGroupBy(value: unknown): value is RangeGroupBy {
  return typeof value === "string" && ["type", "animal", "enclosure"].includes(value);
}

export function sanitizeFiltersForContext(filters: TimelineFilters, context: TimelineContext): TimelineFilters {
  return {
    eventTypes: (Array.isArray(filters.eventTypes) ? filters.eventTypes : []).filter((type): type is ActivityEventType => activityEventTypes.includes(type)),
    animalId: context.kind === "animal" ? undefined : Number.isInteger(filters.animalId) ? filters.animalId : undefined,
    enclosureId: context.kind === "enclosure" ? undefined : Number.isInteger(filters.enclosureId) ? filters.enclosureId : undefined,
    performer: typeof filters.performer === "string" ? filters.performer : "",
    from: isTimelineDate(filters.from) ? filters.from : "",
    to: isTimelineDate(filters.to) ? filters.to : "",
  };
}

export function sanitizeColumnsForContext(columns: TimelineColumnId[], context: TimelineContext) {
  const visibleColumns = columns
    .filter((column) => !(context.kind === "animal" && column === "animal"))
    .filter((column) => !(context.kind === "enclosure" && column === "enclosure"));
  return visibleColumns.length ? visibleColumns : getDefaultColumns(context);
}

export function readSavedViews(): TimelineSavedView[] | null {
  try {
    const savedValue = window.localStorage.getItem(savedViewsKey);
    const value = JSON.parse(savedValue ?? window.localStorage.getItem(legacyPresetsKey) ?? "[]") as unknown;
    if (!Array.isArray(value)) return [];
    return value.flatMap((item): TimelineSavedView[] => {
      if (!item || typeof item !== "object") return [];
      const candidate = item as Partial<TimelineSavedView>;
      if (typeof candidate.id !== "string" || typeof candidate.name !== "string" || !candidate.name.trim()) return [];
      if (!candidate.filters || typeof candidate.filters !== "object" || !isActivitySort(candidate.sort) || !isRangeGroupBy(candidate.groupBy) || !isTimelineView(candidate.view)) return [];
      const validColumnIds: TimelineColumnId[] = ["time", "type", "animal", "enclosure", "details", "performer", "activityId", "createdAt"];
      return [{
        id: candidate.id,
        name: candidate.name.trim(),
        filters: sanitizeFiltersForContext(candidate.filters, { kind: "global" }),
        sort: candidate.sort,
        groupBy: candidate.groupBy,
        view: candidate.view,
        columns: Array.isArray(candidate.columns) ? candidate.columns.filter((column): column is TimelineColumnId => validColumnIds.includes(column)) : undefined,
        search: typeof candidate.search === "string" ? candidate.search : "",
        pinned: candidate.pinned === true,
      }];
    });
  } catch {
    return null;
  }
}

export function writeSavedViews(savedViews: TimelineSavedView[]) {
  try {
    window.localStorage.setItem(savedViewsKey, JSON.stringify(savedViews));
    return true;
  } catch {
    return false;
  }
}

export function writePreferences(context: TimelineContext, columns: TimelineColumnId[], view: TimelineView, sort: ActivitySortDirection, groupBy: RangeGroupBy) {
  const current = readPreferences();
  try {
    window.localStorage.setItem(preferencesKey, JSON.stringify({
      ...current, columns: { ...current.columns, [context.kind]: columns }, view, sort, groupBy,
    }));
  } catch {
    // Storage is optional; keep the timeline usable.
  }
}
