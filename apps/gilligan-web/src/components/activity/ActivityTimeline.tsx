"use client";

import { useCallback, useEffect, useMemo, useRef, useState, type RefObject } from "react";
import { ActivityDetailDrawer } from "@/components/activity/ActivityDetailDrawer";
import { TimelineBulkToolbar } from "@/components/activity/TimelineBulkToolbar";
import { TimelineListView } from "@/components/activity/TimelineListView";
import { TimelineDayView } from "@/components/activity/TimelineDayView";
import { TimelineEmptyState } from "@/components/activity/TimelineEmptyState";
import { TimelineRangeView } from "@/components/activity/TimelineRangeView";
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
  type TimelineSavedView,
  type TimelineView,
} from "@/components/activity/timelineTypes";
import { parseTimelineInitialState } from "./timelineUrlState";
import { useActivityPages, type AdditionalLoadState } from "./useActivityPages";
import { readPreferences, writePreferences, readSavedViews, writeSavedViews, sanitizeFiltersForContext, sanitizeColumnsForContext } from "./timelineStorage";
import { Button, Skeleton, SkeletonText } from "@/components/ui";
import { activityPageSize, formatEnumLabel, type ActivityRecord, type ActivitySortDirection } from "@/lib/api/activity";
import { fetchAnimals, type Animal } from "@/lib/api/animals";
import { fetchEnclosures, type Enclosure } from "@/lib/api/enclosures";

type ActivityTimelineProps = {
  context: TimelineContext;
  initialState?: TimelineInitialState;
};

