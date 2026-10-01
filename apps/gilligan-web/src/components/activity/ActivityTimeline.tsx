"use client";

import { useDeferredValue, useEffect, useMemo, useState } from "react";
import { TimelineListView } from "@/components/activity/TimelineListView";
import { TimelineDayView } from "@/components/activity/TimelineDayView";
import { TimelineEmptyState } from "@/components/activity/TimelineEmptyState";
import { TimelineRangeView } from "@/components/activity/TimelineRangeView";
import { TimelineToolbar } from "@/components/activity/TimelineToolbar";
import { TimelineWeekView, getWeekWindow } from "@/components/activity/TimelineWeekView";
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
  type TimelineView,
} from "@/components/activity/timelineTypes";
import { Button } from "@/components/ui";
import { fetchActivities, type ActivityRecord, type ActivitySortDirection } from "@/lib/api/activity";
import { fetchAnimals, type Animal } from "@/lib/api/animals";
import { fetchEnclosures, type Enclosure } from "@/lib/api/enclosures";

type LoadState = "loading" | "loaded" | "error";
type RequestState = { key: string; status: LoadState };

type ActivityTimelineProps = {
  context: TimelineContext;
  initialDate?: string;
  initialView?: TimelineView;
};

export function ActivityTimeline({ context, initialDate, initialView = "list" }: ActivityTimelineProps) {
  const [activities, setActivities] = useState<ActivityRecord[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [animals, setAnimals] = useState<Animal[]>([]);
  const [enclosures, setEnclosures] = useState<Enclosure[]>([]);
  const [columns, setColumns] = useState<TimelineColumnId[]>(() => getDefaultColumns(context));
  const [filters, setFilters] = useState<TimelineFilters>(defaultTimelineFilters);
  const [search, setSearch] = useState("");
  const [sort, setSort] = useState<ActivitySortDirection>("Newest");
  const [view, setView] = useState<TimelineView>(initialView);
  const [selectedDate, setSelectedDate] = useState(initialDate ?? toDateInputValue(new Date()));
  const [rangeDays, setRangeDays] = useState(30);
  const [groupBy, setGroupBy] = useState<RangeGroupBy>("type");
  const [requestState, setRequestState] = useState<RequestState>({ key: "", status: "loading" });
  const [reloadKey, setReloadKey] = useState(0);
  const deferredSearch = useDeferredValue(search.trim());
  const deferredPerformer = useDeferredValue(filters.performer.trim());
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
      pageSize: 100,
    }),
    [context, deferredPerformer, deferredSearch, filters.animalId, filters.enclosureId, filters.eventTypes, sort, viewWindow.from, viewWindow.to],
  );
  const requestKey = useMemo(() => `${JSON.stringify(query)}:${reloadKey}`, [query, reloadKey]);
  const loadState: LoadState = requestState.key === requestKey ? requestState.status : "loading";

  useEffect(() => {
    const url = new URL(window.location.href);
    if (view === "list") {
      url.searchParams.delete("view");
      url.searchParams.delete("date");
    } else {
      url.searchParams.set("view", view);
      url.searchParams.set("date", selectedDate);
    }
    window.history.replaceState(window.history.state, "", url);
  }, [selectedDate, view]);

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

    fetchActivities(query, abortController.signal)
      .then((response) => {
        setActivities(response.items);
        setTotalCount(response.totalCount);
        setRequestState({ key: requestKey, status: "loaded" });
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }
        console.error(error);
        setRequestState({ key: requestKey, status: "error" });
      });

    return () => abortController.abort();
  }, [query, requestKey]);

  const isFiltered = Boolean(search.trim() || hasActiveFilters(filters));
  const visibleActivities = useMemo(
    () => activities.filter((activity) => matchesDateFilter(activity, filters)),
    [activities, filters],
  );
  const visibleTotalCount = visibleActivities.length === activities.length ? totalCount : visibleActivities.length;

  function clearFilters() {
    setSearch("");
    setFilters(defaultTimelineFilters);
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

  function handleJumpToCurrentRange() {
    const today = toDateInputValue(new Date());
    const duration = daysBetween(viewWindow.from!, viewWindow.to!);
    setSelectedDate(today);
    if (filters.from || filters.to) {
      setFilters((current) => ({ ...current, from: addDays(today, -(duration - 1)), to: today }));
    }
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
        sort={sort}
        view={view}
        onColumnsChange={setColumns}
        onFiltersChange={setFilters}
        onGroupByChange={setGroupBy}
        onSearchChange={setSearch}
        onSortChange={setSort}
        onViewChange={handleViewChange}
      />

      {loadState === "loading" ? <TimelineLoadingState columns={columns.length} view={view} /> : null}
      {loadState === "error" ? (
        <div className="timeline-state" role="alert">
          <strong>Unable to load activity.</strong>
          <span>Your search and filter selections have been preserved.</span>
          <Button variant="secondary" onClick={() => setReloadKey((value) => value + 1)}>Retry</Button>
        </div>
      ) : null}
      {loadState === "loaded" ? (
        <TimelineViewContent
          activities={visibleActivities}
          columns={columns}
          customRange={Boolean(filters.from || filters.to)}
          filtered={isFiltered}
          groupBy={groupBy}
          rangeDays={rangeDays}
          selectedDate={selectedDate}
          sort={sort}
          view={view}
          viewWindow={viewWindow}
          onClear={clearFilters}
          onDateChange={setSelectedDate}
          onOpenDay={handleOpenDay}
          onRangeDaysChange={handleRangeDaysChange}
          onRangeNavigate={handleRangeNavigation}
          onUseCustomRange={handleUseCustomRange}
          onJumpToCurrentRange={handleJumpToCurrentRange}
        />
      ) : null}
    </section>
  );
}

type TimelineViewContentProps = {
  activities: ActivityRecord[];
  columns: TimelineColumnId[];
  customRange: boolean;
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
};

function TimelineViewContent(props: TimelineViewContentProps) {
  if (props.view === "day") {
    return <TimelineDayView activities={props.activities} date={props.selectedDate} filtered={props.filtered} onClear={props.onClear} onDateChange={props.onDateChange} />;
  }
  if (props.view === "week") {
    return <TimelineWeekView activities={props.activities} date={props.selectedDate} filtered={props.filtered} sort={props.sort} onClear={props.onClear} onDateChange={props.onDateChange} onOpenDay={props.onOpenDay} />;
  }
  if (props.view === "range") {
    return (
      <TimelineRangeView
        activities={props.activities}
        filtered={props.filtered}
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
      />
    );
  }
  if (props.activities.length === 0) {
    return <TimelineEmptyState message="No activity recorded yet." filtered={props.filtered} onClear={props.onClear} />;
  }
  return <TimelineListView activities={props.activities} columns={props.columns} sort={props.sort} />;
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
