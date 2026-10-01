"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { ActivityDetailDrawer } from "@/components/activity/ActivityDetailDrawer";
import { TimelineBulkToolbar } from "@/components/activity/TimelineBulkToolbar";
import { TimelineListView } from "@/components/activity/TimelineListView";
import { TimelineDayView } from "@/components/activity/TimelineDayView";
import { TimelineEmptyState } from "@/components/activity/TimelineEmptyState";
import { TimelineRangeView } from "@/components/activity/TimelineRangeView";
import { TimelinePagination } from "@/components/activity/TimelinePagination";
import { TimelineQuickFilters } from "@/components/activity/TimelineQuickFilters";
import { TimelineToolbar } from "@/components/activity/TimelineToolbar";
import { TimelineWeekView, getWeekWindow } from "@/components/activity/TimelineWeekView";
import { useDebouncedValue } from "@/components/activity/useDebouncedValue";
import {
  addDays,
  clampDateWindow,
  dateBoundaryToIso,
  daysBetween,
  localDateKey,
  toDateInputValue,
} from "@/components/activity/timelineDateUtils";
import {
  defaultTimelineFilters,
  getDefaultColumns,
  hasActiveFilters,
  type RangeGroupBy,
  type TimelineColumnId,
  type TimelineContext,
  type TimelineFilters,
  type TimelineInitialState,
  type TimelinePreset,
  type TimelineView,
} from "@/components/activity/timelineTypes";
import { Button } from "@/components/ui";
import { activityEventTypes, activitySortOptions, fetchActivities, formatEnumLabel, type ActivityEventType, type ActivityRecord, type ActivitySortDirection } from "@/lib/api/activity";
import { fetchAnimals, type Animal } from "@/lib/api/animals";
import { fetchEnclosures, type Enclosure } from "@/lib/api/enclosures";

type LoadState = "loading" | "loaded" | "error";
type RequestState = { key: string; status: LoadState };

type ActivityTimelineProps = {
  context: TimelineContext;
  initialState?: TimelineInitialState;
};

const defaultActivityPage = { page: 1, pageSize: 100, totalCount: 0, totalPages: 0, items: [] as ActivityRecord[] };
const preferencesKey = "critterops.activityTimeline.preferences.v1";
const presetsKey = "critterops.activityTimeline.filterPresets.v1";

