"use client";

import { useDeferredValue, useEffect, useMemo, useState } from "react";
import { TimelineListView } from "@/components/activity/TimelineListView";
import { TimelineToolbar } from "@/components/activity/TimelineToolbar";
import {
  defaultTimelineFilters,
  getDefaultColumns,
  hasActiveFilters,
  type TimelineColumnId,
  type TimelineContext,
  type TimelineFilters,
} from "@/components/activity/timelineTypes";
import { Button } from "@/components/ui";
import { fetchActivities, type ActivityRecord, type ActivitySortDirection } from "@/lib/api/activity";
import { fetchAnimals, type Animal } from "@/lib/api/animals";
import { fetchEnclosures, type Enclosure } from "@/lib/api/enclosures";

type LoadState = "loading" | "loaded" | "error";

export function ActivityTimeline({ context }: { context: TimelineContext }) {
  const [activities, setActivities] = useState<ActivityRecord[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [animals, setAnimals] = useState<Animal[]>([]);
  const [enclosures, setEnclosures] = useState<Enclosure[]>([]);
  const [columns, setColumns] = useState<TimelineColumnId[]>(() => getDefaultColumns(context));
  const [filters, setFilters] = useState<TimelineFilters>(defaultTimelineFilters);
  const [search, setSearch] = useState("");
  const [sort, setSort] = useState<ActivitySortDirection>("Newest");
  const [loadState, setLoadState] = useState<LoadState>("loading");
  const [reloadKey, setReloadKey] = useState(0);
  const deferredSearch = useDeferredValue(search.trim());
  const deferredPerformer = useDeferredValue(filters.performer.trim());

  const query = useMemo(
    () => ({
      search: deferredSearch || undefined,
      eventTypes: filters.eventTypes,
      animalId: context.kind === "animal" ? context.id : filters.animalId,
      enclosureId: context.kind === "enclosure" ? context.id : filters.enclosureId,
      performedBy: deferredPerformer || undefined,
      from: filters.from ? dateBoundaryToIso(filters.from, false) : undefined,
      to: filters.to ? dateBoundaryToIso(filters.to, true) : undefined,
      sort,
      pageSize: 100,
    }),
    [context, deferredPerformer, deferredSearch, filters.animalId, filters.enclosureId, filters.eventTypes, filters.from, filters.to, sort],
  );

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
        setLoadState("loaded");
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }
        console.error(error);
        setLoadState("error");
      });

    return () => abortController.abort();
  }, [query, reloadKey]);

  const isFiltered = Boolean(search.trim() || hasActiveFilters(filters));

  return (
    <section className="activity-timeline" aria-labelledby="activity-timeline-title">
      <div className="activity-timeline-heading">
        <div>
          <span className="activity-timeline-eyebrow">Operational history</span>
          <h2 id="activity-timeline-title">Activity timeline</h2>
        </div>
        {loadState === "loaded" ? (
          <span className="activity-result-count">
            {activities.length < totalCount ? `${activities.length} of ` : ""}{totalCount} {totalCount === 1 ? "activity" : "activities"}
          </span>
        ) : null}
      </div>

      <TimelineToolbar
        animals={animals}
        columns={columns}
        context={context}
        enclosures={enclosures}
        filters={filters}
        search={search}
        sort={sort}
        onColumnsChange={setColumns}
        onFiltersChange={setFilters}
        onSearchChange={setSearch}
        onSortChange={setSort}
      />

      {loadState === "loading" ? <TimelineLoadingState columns={columns.length} /> : null}
      {loadState === "error" ? (
        <div className="timeline-state" role="alert">
          <strong>Unable to load activity.</strong>
          <span>Your search and filter selections have been preserved.</span>
          <Button variant="secondary" onClick={() => setReloadKey((value) => value + 1)}>Retry</Button>
        </div>
      ) : null}
      {loadState === "loaded" && activities.length === 0 ? (
        <div className="timeline-state">
          <strong>{isFiltered ? "No activities match the selected filters." : "No activity recorded yet."}</strong>
          {isFiltered ? (
            <Button variant="secondary" onClick={() => { setSearch(""); setFilters(defaultTimelineFilters); }}>Clear filters</Button>
          ) : null}
        </div>
      ) : null}
      {loadState === "loaded" && activities.length > 0 ? <TimelineListView activities={activities} columns={columns} sort={sort} /> : null}
    </section>
  );
}

function TimelineLoadingState({ columns }: { columns: number }) {
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

function dateBoundaryToIso(date: string, endOfDay: boolean) {
  const boundary = new Date(`${date}T${endOfDay ? "23:59:59.999" : "00:00:00.000"}`);
  return boundary.toISOString();
}
