"use client";

import { useMemo, useState, type MouseEvent } from "react";
import type { Animal } from "@/lib/api/animals";
import type { Enclosure } from "@/lib/api/enclosures";
import { activityEventTypes, formatEnumLabel, type ActivityEventType, type ActivitySortDirection } from "@/lib/api/activity";
import { Button } from "@/components/ui";
import {
  hasActiveFilters,
  timelineColumns,
  type RangeGroupBy,
  type TimelineColumnId,
  type TimelineContext,
  type TimelineFilters,
  type TimelineView,
} from "@/components/activity/timelineTypes";

type TimelineToolbarProps = {
  animals: Animal[];
  columns: TimelineColumnId[];
  context: TimelineContext;
  enclosures: Enclosure[];
  filters: TimelineFilters;
  groupBy: RangeGroupBy;
  search: string;
  sort: ActivitySortDirection;
  view: TimelineView;
  onColumnsChange: (columns: TimelineColumnId[]) => void;
  onFiltersChange: (filters: TimelineFilters) => void;
  onGroupByChange: (groupBy: RangeGroupBy) => void;
  onSearchChange: (search: string) => void;
  onSortChange: (sort: ActivitySortDirection) => void;
  onViewChange: (view: TimelineView) => void;
};

export function TimelineToolbar({
  animals,
  columns,
  context,
  enclosures,
  filters,
  groupBy,
  search,
  sort,
  view,
  onColumnsChange,
  onFiltersChange,
  onGroupByChange,
  onSearchChange,
  onSortChange,
  onViewChange,
}: TimelineToolbarProps) {
  const [animalSearch, setAnimalSearch] = useState("");
  const [enclosureSearch, setEnclosureSearch] = useState("");
  const filteredAnimals = useMemo(
    () => animals.filter((animal) => animal.name.toLowerCase().includes(animalSearch.toLowerCase())),
    [animalSearch, animals],
  );
  const filteredEnclosures = useMemo(
    () => enclosures.filter((enclosure) => enclosure.name.toLowerCase().includes(enclosureSearch.toLowerCase())),
    [enclosureSearch, enclosures],
  );

  function toggleEventType(eventType: ActivityEventType) {
    const eventTypes = filters.eventTypes.includes(eventType)
      ? filters.eventTypes.filter((value) => value !== eventType)
      : [...filters.eventTypes, eventType];
    onFiltersChange({ ...filters, eventTypes });
  }

  function toggleColumn(column: TimelineColumnId) {
    if (columns.includes(column)) {
      if (columns.length > 1) {
        onColumnsChange(columns.filter((value) => value !== column));
      }
      return;
    }

    const orderedColumns = timelineColumns.map((item) => item.id).filter((value) => [...columns, column].includes(value));
    onColumnsChange(orderedColumns);
  }

  return (
    <div className="timeline-toolbar" aria-label="Activity timeline controls">
      <label className="timeline-search">
        <span className="visually-hidden">Search activities</span>
        <SearchIcon />
        <input
          type="search"
          value={search}
          onChange={(event) => onSearchChange(event.target.value)}
          placeholder="Search activities..."
        />
      </label>

      <div className="timeline-property-controls">
        <FilterPopover label="Type" count={filters.eventTypes.length} onClear={() => onFiltersChange({ ...filters, eventTypes: [] })}>
          <PopoverHeading title="Event type" />
          <div className="timeline-option-list">
            {activityEventTypes.map((eventType) => (
              <label key={eventType} className="timeline-check-option">
                <input
                  type="checkbox"
                  checked={filters.eventTypes.includes(eventType)}
                  onChange={() => toggleEventType(eventType)}
                />
                <ActivityTypeMark type={eventType} />
                <span>{formatEnumLabel(eventType)}</span>
              </label>
            ))}
          </div>
        </FilterPopover>

        {context.kind !== "animal" ? (
          <FilterPopover label="Animal" count={filters.animalId ? 1 : 0} onClear={() => onFiltersChange({ ...filters, animalId: undefined })}>
            <PopoverHeading title="Animal" />
            <OptionSearch label="Search animals" value={animalSearch} onChange={setAnimalSearch} />
            <div className="timeline-option-list timeline-option-list-scroll">
              {filteredAnimals.map((animal) => (
                <label key={animal.id} className="timeline-check-option">
                  <input
                    type="radio"
                    name="timeline-animal"
                    checked={filters.animalId === animal.id}
                    onChange={() => onFiltersChange({ ...filters, animalId: animal.id })}
                  />
                  <span>{animal.name}</span>
                </label>
              ))}
              {filteredAnimals.length === 0 ? <span className="timeline-option-empty">No animals found.</span> : null}
            </div>
          </FilterPopover>
        ) : null}

        {context.kind !== "enclosure" ? (
          <FilterPopover label="Enclosure" count={filters.enclosureId ? 1 : 0} onClear={() => onFiltersChange({ ...filters, enclosureId: undefined })}>
            <PopoverHeading title="Enclosure" />
            <OptionSearch label="Search enclosures" value={enclosureSearch} onChange={setEnclosureSearch} />
            <div className="timeline-option-list timeline-option-list-scroll">
              {filteredEnclosures.map((enclosure) => (
                <label key={enclosure.id} className="timeline-check-option">
                  <input
                    type="radio"
                    name="timeline-enclosure"
                    checked={filters.enclosureId === enclosure.id}
                    onChange={() => onFiltersChange({ ...filters, enclosureId: enclosure.id })}
                  />
                  <span>{enclosure.name}</span>
                </label>
              ))}
              {filteredEnclosures.length === 0 ? <span className="timeline-option-empty">No enclosures found.</span> : null}
            </div>
          </FilterPopover>
        ) : null}

        <FilterPopover label="Date" count={filters.from || filters.to ? 1 : 0} onClear={() => onFiltersChange({ ...filters, from: "", to: "" })}>
          <PopoverHeading title="Date range" />
          <div className="timeline-date-shortcuts">
            <button type="button" onClick={() => setDateShortcut("today", filters, onFiltersChange)}>Today</button>
            <button type="button" onClick={() => setDateShortcut("yesterday", filters, onFiltersChange)}>Yesterday</button>
            <button type="button" onClick={() => setDateShortcut("week", filters, onFiltersChange)}>Last 7 days</button>
            <button type="button" onClick={() => setDateShortcut("month", filters, onFiltersChange)}>Last 30 days</button>
          </div>
          <div className="timeline-date-fields">
            <label>
              <span>From</span>
              <input type="date" value={filters.from} onChange={(event) => onFiltersChange({ ...filters, from: event.target.value })} />
            </label>
            <label>
              <span>To</span>
              <input type="date" value={filters.to} onChange={(event) => onFiltersChange({ ...filters, to: event.target.value })} />
            </label>
          </div>
        </FilterPopover>

        <FilterPopover label="Performer" count={filters.performer ? 1 : 0} onClear={() => onFiltersChange({ ...filters, performer: "" })}>
          <PopoverHeading title="Performer" />
          <label className="timeline-popover-field">
            <span className="visually-hidden">Filter by performer</span>
            <input
              type="search"
              value={filters.performer}
              onChange={(event) => onFiltersChange({ ...filters, performer: event.target.value })}
              placeholder="Search performers..."
            />
          </label>
        </FilterPopover>
      </div>

      <div className="timeline-view-controls">
        {hasActiveFilters(filters) ? (
          <Button variant="ghost" className="timeline-clear-all" onClick={() => onFiltersChange({ eventTypes: [], performer: "", from: "", to: "" })}>
            Clear filters
          </Button>
        ) : null}

        {view === "list" ? (
          <>
            <FilterPopover label="Columns">
              <PopoverHeading title="Columns" />
              <div className="timeline-option-list">
                {timelineColumns.map((column) => (
                  <label key={column.id} className="timeline-check-option">
                    <input type="checkbox" checked={columns.includes(column.id)} onChange={() => toggleColumn(column.id)} />
                    <span>{column.label}</span>
                  </label>
                ))}
              </div>
            </FilterPopover>

            <FilterPopover label={sort === "Newest" ? "Newest first" : "Oldest first"}>
              <PopoverHeading title="Sort" />
              <div className="timeline-option-list">
                {(["Newest", "Oldest"] as ActivitySortDirection[]).map((option) => (
                  <label key={option} className="timeline-check-option">
                    <input type="radio" name="timeline-sort" checked={sort === option} onChange={() => onSortChange(option)} />
                    <span>{option} first</span>
                  </label>
                ))}
              </div>
            </FilterPopover>
          </>
        ) : null}

        {view === "range" ? (
          <FilterPopover label={`Group: ${getGroupByLabel(groupBy)}`}>
            <PopoverHeading title="Group range by" />
            <div className="timeline-option-list" role="radiogroup" aria-label="Group range by">
              {(["type", "animal", "enclosure"] as RangeGroupBy[]).map((option) => (
                <button
                  type="button"
                  role="radio"
                  aria-label={`Group by ${getGroupByLabel(option)}`}
                  aria-checked={groupBy === option}
                  className={groupBy === option ? "timeline-view-option timeline-view-option-active" : "timeline-view-option"}
                  key={option}
                  onClick={(event) => {
                    onGroupByChange(option);
                    closeContainingPopover(event);
                  }}
                >
                  {getGroupByLabel(option)} {groupBy === option ? <span>Selected</span> : null}
                </button>
              ))}
            </div>
          </FilterPopover>
        ) : null}

        <details className="timeline-popover">
          <summary className="timeline-control" aria-label={`View: ${getViewLabel(view)}`}>{getViewLabel(view)} <ChevronIcon /></summary>
          <div className="timeline-popover-panel timeline-popover-panel-right">
            <PopoverHeading title="View" />
            <div className="timeline-option-list" role="radiogroup" aria-label="Timeline view">
              {(["list", "day", "week", "range"] as TimelineView[]).map((option) => (
                <button
                  type="button"
                  role="radio"
                  aria-label={`${getViewLabel(option)} view`}
                  aria-checked={view === option}
                  className={view === option ? "timeline-view-option timeline-view-option-active" : "timeline-view-option"}
                  key={option}
                  onClick={(event) => {
                    onViewChange(option);
                    closeContainingPopover(event);
                  }}
                >
                  {getViewLabel(option)} {view === option ? <span>Selected</span> : null}
                </button>
              ))}
            </div>
          </div>
        </details>
      </div>
    </div>
  );
}