export function ActivityTimeline({ context, initialState = {} }: ActivityTimelineProps) {
  const initialFilters: TimelineFilters = {
    eventTypes: initialState.filters?.eventTypes ?? [],
    animalId: context.kind === "animal" ? undefined : initialState.filters?.animalId,
    enclosureId: context.kind === "enclosure" ? undefined : initialState.filters?.enclosureId,
    performer: initialState.filters?.performer ?? "",
    from: initialState.filters?.from ?? "",
    to: initialState.filters?.to ?? "",
  };
  const [activityPage, setActivityPage] = useState(defaultActivityPage);
  const [animals, setAnimals] = useState<Animal[]>([]);
  const [enclosures, setEnclosures] = useState<Enclosure[]>([]);
  const [columns, setColumns] = useState<TimelineColumnId[]>(() => getDefaultColumns(context));
  const [filters, setFilters] = useState<TimelineFilters>(initialFilters);
  const [search, setSearch] = useState(initialState.search ?? "");
  const [sort, setSort] = useState<ActivitySortDirection>(initialState.sort ?? "Newest");
  const [view, setView] = useState<TimelineView>(initialState.view ?? "list");
  const [selectedDate, setSelectedDate] = useState(initialState.date ?? toDateInputValue(new Date()));
  const [rangeDays, setRangeDays] = useState(30);
  const [groupBy, setGroupBy] = useState<RangeGroupBy>(initialState.groupBy ?? "type");
  const [selectedActivityId, setSelectedActivityId] = useState<number | null>(null);
  const [selectedActivityIds, setSelectedActivityIds] = useState<Set<number>>(() => new Set());
  const [presets, setPresets] = useState<TimelinePreset[]>([]);
  const [page, setPage] = useState(initialState.page ?? 1);
  const [requestState, setRequestState] = useState<RequestState>({ key: "", status: "loading" });
  const [reloadKey, setReloadKey] = useState(0);
  const preferencesReadyRef = useRef(false);
  const drawerTriggerRef = useRef<HTMLElement | null>(null);
  const searchInputRef = useRef<HTMLInputElement>(null);
  const lastSelectedIndexRef = useRef<number | null>(null);
  const deferredSearch = useDebouncedValue(search.trim(), 300);
  const deferredPerformer = useDebouncedValue(filters.performer.trim(), 300);
  const viewWindow = useMemo(
    () => getViewWindow(view, selectedDate, rangeDays, filters),
    [filters, rangeDays, selectedDate, view],
  );

  const query = useMemo(
    () => ({
      search: deferredSearch || undefined,
      eventTypes: filters.eventTypes,
      animalId: context.kind === "animal" ? context.id : filters.animalId,
      enclosureId: context.kind === "enclosure" ? context.id : filters.enclosureId,
      performedBy: deferredPerformer || undefined,
      from: viewWindow.from ? dateBoundaryToIso(viewWindow.from, false) : undefined,
      to: viewWindow.to ? dateBoundaryToIso(viewWindow.to, true) : undefined,
      sort,
      page,
      pageSize: 100,
    }),
    [context, deferredPerformer, deferredSearch, filters.animalId, filters.enclosureId, filters.eventTypes, page, sort, viewWindow.from, viewWindow.to],
  );
  const requestKey = useMemo(() => `${JSON.stringify(query)}:${reloadKey}`, [query, reloadKey]);
  const loadState: LoadState = requestState.key === requestKey ? requestState.status : "loading";

  useEffect(() => {
    const url = new URL(window.location.href);
    url.searchParams.set("view", view);
    if (view === "list") {
      url.searchParams.delete("date");
    } else {
      url.searchParams.set("date", selectedDate);
    }
    setUrlParameter(url, "search", search.trim());
    setUrlParameter(url, "from", filters.from);
    setUrlParameter(url, "to", filters.to);
    setUrlParameter(url, "animal", filters.animalId);
    setUrlParameter(url, "enclosure", filters.enclosureId);
    setUrlParameter(url, "performer", filters.performer.trim());
    setUrlParameter(url, "type", filters.eventTypes.length ? filters.eventTypes.join(",") : undefined);
    setUrlParameter(url, "sort", sort === "Newest" ? undefined : sort.toLowerCase());
    setUrlParameter(url, "group", groupBy === "type" ? undefined : groupBy);
    setUrlParameter(url, "page", page > 1 ? page : undefined);
    window.history.replaceState(window.history.state, "", url);
  }, [filters, groupBy, page, search, selectedDate, sort, view]);

  useEffect(() => {
    const frame = window.requestAnimationFrame(() => {
      const preferences = readPreferences();
      const savedColumns = preferences.columns?.[context.kind];
      if (savedColumns?.length) setColumns(savedColumns);
      if (!initialState.view && preferences.view) setView(preferences.view);
      if (!initialState.sort && preferences.sort) setSort(preferences.sort);
      if (!initialState.groupBy && preferences.groupBy) setGroupBy(preferences.groupBy);
      setPresets(readPresets());
      preferencesReadyRef.current = true;
    });
    return () => window.cancelAnimationFrame(frame);
  }, [context.kind, initialState.groupBy, initialState.sort, initialState.view]);

  useEffect(() => {
    if (!preferencesReadyRef.current) return;
    const current = readPreferences();
    try {
      window.localStorage.setItem(preferencesKey, JSON.stringify({
        ...current,
        columns: { ...current.columns, [context.kind]: columns },
        view,
        sort,
        groupBy,
      }));
    } catch {
      // The timeline remains usable when storage is unavailable.
    }
  }, [columns, context.kind, groupBy, sort, view]);

  useEffect(() => {
    let isMounted = true;
    Promise.all([fetchAnimals(), fetchEnclosures()])
      .then(([animalData, enclosureData]) => {
        if (isMounted) {
          setAnimals(animalData);
          setEnclosures(enclosureData);
        }
      })
      .catch((error) => console.error("Unable to load timeline filter options", error));
    return () => { isMounted = false; };
  }, []);

  useEffect(() => {
    const abortController = new AbortController();
    let isActive = true;

    fetchActivities(query, abortController.signal)
      .then((response) => {
        if (!isActive) return;
        if (response.totalPages > 0 && response.page > response.totalPages) {
          setPage(response.totalPages);
          return;
        }
        setActivityPage(response);
        setRequestState({ key: requestKey, status: "loaded" });
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }
        if (!isActive) return;
        console.error(error);
        setRequestState({ key: requestKey, status: "error" });
      });

    return () => {
      isActive = false;
      abortController.abort();
    };
  }, [query, requestKey]);

  const isFiltered = Boolean(search.trim() || hasActiveFilters(filters));
  const visibleActivities = useMemo(
    () => activityPage.items.filter((activity) => matchesDateFilter(activity, filters)),
    [activityPage.items, filters],
  );
  const visibleTotalCount = visibleActivities.length === activityPage.items.length ? activityPage.totalCount : visibleActivities.length;
  const selectedActivityIndex = visibleActivities.findIndex((activity) => activity.id === selectedActivityId);
  const selectedActivity = selectedActivityIndex >= 0 ? visibleActivities[selectedActivityIndex] : undefined;
  const isInitialLoading = loadState === "loading" && requestState.key === "";
  const isRefreshing = loadState === "loading" && requestState.key !== "";

  const handleSelectActivity = useCallback((activity: ActivityRecord, trigger: HTMLElement) => {
    drawerTriggerRef.current = trigger;
    setSelectedActivityId(activity.id);
  }, []);

  const handleCloseDrawer = useCallback(() => {
    setSelectedActivityId(null);
    window.requestAnimationFrame(() => drawerTriggerRef.current?.focus());
  }, []);

  function clearFilters() {
    setSearch("");
    setFilters(defaultTimelineFilters);
    setPage(1);
    setSelectedActivityIds(new Set());
  }

  function handleViewChange(nextView: TimelineView) {
    setView(nextView);
    setPage(1);
  }

  function handleOpenDay(date: string) {
    setSelectedDate(date);
    setView("day");
    setPage(1);
  }

  function handleRangeNavigation(direction: -1 | 1) {
    const duration = daysBetween(viewWindow.from!, viewWindow.to!);
    if (filters.from || filters.to) {
      const nextFrom = addDays(viewWindow.from!, duration * direction);
      const nextTo = addDays(viewWindow.to!, duration * direction);
      setFilters((current) => ({ ...current, from: nextFrom, to: nextTo }));
      setSelectedDate(nextTo);
      setPage(1);
      return;
    }
    setSelectedDate(addDays(selectedDate, rangeDays * direction));
    setPage(1);
  }

  function handleRangeDaysChange(days: number) {
    setRangeDays(days);
    setFilters((current) => ({ ...current, from: "", to: "" }));
    setPage(1);
  }

  function handleUseCustomRange() {
    setFilters((current) => ({ ...current, from: viewWindow.from!, to: viewWindow.to! }));
    setPage(1);
  }

  function handleDateChange(date: string) {
    setSelectedDate(date);
    setPage(1);
    setSelectedActivityIds(new Set());
  }

  function handleFiltersChange(nextFilters: TimelineFilters) {
    setFilters(nextFilters);
    setPage(1);
    setSelectedActivityIds(new Set());
  }

  function handleSearchChange(nextSearch: string) {
    setSearch(nextSearch);
    setPage(1);
  }

  function handleSortChange(nextSort: ActivitySortDirection) {
    setSort(nextSort);
    setPage(1);
  }

  function handleGroupByChange(nextGroupBy: RangeGroupBy) {
    setGroupBy(nextGroupBy);
    setPage(1);
  }

  function handleSavePreset(name: string) {
    const preset: TimelinePreset = {
      id: typeof crypto.randomUUID === "function" ? crypto.randomUUID() : `${Date.now()}`,
      name,
      filters: { ...filters, eventTypes: [...filters.eventTypes] },
      sort,
      groupBy,
      view,
    };
    const nextPresets = [...presets, preset];
    setPresets(nextPresets);
    writePresets(nextPresets);
  }

  function handleApplyPreset(preset: TimelinePreset) {
    setSearch("");
    setFilters(sanitizeFiltersForContext(preset.filters, context));
    setSort(preset.sort);
    setGroupBy(preset.groupBy);
    setView(preset.view);
    setPage(1);
    setSelectedActivityIds(new Set());
  }

  function handleDeletePreset(id: string) {
    const nextPresets = presets.filter((preset) => preset.id !== id);
    setPresets(nextPresets);
    writePresets(nextPresets);
  }

  function handleToggleSelection(activityId: number, modifiers: { shiftKey: boolean; additive: boolean }) {
    const index = visibleActivities.findIndex((activity) => activity.id === activityId);
    setSelectedActivityIds((current) => {
      if (modifiers.shiftKey && lastSelectedIndexRef.current !== null && index >= 0) {
        const from = Math.min(lastSelectedIndexRef.current, index);
        const to = Math.max(lastSelectedIndexRef.current, index);
        return new Set([...current, ...visibleActivities.slice(from, to + 1).map((activity) => activity.id)]);
      }
      if (modifiers.additive) {
        const next = new Set(current);
        if (next.has(activityId)) next.delete(activityId);
        else next.add(activityId);
        return next;
      }
      return current.size === 1 && current.has(activityId) ? new Set() : new Set([activityId]);
    });
    if (index >= 0) lastSelectedIndexRef.current = index;
  }

  function handleToggleAll(checked: boolean) {
    setSelectedActivityIds(checked ? new Set(visibleActivities.map((activity) => activity.id)) : new Set());
  }

  function handlePageChange(nextPage: number) {
    setPage(nextPage);
    setSelectedActivityIds(new Set());
    setSelectedActivityId(null);
  }

  useEffect(() => {
    function handleShortcut(event: KeyboardEvent) {
      const target = event.target as HTMLElement | null;
      const isTyping = target instanceof HTMLInputElement || target instanceof HTMLTextAreaElement || target?.isContentEditable;
      if (event.key === "/" && !isTyping && !event.metaKey && !event.ctrlKey && !event.altKey) {
        event.preventDefault();
        searchInputRef.current?.focus();
        return;
      }
      if (!selectedActivity || isTyping) return;
      if (event.key === "ArrowLeft" && selectedActivityIndex > 0) {
        event.preventDefault();
        setSelectedActivityId(visibleActivities[selectedActivityIndex - 1].id);
      } else if (event.key === "ArrowRight" && selectedActivityIndex < visibleActivities.length - 1) {
        event.preventDefault();
        setSelectedActivityId(visibleActivities[selectedActivityIndex + 1].id);
      }
    }
    window.addEventListener("keydown", handleShortcut);
    return () => window.removeEventListener("keydown", handleShortcut);
  }, [selectedActivity, selectedActivityIndex, visibleActivities]);

  function handleJumpToCurrentRange() {
    const today = toDateInputValue(new Date());
    const duration = daysBetween(viewWindow.from!, viewWindow.to!);
    setSelectedDate(today);
    if (filters.from || filters.to) {
      setFilters((current) => ({ ...current, from: addDays(today, -(duration - 1)), to: today }));
    }
    setPage(1);
  }

  return (
    <section className="activity-timeline" aria-labelledby="activity-timeline-title">
      <div className="activity-timeline-heading">
        <div>
          <span className="activity-timeline-eyebrow">Operational history</span>
          <h2 id="activity-timeline-title">Activity timeline</h2>
        </div>
        {loadState === "loaded" ? (
          <span className="activity-result-count">
            {visibleActivities.length < visibleTotalCount ? `${visibleActivities.length} of ` : ""}{visibleTotalCount} {visibleTotalCount === 1 ? "activity" : "activities"}
          </span>
        ) : null}
      </div>

      <TimelineToolbar
        animals={animals}
        columns={columns}
        context={context}
        enclosures={enclosures}
        filters={filters}
        groupBy={groupBy}
        search={search}
        searchInputRef={searchInputRef}
        sort={sort}
        view={view}
        onColumnsChange={setColumns}
        presets={presets}
        onFiltersChange={handleFiltersChange}
        onGroupByChange={handleGroupByChange}
        onSearchChange={handleSearchChange}
        onSortChange={handleSortChange}
        onViewChange={handleViewChange}
        onResetColumns={() => setColumns(getDefaultColumns(context))}
        onApplyPreset={handleApplyPreset}
        onDeletePreset={handleDeletePreset}
        onSavePreset={handleSavePreset}
      />

      <TimelineQuickFilters filters={filters} onChange={handleFiltersChange} />
      {selectedActivityIds.size ? <TimelineBulkToolbar count={selectedActivityIds.size} onClear={() => setSelectedActivityIds(new Set())} /> : null}
      {isRefreshing ? <div className="timeline-refresh-indicator" role="status"><span /> Updating activity…</div> : null}

      {isInitialLoading ? <TimelineLoadingState columns={columns.length} view={view} /> : null}
      {loadState === "error" ? (
        <div className="timeline-state" role="alert">
          <strong>Unable to load activity.</strong>
          <span>Your search and filter selections have been preserved.</span>
          <Button variant="secondary" onClick={() => setReloadKey((value) => value + 1)}>Retry</Button>
        </div>
      ) : null}
      {requestState.status === "loaded" ? (
        <TimelineViewContent
          activities={visibleActivities}
          columns={columns}
          customRange={Boolean(filters.from || filters.to)}
          emptyMessage={context.kind === "global" ? "No activity has been recorded." : "No activity recorded yet."}
          filteredMessage={getFilteredEmptyMessage(filters, search)}
          filtered={isFiltered}
          groupBy={groupBy}
          rangeDays={rangeDays}
          selectedDate={selectedDate}
          sort={sort}
          view={view}
          viewWindow={viewWindow}
          onClear={clearFilters}
          onDateChange={handleDateChange}
          onOpenDay={handleOpenDay}
          onRangeDaysChange={handleRangeDaysChange}
          onRangeNavigate={handleRangeNavigation}
          onUseCustomRange={handleUseCustomRange}
          onJumpToCurrentRange={handleJumpToCurrentRange}
          onSelectActivity={handleSelectActivity}
          selectedActivityIds={selectedActivityIds}
          onToggleSelection={handleToggleSelection}
          onToggleAll={handleToggleAll}
        />
      ) : null}

      {requestState.status === "loaded" && !isRefreshing ? (
        <TimelinePagination {...activityPage} onPageChange={handlePageChange} />
      ) : null}

      {selectedActivity ? (
        <ActivityDetailDrawer
          activity={selectedActivity}
          hasPrevious={selectedActivityIndex > 0}
          hasNext={selectedActivityIndex < visibleActivities.length - 1}
          onClose={handleCloseDrawer}
          onPrevious={() => setSelectedActivityId(visibleActivities[selectedActivityIndex - 1].id)}
          onNext={() => setSelectedActivityId(visibleActivities[selectedActivityIndex + 1].id)}
        />
      ) : null}
    </section>
  );
}