export function ActivityTimeline({ context, initialState = {} }: ActivityTimelineProps) {
  const initialFilters: TimelineFilters = {
    eventTypes: initialState.filters?.eventTypes ?? [],
    animalId: context.kind === "animal" ? undefined : initialState.filters?.animalId,
    enclosureId: context.kind === "enclosure" ? undefined : initialState.filters?.enclosureId,
    performer: initialState.filters?.performer ?? "",
    from: initialState.filters?.from ?? "",
    to: initialState.filters?.to ?? "",
  };
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
  const [savedViews, setSavedViews] = useState<TimelineSavedView[]>([]);
  const [savedViewsError, setSavedViewsError] = useState("");
  const preferencesReadyRef = useRef(false);
  const drawerTriggerRef = useRef<HTMLElement | null>(null);
  const searchInputRef = useRef<HTMLInputElement>(null);
  const lastSelectedIndexRef = useRef<number | null>(null);
  const loadingSentinelRef = useRef<HTMLDivElement>(null);
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
      pageSize: activityPageSize,
    }),
    [context, deferredPerformer, deferredSearch, filters.animalId, filters.enclosureId, filters.eventTypes, sort, viewWindow.from, viewWindow.to],
  );
  const { activityPage, performers, loadState, additionalLoadState, hasMore, loadNextPage, retry } = useActivityPages(query);

  // Component state owns edits; browser history restores it through the same URL parser as the page.
  useEffect(() => {
    function restoreUrlState() {
      const state = parseTimelineInitialState(Object.fromEntries(new URLSearchParams(window.location.search)));
      setFilters(sanitizeFiltersForContext({ ...defaultTimelineFilters, ...state.filters }, context));
      setSearch(state.search ?? "");
      setSort(state.sort ?? "Newest");
      setView(state.view ?? "list");
      setGroupBy(state.groupBy ?? "type");
      setSelectedDate(state.date ?? toDateInputValue(new Date()));
      setSelectedActivityIds(new Set());
    }
    window.addEventListener("popstate", restoreUrlState);
    return () => window.removeEventListener("popstate", restoreUrlState);
  }, [context]);

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
    window.history.replaceState(window.history.state, "", url);
  }, [filters, groupBy, search, selectedDate, sort, view]);

  useEffect(() => {
    const frame = window.requestAnimationFrame(() => {
      const preferences = readPreferences();
      const savedColumns = preferences.columns?.[context.kind];
      if (savedColumns?.length) setColumns(savedColumns);
      if (!initialState.view && preferences.view) setView(preferences.view);
      if (!initialState.sort && preferences.sort) setSort(preferences.sort);
      if (!initialState.groupBy && preferences.groupBy) setGroupBy(preferences.groupBy);
      const storedSavedViews = readSavedViews();
      if (storedSavedViews) setSavedViews(storedSavedViews);
      else setSavedViewsError("Saved Views are unavailable in this browser.");
      preferencesReadyRef.current = true;
    });
    return () => window.cancelAnimationFrame(frame);
  }, [context.kind, initialState.groupBy, initialState.sort, initialState.view]);

  useEffect(() => {
    if (!preferencesReadyRef.current) return;
    writePreferences(context, columns, view, sort, groupBy);
  }, [columns, context, groupBy, sort, view]);

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

  const isFiltered = Boolean(deferredSearch.trim() || hasActiveFilters(filters));
  const visibleActivities = useMemo(
    () => activityPage.items.filter((activity) => matchesDateFilter(activity, filters)),
    [activityPage.items, filters],
  );
  const resultsComplete = loadState === "loaded" && !hasMore;
  const automaticallyLoadMore = view === "list" || view === "day";
  const selectedActivityIndex = visibleActivities.findIndex((activity) => activity.id === selectedActivityId);
  const selectedActivity = selectedActivityIndex >= 0 ? visibleActivities[selectedActivityIndex] : undefined;
  const isInitialLoading = loadState === "loading";

  useEffect(() => {
    const sentinel = loadingSentinelRef.current;
    if (!sentinel || !automaticallyLoadMore || !hasMore || loadState !== "loaded" || additionalLoadState === "error") return;
    const observer = new IntersectionObserver(
      ([entry]) => { if (entry.isIntersecting) loadNextPage(); },
      { rootMargin: "0px 0px 480px", threshold: 0 },
    );
    observer.observe(sentinel);
    return () => observer.disconnect();
  }, [additionalLoadState, automaticallyLoadMore, hasMore, loadNextPage, loadState]);

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
    setSelectedActivityIds(new Set());
  }

  function handleViewChange(nextView: TimelineView) {
    setView(nextView);
  }

  function handleOpenDay(date: string) {
    setSelectedDate(date);
    setView("day");
  }

  function handleRangeNavigation(direction: -1 | 1) {
    const duration = daysBetween(viewWindow.from!, viewWindow.to!);
    if (filters.from || filters.to) {
      const nextFrom = addDays(viewWindow.from!, duration * direction);
      const nextTo = addDays(viewWindow.to!, duration * direction);
      setFilters((current) => ({ ...current, from: nextFrom, to: nextTo }));
      setSelectedDate(nextTo);
      return;
    }
    setSelectedDate(addDays(selectedDate, rangeDays * direction));
  }

  function handleRangeDaysChange(days: number) {
    setRangeDays(days);
    setFilters((current) => ({ ...current, from: "", to: "" }));
  }

  function handleUseCustomRange() {
    setFilters((current) => ({ ...current, from: viewWindow.from!, to: viewWindow.to! }));
  }

  function handleDateChange(date: string) {
    setSelectedDate(date);
    setSelectedActivityIds(new Set());
  }

  function handleFiltersChange(nextFilters: TimelineFilters) {
    setFilters(nextFilters);
    setSelectedActivityIds(new Set());
  }

  function handleSearchChange(nextSearch: string) {
    setSearch(nextSearch);
  }

  function handleSortChange(nextSort: ActivitySortDirection) {
    setSort(nextSort);
  }

  function handleGroupByChange(nextGroupBy: RangeGroupBy) {
    setGroupBy(nextGroupBy);
  }

  function handleSaveView(name: string) {
    const savedView: TimelineSavedView = {
      id: crypto.randomUUID(),
      name,
      filters: { ...filters, eventTypes: [...filters.eventTypes] },
      sort,
      groupBy,
      view,
      columns: [...columns],
      search,
      pinned: false,
    };
    updateSavedViews([...savedViews, savedView]);
  }

  function handleApplySavedView(savedView: TimelineSavedView) {
    setSearch(savedView.search ?? "");
    setFilters(sanitizeFiltersForContext(savedView.filters, context));
    setSort(savedView.sort);
    setGroupBy(savedView.groupBy);
    setView(savedView.view);
    if (savedView.columns?.length) setColumns(sanitizeColumnsForContext(savedView.columns, context));
    setSelectedActivityIds(new Set());
  }

  function handleDeleteSavedView(id: string) {
    updateSavedViews(savedViews.filter((savedView) => savedView.id !== id));
  }

  function handleRenameSavedView(id: string, name: string) {
    updateSavedViews(savedViews.map((savedView) => savedView.id === id ? { ...savedView, name } : savedView));
  }

  function handleToggleSavedViewPin(id: string) {
    updateSavedViews(savedViews.map((savedView) => savedView.id === id ? { ...savedView, pinned: !savedView.pinned } : savedView));
  }

  function updateSavedViews(nextSavedViews: TimelineSavedView[]) {
    setSavedViews(nextSavedViews);
    setSavedViewsError(writeSavedViews(nextSavedViews) ? "" : "Saved Views could not be saved in this browser.");
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
  }

  return (
    <section className="activity-timeline" aria-labelledby="activity-timeline-title" aria-busy={isInitialLoading || additionalLoadState === "loading"}>
      <div className="activity-timeline-heading">
        <div>
          <span className="activity-timeline-eyebrow">Operational history</span>
          <h2 id="activity-timeline-title">Activity timeline</h2>
        </div>
      </div>

      <TimelineToolbar
        animals={animals}
        columns={columns}
        context={context}
        enclosures={enclosures}
        filters={filters}
        groupBy={groupBy}
        performers={performers}
        search={search}
        searchInputRef={searchInputRef}
        sort={sort}
        view={view}
        onColumnsChange={setColumns}
        onFiltersChange={handleFiltersChange}
        onGroupByChange={handleGroupByChange}
        onSearchChange={handleSearchChange}
        onSortChange={handleSortChange}
        onViewChange={handleViewChange}
        onResetColumns={() => setColumns(getDefaultColumns(context))}
      />

      <TimelineQuickFilters
        columns={columns}
        filters={filters}
        groupBy={groupBy}
        savedViews={savedViews}
        savedViewsError={savedViewsError}
        search={search}
        sort={sort}
        view={view}
        onChange={handleFiltersChange}
        onApplySavedView={handleApplySavedView}
        onDeleteSavedView={handleDeleteSavedView}
        onRenameSavedView={handleRenameSavedView}
        onSaveView={handleSaveView}
        onTogglePin={handleToggleSavedViewPin}
      />
      {selectedActivityIds.size ? <TimelineBulkToolbar count={selectedActivityIds.size} onClear={() => setSelectedActivityIds(new Set())} /> : null}

      {isInitialLoading ? <TimelineLoadingState columns={columns.length} view={view} /> : null}
      {loadState === "error" ? (
        <div className="timeline-state" role="alert">
          <strong>Unable to load activity.</strong>
          <span>Your search and filter selections have been preserved.</span>
          <Button variant="secondary" onClick={retry}>Retry</Button>
        </div>
      ) : null}
      {loadState === "loaded" ? (
        <TimelineViewContent
          activities={visibleActivities}
          columns={columns}
          customRange={Boolean(filters.from || filters.to)}
          emptyMessage={context.kind === "global" ? "No activity has been recorded." : "No activity recorded yet."}
          filteredMessage={getFilteredEmptyMessage(filters, deferredSearch)}
          filtered={isFiltered}
          groupBy={groupBy}
          rangeDays={rangeDays}
          resultsComplete={resultsComplete}
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

      {loadState === "loaded" && visibleActivities.length > 0 ? (
        <TimelineInfiniteScrollStatus
          additionalLoadState={additionalLoadState}
          automaticallyLoadMore={automaticallyLoadMore}
          hasMore={hasMore}
          sentinelRef={loadingSentinelRef}
          onRetry={loadNextPage}
        />
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
  resultsComplete: boolean;
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
    return <TimelineDayView activities={props.activities} date={props.selectedDate} filtered={props.filtered} filteredMessage={props.filteredMessage} resultsComplete={props.resultsComplete} onClear={props.onClear} onDateChange={props.onDateChange} onSelectActivity={props.onSelectActivity} />;
  }
  if (props.view === "week") {
    return <TimelineWeekView activities={props.activities} date={props.selectedDate} filtered={props.filtered} filteredMessage={props.filteredMessage} resultsComplete={props.resultsComplete} sort={props.sort} onClear={props.onClear} onDateChange={props.onDateChange} onOpenDay={props.onOpenDay} onSelectActivity={props.onSelectActivity} />;
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
        resultsComplete={props.resultsComplete}
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
    return <div className="timeline-loading timeline-loading-day" role="status" aria-label="Loading day activity">{[0, 1, 2].map((row) => <div key={row}><Skeleton /><Skeleton /></div>)}</div>;
  }
  if (view === "week") {
    return <div className="timeline-loading timeline-loading-week" role="status" aria-label="Loading week activity">{Array.from({ length: 7 }, (_, column) => <Skeleton key={column} />)}</div>;
  }
  if (view === "range") {
    return <div className="timeline-loading timeline-loading-range" role="status" aria-label="Loading range activity">{[0, 1, 2, 3].map((row) => <div key={row}>{Array.from({ length: 8 }, (_, column) => <Skeleton key={column} />)}</div>)}</div>;
  }
  return (
    <div className="timeline-loading" role="status" aria-label="Loading activity">
      <span className="visually-hidden">Loading activity...</span>
      {[0, 1, 2, 3, 4].map((row) => (
        <div className="timeline-skeleton-row" key={row} style={{ gridTemplateColumns: `repeat(${columns}, minmax(4rem, 1fr))` }}>
          {Array.from({ length: columns }, (_, column) => <SkeletonText variant="metadata" width={column % 3 === 0 ? "72%" : "88%"} key={column} />)}
        </div>
      ))}
    </div>
  );
}

function TimelineInfiniteScrollStatus({
  additionalLoadState,
  automaticallyLoadMore,
  hasMore,
  sentinelRef,
  onRetry,
}: {
  additionalLoadState: AdditionalLoadState;
  automaticallyLoadMore: boolean;
  hasMore: boolean;
  sentinelRef: RefObject<HTMLDivElement | null>;
  onRetry: () => void;
}) {
  return (
    <div ref={sentinelRef} className="timeline-load-more-sentinel" role="status" aria-live="polite">
      {additionalLoadState === "loading" ? (
        <div className="timeline-load-more-message timeline-load-more-skeleton">
          <SkeletonText variant="metadata" width="7rem" />
          <SkeletonText variant="metadata" width="11rem" />
          <span className="visually-hidden">Loading more activity…</span>
        </div>
      ) : null}
      {additionalLoadState === "error" ? (
        <span className="timeline-load-more-error">
          <span>Unable to load more activity.</span>
          <Button variant="secondary" onClick={onRetry}>Retry</Button>
        </span>
      ) : null}
      {additionalLoadState === "idle" && hasMore && automaticallyLoadMore ? <span className="visually-hidden">More activity will load as you scroll.</span> : null}
      {additionalLoadState === "idle" && hasMore && !automaticallyLoadMore ? <Button variant="secondary" onClick={onRetry}>Load more activity</Button> : null}
      {additionalLoadState === "idle" && !hasMore ? <span className="timeline-end-of-activity">End of activity</span> : null}
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