function FilterPopover({
  children,
  count = 0,
  label,
  onClear,
}: {
  children: React.ReactNode;
  count?: number;
  label: string;
  onClear?: () => void;
}) {
  return (
    <details className={count ? "timeline-popover timeline-popover-active" : "timeline-popover"}>
      <summary className="timeline-control">
        {label}{count ? ` (${count})` : ""} <ChevronIcon />
      </summary>
      <div className="timeline-popover-panel">
        {children}
        {count && onClear ? <button type="button" className="timeline-popover-clear" onClick={onClear}>Clear {label.toLowerCase()}</button> : null}
      </div>
    </details>
  );
}

function PopoverHeading({ title }: { title: string }) {
  return <strong className="timeline-popover-title">{title}</strong>;
}

function OptionSearch({ label, value, onChange }: { label: string; value: string; onChange: (value: string) => void }) {
  return (
    <label className="timeline-popover-field">
      <span className="visually-hidden">{label}</span>
      <input type="search" value={value} onChange={(event) => onChange(event.target.value)} placeholder={`${label}...`} />
    </label>
  );
}

function setDateShortcut(shortcut: "today" | "yesterday" | "week" | "month", filters: TimelineFilters, onChange: (filters: TimelineFilters) => void) {
  const end = new Date();
  const start = new Date();
  if (shortcut === "yesterday") {
    start.setDate(start.getDate() - 1);
    end.setDate(end.getDate() - 1);
  } else if (shortcut === "week") {
    start.setDate(start.getDate() - 6);
  } else if (shortcut === "month") {
    start.setDate(start.getDate() - 29);
  }
  onChange({ ...filters, from: toDateInputValue(start), to: toDateInputValue(end) });
}

function toDateInputValue(date: Date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function SearchIcon() {
  return <svg viewBox="0 0 20 20" aria-hidden="true"><circle cx="8.5" cy="8.5" r="5.5" /><path d="m12.5 12.5 4 4" /></svg>;
}

function ChevronIcon() {
  return <svg viewBox="0 0 16 16" aria-hidden="true"><path d="m4 6 4 4 4-4" /></svg>;
}

export function ActivityTypeMark({ type }: { type: ActivityEventType }) {
  return <span className={`activity-type-mark activity-type-${type.toLowerCase()}`} aria-hidden="true">{formatEnumLabel(type).charAt(0)}</span>;
}

function getViewLabel(view: TimelineView) {
  return view.charAt(0).toUpperCase() + view.slice(1);
}

function getGroupByLabel(groupBy: RangeGroupBy) {
  if (groupBy === "animal") return "Animal";
  if (groupBy === "enclosure") return "Enclosure";
  return "Event type";
}

function closeContainingPopover(event: MouseEvent<HTMLButtonElement>) {
  event.currentTarget.closest("details")?.removeAttribute("open");
}