type TimelineViewContentProps = {
  activities: ActivityRecord[];
  columns: TimelineColumnId[];
  customRange: boolean;
  emptyMessage: string;
  filteredMessage: string;
  filtered: boolean;
  groupBy: RangeGroupBy;
  rangeDays: number;
  selectedDate: string;
  sort: ActivitySortDirection;
  view: TimelineView;
  viewWindow: { from?: string; to?: string };
  onClear: () => void;
  onDateChange: (date: string) => void;
  onJumpToCurrentRange: () => void;
  onOpenDay: (date: string) => void;
  onRangeDaysChange: (days: number) => void;
  onRangeNavigate: (direction: -1 | 1) => void;
  onUseCustomRange: () => void;
  onSelectActivity: (activity: ActivityRecord, trigger: HTMLElement) => void;
  selectedActivityIds: Set<number>;
  onToggleSelection: (activityId: number, modifiers: { shiftKey: boolean; additive: boolean }) => void;
  onToggleAll: (checked: boolean) => void;
};

function TimelineViewContent(props: TimelineViewContentProps) {
  if (props.view === "day") {
    return <TimelineDayView activities={props.activities} date={props.selectedDate} filtered={props.filtered} filteredMessage={props.filteredMessage} onClear={props.onClear} onDateChange={props.onDateChange} onSelectActivity={props.onSelectActivity} />;
  }
  if (props.view === "week") {
    return <TimelineWeekView activities={props.activities} date={props.selectedDate} filtered={props.filtered} filteredMessage={props.filteredMessage} sort={props.sort} onClear={props.onClear} onDateChange={props.onDateChange} onOpenDay={props.onOpenDay} onSelectActivity={props.onSelectActivity} />;
  }
  if (props.view === "range") {
    return (
      <TimelineRangeView
        activities={props.activities}
        filtered={props.filtered}
        filteredMessage={props.filteredMessage}
        from={props.viewWindow.from!}
        groupBy={props.groupBy}
        rangeDays={props.rangeDays}
        to={props.viewWindow.to!}
        customRange={props.customRange}
        onClear={props.onClear}
        onNavigate={props.onRangeNavigate}
        onRangeDaysChange={props.onRangeDaysChange}
        onUseCustomRange={props.onUseCustomRange}
        onJumpToCurrent={props.onJumpToCurrentRange}
        onSelectActivity={props.onSelectActivity}
      />
    );
  }
  if (props.activities.length === 0) {
    return <TimelineEmptyState message={props.emptyMessage} filtered={props.filtered} filteredMessage={props.filteredMessage} onClear={props.onClear} />;
  }
  return (
    <TimelineListView
      activities={props.activities}
      columns={props.columns}
      sort={props.sort}
      onSelectActivity={props.onSelectActivity}
      selectedActivityIds={props.selectedActivityIds}
      onToggleSelection={props.onToggleSelection}
      onToggleAll={props.onToggleAll}
    />
  );
}

