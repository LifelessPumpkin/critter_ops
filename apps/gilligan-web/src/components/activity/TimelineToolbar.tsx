"use client";

import { useEffect, useMemo, useRef, useState, type ReactNode, type RefObject } from "react";
import type { Animal } from "@/lib/api/animals";
import type { Enclosure } from "@/lib/api/enclosures";
import {
  activityEventTypes,
  activitySortOptions,
  formatEnumLabel,
  getActivitySortLabel,
  type ActivityEventType,
  type ActivitySortDirection,
} from "@/lib/api/activity";
import { Button } from "@/components/ui";
import { useDebouncedValue } from "@/components/activity/useDebouncedValue";
import {
  hasActiveFilters,
  timelineColumns,
  type RangeGroupBy,
  type TimelineColumnId,
  type TimelineContext,
  type TimelineFilters,
  type TimelineView,
} from "@/components/activity/timelineTypes";

type PopoverId = "type" | "date" | "more" | "columns" | "sort" | "group" | "view";
type SecondaryFilter = "animal" | "enclosure" | "performer";

type TimelineToolbarProps = {
  animals: Animal[];
  columns: TimelineColumnId[];
  context: TimelineContext;
  enclosures: Enclosure[];
  filters: TimelineFilters;
  groupBy: RangeGroupBy;
  performers: string[];
  search: string;
  searchInputRef: RefObject<HTMLInputElement | null>;
  sort: ActivitySortDirection;
  view: TimelineView;
  onColumnsChange: (columns: TimelineColumnId[]) => void;
  onFiltersChange: (filters: TimelineFilters) => void;
  onGroupByChange: (groupBy: RangeGroupBy) => void;
  onSearchChange: (search: string) => void;
  onSortChange: (sort: ActivitySortDirection) => void;
  onViewChange: (view: TimelineView) => void;
  onResetColumns: () => void;
};