function TimelineLoadingState({ columns, view }: { columns: number; view: TimelineView }) {
  if (view === "day") {
    return <div className="timeline-loading timeline-loading-day" role="status" aria-label="Loading day activity">{[0, 1, 2].map((row) => <div key={row}><span className="skeleton" /><span className="skeleton" /></div>)}</div>;
  }
  if (view === "week") {
    return <div className="timeline-loading timeline-loading-week" role="status" aria-label="Loading week activity">{Array.from({ length: 7 }, (_, column) => <span className="skeleton" key={column} />)}</div>;
  }
  if (view === "range") {
    return <div className="timeline-loading timeline-loading-range" role="status" aria-label="Loading range activity">{[0, 1, 2, 3].map((row) => <div key={row}>{Array.from({ length: 8 }, (_, column) => <span className="skeleton" key={column} />)}</div>)}</div>;
  }
  return (
    <div className="timeline-loading" role="status" aria-label="Loading activity">
      <span className="visually-hidden">Loading activity...</span>
      {[0, 1, 2, 3, 4].map((row) => (
        <div className="timeline-skeleton-row" key={row} style={{ gridTemplateColumns: `repeat(${columns}, minmax(4rem, 1fr))` }}>
          {Array.from({ length: columns }, (_, column) => <span className="skeleton" key={column} />)}
        </div>
      ))}
    </div>
  );
}

function getViewWindow(view: TimelineView, selectedDate: string, rangeDays: number, filters: TimelineFilters) {
  if (view === "list") {
    return { from: filters.from || undefined, to: filters.to || undefined };
  }
  if (view === "range") {
    return {
      from: filters.from || addDays(selectedDate, -(rangeDays - 1)),
      to: filters.to || selectedDate,
    };
  }
  const baseWindow = view === "day"
    ? { from: selectedDate, to: selectedDate }
    : getWeekWindow(selectedDate);
  const clamped = clampDateWindow(baseWindow.from, baseWindow.to, filters.from, filters.to);
  if (clamped.from && clamped.to && clamped.from <= clamped.to) {
    return clamped;
  }
  return baseWindow;
}

function matchesDateFilter(activity: ActivityRecord, filters: TimelineFilters) {
  const date = localDateKey(activity.occurredAt);
  return (!filters.from || date >= filters.from) && (!filters.to || date <= filters.to);
}

function setUrlParameter(url: URL, key: string, value: string | number | undefined) {
  if (value === undefined || value === "") url.searchParams.delete(key);
  else url.searchParams.set(key, String(value));
}

type TimelinePreferences = {
  columns?: Partial<Record<TimelineContext["kind"], TimelineColumnId[]>>;
  view?: TimelineView;
  sort?: ActivitySortDirection;
  groupBy?: RangeGroupBy;
};