export function TimelineToolbar({
  animals,
  columns,
  context,
  enclosures,
  filters,
  groupBy,
  performers,
  search,
  searchInputRef,
  sort,
  view,
  onColumnsChange,
  onFiltersChange,
  onGroupByChange,
  onSearchChange,
  onSortChange,
  onViewChange,
  onResetColumns,
}: TimelineToolbarProps) {
  const [openPopover, setOpenPopover] = useState<PopoverId | null>(null);
  const [secondaryFilter, setSecondaryFilter] = useState<SecondaryFilter>(() => getAvailableSecondaryFilters(context)[0] ?? "performer");
  const [animalSearch, setAnimalSearch] = useState("");
  const [enclosureSearch, setEnclosureSearch] = useState("");
  const [performerSearch, setPerformerSearch] = useState("");
  const toolbarRef = useRef<HTMLDivElement>(null);
  const triggerRefs = useRef(new Map<PopoverId, HTMLButtonElement>());
  const secondarySearchRef = useRef<HTMLInputElement>(null);
  const debouncedAnimalSearch = useDebouncedValue(animalSearch, 300);
  const debouncedEnclosureSearch = useDebouncedValue(enclosureSearch, 300);
  const debouncedPerformerSearch = useDebouncedValue(performerSearch, 300);
  const availableSecondaryFilters = getAvailableSecondaryFilters(context);
  const hiddenFilterCount = Number(Boolean(filters.animalId)) + Number(Boolean(filters.enclosureId)) + Number(Boolean(filters.performer));
  const filteredAnimals = useMemo(
    () => animals.filter((animal) => animal.name.toLowerCase().includes(debouncedAnimalSearch.toLowerCase())),
    [debouncedAnimalSearch, animals],
  );
  const filteredEnclosures = useMemo(
    () => enclosures.filter((enclosure) => enclosure.name.toLowerCase().includes(debouncedEnclosureSearch.toLowerCase())),
    [debouncedEnclosureSearch, enclosures],
  );
  const filteredPerformers = useMemo(() => {
    const normalizedSearch = debouncedPerformerSearch.trim().toLowerCase();
    return performers.filter((performer) => performer.toLowerCase().includes(normalizedSearch));
  }, [debouncedPerformerSearch, performers]);

  useEffect(() => {
    if (!openPopover) return;
    const activePopover = openPopover;

    function handlePointerDown(event: PointerEvent) {
      const popover = toolbarRef.current?.querySelector(`[data-timeline-popover="${activePopover}"]`);
      if (popover && !popover.contains(event.target as Node)) setOpenPopover(null);
    }

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key !== "Escape") return;
      event.preventDefault();
      const trigger = triggerRefs.current.get(activePopover);
      setOpenPopover(null);
      window.requestAnimationFrame(() => trigger?.focus());
    }

    document.addEventListener("pointerdown", handlePointerDown);
    document.addEventListener("keydown", handleKeyDown);
    return () => {
      document.removeEventListener("pointerdown", handlePointerDown);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [openPopover]);

  useEffect(() => {
    if (openPopover !== "more") return;
    const frame = window.requestAnimationFrame(() => secondarySearchRef.current?.focus());
    return () => window.cancelAnimationFrame(frame);
  }, [openPopover, secondaryFilter]);

  function toggleEventType(eventType: ActivityEventType) {
    const eventTypes = filters.eventTypes.includes(eventType)
      ? filters.eventTypes.filter((value) => value !== eventType)
      : [...filters.eventTypes, eventType];
    onFiltersChange({ ...filters, eventTypes });
  }

  function toggleColumn(column: TimelineColumnId) {
    if (columns.includes(column)) {
      if (columns.length > 1) onColumnsChange(columns.filter((value) => value !== column));
      return;
    }
    onColumnsChange([...columns, column]);
  }

  function moveColumn(column: TimelineColumnId, direction: -1 | 1) {
    const index = columns.indexOf(column);
    const nextIndex = index + direction;
    if (index < 0 || nextIndex < 0 || nextIndex >= columns.length) return;
    const reordered = [...columns];
    [reordered[index], reordered[nextIndex]] = [reordered[nextIndex], reordered[index]];
    onColumnsChange(reordered);
  }

  function renderSecondaryFilter() {
    if (secondaryFilter === "animal") {
      return (
        <>
          <OptionSearch inputRef={secondarySearchRef} label="Search animals" value={animalSearch} onChange={setAnimalSearch} />
          <div className="timeline-option-list timeline-option-list-scroll">
            {filteredAnimals.map((animal) => (
              <label key={animal.id} className="timeline-check-option">
                <input type="radio" name="timeline-animal" checked={filters.animalId === animal.id} onChange={() => onFiltersChange({ ...filters, animalId: animal.id })} />
                <span>{animal.name}</span>
              </label>
            ))}
            {filteredAnimals.length === 0 ? <span className="timeline-option-empty">No animals found.</span> : null}
          </div>
          {filters.animalId ? <button type="button" className="timeline-popover-clear" onClick={() => onFiltersChange({ ...filters, animalId: undefined })}>Clear animal</button> : null}
        </>
      );
    }

    if (secondaryFilter === "enclosure") {
      return (
        <>
          <OptionSearch inputRef={secondarySearchRef} label="Search enclosures" value={enclosureSearch} onChange={setEnclosureSearch} />
          <div className="timeline-option-list timeline-option-list-scroll">
            {filteredEnclosures.map((enclosure) => (
              <label key={enclosure.id} className="timeline-check-option">
                <input type="radio" name="timeline-enclosure" checked={filters.enclosureId === enclosure.id} onChange={() => onFiltersChange({ ...filters, enclosureId: enclosure.id })} />
                <span>{enclosure.name}</span>
              </label>
            ))}
            {filteredEnclosures.length === 0 ? <span className="timeline-option-empty">No enclosures found.</span> : null}
          </div>
          {filters.enclosureId ? <button type="button" className="timeline-popover-clear" onClick={() => onFiltersChange({ ...filters, enclosureId: undefined })}>Clear enclosure</button> : null}
        </>
      );
    }

    return (
      <>
        <OptionSearch inputRef={secondarySearchRef} label="Search performers" value={performerSearch} onChange={setPerformerSearch} />
        <strong className="timeline-option-section-title">All performers</strong>
        <div className="timeline-option-list timeline-option-list-scroll">
          {filteredPerformers.map((performer) => (
            <label key={performer} className="timeline-check-option">
              <input type="radio" name="timeline-performer" checked={filters.performer === performer} onChange={() => onFiltersChange({ ...filters, performer })} />
              <span>{performer}</span>
            </label>
          ))}
          {filteredPerformers.length === 0 ? <span className="timeline-option-empty">No performers found.</span> : null}
        </div>
        {filters.performer ? <button type="button" className="timeline-popover-clear" onClick={() => onFiltersChange({ ...filters, performer: "" })}>Clear performer</button> : null}
      </>
    );
  }

  return (
    <div className="timeline-toolbar" aria-label="Activity timeline controls" ref={toolbarRef}>
      <div className="timeline-toolbar-primary">
        <label className="timeline-search">
          <span className="visually-hidden">Search activities</span>
          <SearchIcon />
          <input ref={searchInputRef} type="search" value={search} onChange={(event) => onSearchChange(event.target.value)} placeholder="Search activities..." aria-keyshortcuts="/" />
        </label>

        <div className="timeline-property-controls">
          <ToolbarPopover id="type" label="Type" count={filters.eventTypes.length} openPopover={openPopover} setOpenPopover={setOpenPopover} triggerRefs={triggerRefs}>
            <PopoverHeading title="Event type" />
            <div className="timeline-option-list">
              {activityEventTypes.map((eventType) => (
                <label key={eventType} className="timeline-check-option">
                  <input type="checkbox" checked={filters.eventTypes.includes(eventType)} onChange={() => toggleEventType(eventType)} />
                  <ActivityTypeMark type={eventType} />
                  <span>{formatEnumLabel(eventType)}</span>
                </label>
              ))}
            </div>
            {filters.eventTypes.length ? <button type="button" className="timeline-popover-clear" onClick={() => onFiltersChange({ ...filters, eventTypes: [] })}>Clear types</button> : null}
          </ToolbarPopover>

          <ToolbarPopover id="date" label="Date" count={filters.from || filters.to ? 1 : 0} openPopover={openPopover} setOpenPopover={setOpenPopover} triggerRefs={triggerRefs}>
            <PopoverHeading title="Date range" />
            <div className="timeline-date-shortcuts">
              <button type="button" onClick={() => setDateShortcut("today", filters, onFiltersChange)}>Today</button>
              <button type="button" onClick={() => setDateShortcut("yesterday", filters, onFiltersChange)}>Yesterday</button>
              <button type="button" onClick={() => setDateShortcut("week", filters, onFiltersChange)}>Last 7 days</button>
              <button type="button" onClick={() => setDateShortcut("month", filters, onFiltersChange)}>Last 30 days</button>
            </div>
            <div className="timeline-date-fields">
              <label><span>From</span><input type="date" value={filters.from} onChange={(event) => onFiltersChange({ ...filters, from: event.target.value })} /></label>
              <label><span>To</span><input type="date" value={filters.to} onChange={(event) => onFiltersChange({ ...filters, to: event.target.value })} /></label>
            </div>
            {filters.from || filters.to ? <button type="button" className="timeline-popover-clear" onClick={() => onFiltersChange({ ...filters, from: "", to: "" })}>Clear date</button> : null}
          </ToolbarPopover>

          <ToolbarPopover id="more" label="More Filters" count={hiddenFilterCount} panelClassName="timeline-more-filters-panel" openPopover={openPopover} setOpenPopover={setOpenPopover} triggerRefs={triggerRefs}>
            <PopoverHeading title="More filters" />
            <div className="timeline-more-filters-layout">
              <div className="timeline-more-filter-tabs" role="tablist" aria-label="Additional filters">
                {availableSecondaryFilters.map((filter) => (
                  <button key={filter} type="button" role="tab" aria-selected={secondaryFilter === filter} onClick={() => setSecondaryFilter(filter)}>
                    {getSecondaryFilterLabel(filter)}{getSecondaryFilterCount(filter, filters) ? ` (${getSecondaryFilterCount(filter, filters)})` : ""}
                  </button>
                ))}
              </div>
              <div className="timeline-more-filter-content" role="tabpanel" aria-label={`${getSecondaryFilterLabel(secondaryFilter)} filter`}>
                {renderSecondaryFilter()}
              </div>
            </div>
          </ToolbarPopover>
        </div>
      </div>

      <div className="timeline-view-controls" aria-label="Timeline presentation controls">
        {hasActiveFilters(filters) ? <Button variant="ghost" className="timeline-clear-all" onClick={() => onFiltersChange({ eventTypes: [], performer: "", from: "", to: "" })}>Clear filters</Button> : null}

        {view === "list" ? (
          <>
            <ToolbarPopover id="columns" label="Columns" alignRight openPopover={openPopover} setOpenPopover={setOpenPopover} triggerRefs={triggerRefs}>
              <PopoverHeading title="Columns" />
              <div className="timeline-option-list">
                {[...columns, ...timelineColumns.map((column) => column.id).filter((column) => !columns.includes(column))].map((columnId) => (
                  <div key={columnId} className="timeline-column-option">
                    <label className="timeline-check-option"><input type="checkbox" checked={columns.includes(columnId)} onChange={() => toggleColumn(columnId)} /><span>{timelineColumns.find((column) => column.id === columnId)?.label}</span></label>
                    {columns.includes(columnId) ? (
                      <span className="timeline-column-order-controls">
                        <button type="button" aria-label={`Move ${columnId} column left`} disabled={columns.indexOf(columnId) === 0} onClick={() => moveColumn(columnId, -1)}>←</button>
                        <button type="button" aria-label={`Move ${columnId} column right`} disabled={columns.indexOf(columnId) === columns.length - 1} onClick={() => moveColumn(columnId, 1)}>→</button>
                      </span>
                    ) : null}
                  </div>
                ))}
              </div>
              <button type="button" className="timeline-popover-clear" onClick={onResetColumns}>Reset to default</button>
            </ToolbarPopover>

            <ToolbarPopover id="sort" label={getActivitySortLabel(sort)} alignRight openPopover={openPopover} setOpenPopover={setOpenPopover} triggerRefs={triggerRefs}>
              <PopoverHeading title="Sort" />
              <div className="timeline-option-list">
                {activitySortOptions.map((option) => (
                  <label key={option.value} className="timeline-check-option">
                    <input type="radio" name="timeline-sort" aria-label={option.label} checked={sort === option.value} onChange={() => { onSortChange(option.value); setOpenPopover(null); }} />
                    <span>{option.label}</span>
                  </label>
                ))}
              </div>
            </ToolbarPopover>
          </>
        ) : null}

        {view === "range" ? (
          <ToolbarPopover id="group" label={`Group: ${getGroupByLabel(groupBy)}`} alignRight openPopover={openPopover} setOpenPopover={setOpenPopover} triggerRefs={triggerRefs}>
            <PopoverHeading title="Group range by" />
            <div className="timeline-option-list" role="radiogroup" aria-label="Group range by">
              {(["type", "animal", "enclosure"] as RangeGroupBy[]).map((option) => (
                <button type="button" role="radio" aria-label={`Group by ${getGroupByLabel(option)}`} aria-checked={groupBy === option} className={groupBy === option ? "timeline-view-option timeline-view-option-active" : "timeline-view-option"} key={option} onClick={() => { onGroupByChange(option); setOpenPopover(null); }}>
                  {getGroupByLabel(option)} {groupBy === option ? <span>Selected</span> : null}
                </button>
              ))}
            </div>
          </ToolbarPopover>
        ) : null}

        <ToolbarPopover id="view" label={getViewLabel(view)} ariaLabel={`View: ${getViewLabel(view)}`} alignRight openPopover={openPopover} setOpenPopover={setOpenPopover} triggerRefs={triggerRefs}>
          <PopoverHeading title="View" />
          <div className="timeline-option-list" role="radiogroup" aria-label="Timeline view">
            {(["list", "day", "week", "range"] as TimelineView[]).map((option) => (
              <button type="button" role="radio" aria-label={`${getViewLabel(option)} view`} aria-checked={view === option} className={view === option ? "timeline-view-option timeline-view-option-active" : "timeline-view-option"} key={option} onClick={() => { setOpenPopover(null); onViewChange(option); }}>
                {getViewLabel(option)} {view === option ? <span>Selected</span> : null}
              </button>
            ))}
          </div>
        </ToolbarPopover>
      </div>

      {hiddenFilterCount ? (
        <div className="timeline-active-hidden-filters" aria-label="Active additional filters">
          {filters.animalId ? <ActiveFilterChip label={`Animal: ${animals.find((animal) => animal.id === filters.animalId)?.name ?? `#${filters.animalId}`}`} onRemove={() => onFiltersChange({ ...filters, animalId: undefined })} /> : null}
          {filters.enclosureId ? <ActiveFilterChip label={`Enclosure: ${enclosures.find((enclosure) => enclosure.id === filters.enclosureId)?.name ?? `#${filters.enclosureId}`}`} onRemove={() => onFiltersChange({ ...filters, enclosureId: undefined })} /> : null}
          {filters.performer ? <ActiveFilterChip label={`Performer: ${filters.performer}`} onRemove={() => onFiltersChange({ ...filters, performer: "" })} /> : null}
        </div>
      ) : null}
    </div>
  );
}

function ToolbarPopover({
  id, label, ariaLabel, count = 0, alignRight = false, panelClassName = "", children, openPopover, setOpenPopover, triggerRefs,
}: {
  id: PopoverId;
  label: string;
  ariaLabel?: string;
  count?: number;
  alignRight?: boolean;
  panelClassName?: string;
  children: ReactNode;
  openPopover: PopoverId | null;
  setOpenPopover: (id: PopoverId | null) => void;
  triggerRefs: RefObject<Map<PopoverId, HTMLButtonElement>>;
}) {
  const open = openPopover === id;
  return (
    <div className={count ? "timeline-popover timeline-popover-active" : "timeline-popover"} data-timeline-popover={id}>
      <button
        type="button"
        className="timeline-control"
        aria-label={ariaLabel}
        aria-haspopup="dialog"
        aria-expanded={open}
        ref={(element) => { if (element) triggerRefs.current.set(id, element); else triggerRefs.current.delete(id); }}
        onClick={() => setOpenPopover(open ? null : id)}
      >
        {label}{count ? ` (${count})` : ""} <ChevronIcon />
      </button>
      {open ? <div className={`timeline-popover-panel${alignRight ? " timeline-popover-panel-right" : ""}${panelClassName ? ` ${panelClassName}` : ""}`} role="dialog" aria-label={`${label} options`}>{children}</div> : null}
    </div>
  );
}

function ActiveFilterChip({ label, onRemove }: { label: string; onRemove: () => void }) {
  return <span className="timeline-active-filter-chip"><span>{label}</span><button type="button" aria-label={`Remove ${label} filter`} onClick={onRemove}>×</button></span>;
}

function PopoverHeading({ title }: { title: string }) {
  return <strong className="timeline-popover-title">{title}</strong>;
}

function OptionSearch({ inputRef, label, value, onChange }: { inputRef?: RefObject<HTMLInputElement | null>; label: string; value: string; onChange: (value: string) => void }) {
  return <label className="timeline-popover-field"><span className="visually-hidden">{label}</span><input ref={inputRef} type="search" value={value} onChange={(event) => onChange(event.target.value)} placeholder={`${label}...`} /></label>;
}

function getAvailableSecondaryFilters(context: TimelineContext): SecondaryFilter[] {
  return (["animal", "enclosure", "performer"] as SecondaryFilter[])
    .filter((filter) => !(context.kind === "animal" && filter === "animal"))
    .filter((filter) => !(context.kind === "enclosure" && filter === "enclosure"));
}

function getSecondaryFilterLabel(filter: SecondaryFilter) {
  return filter.charAt(0).toUpperCase() + filter.slice(1);
}

function getSecondaryFilterCount(filter: SecondaryFilter, filters: TimelineFilters) {
  if (filter === "animal") return Number(Boolean(filters.animalId));
  if (filter === "enclosure") return Number(Boolean(filters.enclosureId));
  return Number(Boolean(filters.performer));
}

function setDateShortcut(shortcut: "today" | "yesterday" | "week" | "month", filters: TimelineFilters, onChange: (filters: TimelineFilters) => void) {
  const end = new Date();
  const start = new Date();
  if (shortcut === "yesterday") {
    start.setDate(start.getDate() - 1);
    end.setDate(end.getDate() - 1);
  } else if (shortcut === "week") start.setDate(start.getDate() - 6);
  else if (shortcut === "month") start.setDate(start.getDate() - 29);
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