function readPreferences(): TimelinePreferences {
  try {
    const value = JSON.parse(window.localStorage.getItem(preferencesKey) ?? "{}") as TimelinePreferences;
    const validColumnIds = ["time", "type", "animal", "enclosure", "details", "performer", "activityId", "createdAt"];
    const columns = Object.fromEntries(
      Object.entries(value.columns ?? {}).map(([key, columnIds]) => [
        key,
        columnIds?.filter((column): column is TimelineColumnId => validColumnIds.includes(column)),
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

function sanitizeFiltersForContext(filters: TimelineFilters, context: TimelineContext): TimelineFilters {
  return {
    eventTypes: filters.eventTypes.filter((type): type is ActivityEventType => activityEventTypes.includes(type)),
    animalId: context.kind === "animal" ? undefined : Number.isInteger(filters.animalId) ? filters.animalId : undefined,
    enclosureId: context.kind === "enclosure" ? undefined : Number.isInteger(filters.enclosureId) ? filters.enclosureId : undefined,
    performer: typeof filters.performer === "string" ? filters.performer : "",
    from: /^\d{4}-\d{2}-\d{2}$/.test(filters.from) ? filters.from : "",
    to: /^\d{4}-\d{2}-\d{2}$/.test(filters.to) ? filters.to : "",
  };
}

function readPresets(): TimelinePreset[] {
  try {
    const value = JSON.parse(window.localStorage.getItem(presetsKey) ?? "[]") as unknown;
    if (!Array.isArray(value)) return [];
    return value.flatMap((item): TimelinePreset[] => {
      if (!item || typeof item !== "object") return [];
      const candidate = item as Partial<TimelinePreset>;
      if (typeof candidate.id !== "string" || typeof candidate.name !== "string" || !candidate.name.trim()) return [];
      if (!candidate.filters || !isActivitySort(candidate.sort) || !isRangeGroupBy(candidate.groupBy) || !isTimelineView(candidate.view)) return [];
      return [{
        id: candidate.id,
        name: candidate.name.trim(),
        filters: sanitizeFiltersForContext(candidate.filters, { kind: "global" }),
        sort: candidate.sort,
        groupBy: candidate.groupBy,
        view: candidate.view,
      }];
    });
  } catch {
    return [];
  }
}

function writePresets(presets: TimelinePreset[]) {
  try {
    window.localStorage.setItem(presetsKey, JSON.stringify(presets));
  } catch {
    // Presets are still available for the current session.
  }
}

function getFilteredEmptyMessage(filters: TimelineFilters, search: string) {
  if (search.trim()) return "No activity matches the current search and filters.";
  if (filters.eventTypes.length === 1) {
    const typeLabel = formatEnumLabel(filters.eventTypes[0]).toLowerCase();
    const pluralLabel = typeLabel.endsWith("s") ? typeLabel : `${typeLabel}s`;
    if (filters.from || filters.to) return `No ${pluralLabel} were recorded during the selected time range.`;
    return `No ${pluralLabel} have been recorded.`;
  }
  if (filters.performer.trim()) return `No activity was recorded by ${filters.performer.trim()} in the selected time range.`;
  if (filters.animalId || filters.enclosureId) return "No activity was recorded for the selected entity and time range.";
  if (filters.from || filters.to) return "No activity was recorded during the selected time range.";
  return "No activity matches the selected filters.";
}
