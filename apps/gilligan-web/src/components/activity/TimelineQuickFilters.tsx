"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { toDateInputValue } from "@/components/activity/timelineDateUtils";
import type {
  RangeGroupBy,
  TimelineColumnId,
  TimelineFilters,
  TimelineSavedView,
  TimelineView,
} from "@/components/activity/timelineTypes";
import type { ActivityEventType, ActivitySortDirection } from "@/lib/api/activity";

type TimelineQuickFiltersProps = {
  columns: TimelineColumnId[];
  filters: TimelineFilters;
  groupBy: RangeGroupBy;
  savedViews: TimelineSavedView[];
  savedViewsError?: string;
  search: string;
  sort: ActivitySortDirection;
  view: TimelineView;
  onChange: (filters: TimelineFilters) => void;
  onApplySavedView: (savedView: TimelineSavedView) => void;
  onDeleteSavedView: (id: string) => void;
  onRenameSavedView: (id: string, name: string) => void;
  onSaveView: (name: string) => void;
  onTogglePin: (id: string) => void;
};

const typeFilters: Array<{ label: string; value: ActivityEventType }> = [
  { label: "Feedings", value: "Feeding" },
  { label: "Treatments", value: "Treatment" },
  { label: "Cleaning", value: "Cleaning" },
  { label: "Water changes", value: "WaterChange" },
  { label: "Inspections", value: "Inspection" },
  { label: "Maintenance", value: "Maintenance" },
  { label: "Tasks", value: "Task" },
];

export function TimelineQuickFilters({
  columns,
  filters,
  groupBy,
  savedViews,
  savedViewsError,
  search,
  sort,
  view,
  onChange,
  onApplySavedView,
  onDeleteSavedView,
  onRenameSavedView,
  onSaveView,
  onTogglePin,
}: TimelineQuickFiltersProps) {
  const [openPanel, setOpenPanel] = useState<"save" | "manage" | null>(null);
  const [name, setName] = useState("");
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editingName, setEditingName] = useState("");
  const rootRef = useRef<HTMLDivElement>(null);
  const saveTriggerRef = useRef<HTMLButtonElement>(null);
  const manageTriggerRef = useRef<HTMLButtonElement>(null);
  const nameInputRef = useRef<HTMLInputElement>(null);
  const orderedSavedViews = useMemo(
    () => savedViews.map((savedView, index) => ({ savedView, index })).sort((first, second) => Number(second.savedView.pinned) - Number(first.savedView.pinned) || first.index - second.index).map(({ savedView }) => savedView),
    [savedViews],
  );

  useEffect(() => {
    if (!openPanel) return;

    function handlePointerDown(event: PointerEvent) {
      if (!rootRef.current?.contains(event.target as Node)) setOpenPanel(null);
    }

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key !== "Escape") return;
      event.preventDefault();
      const trigger = openPanel === "save" ? saveTriggerRef.current : manageTriggerRef.current;
      setOpenPanel(null);
      setEditingId(null);
      window.requestAnimationFrame(() => trigger?.focus());
    }

    document.addEventListener("pointerdown", handlePointerDown);
    document.addEventListener("keydown", handleKeyDown);
    return () => {
      document.removeEventListener("pointerdown", handlePointerDown);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [openPanel]);

  useEffect(() => {
    if (openPanel !== "save") return;
    const frame = window.requestAnimationFrame(() => nameInputRef.current?.focus());
    return () => window.cancelAnimationFrame(frame);
  }, [openPanel]);

  function setDateRange(daysAgo: number, length: number) {
    setOpenPanel(null);
    const to = new Date();
    to.setDate(to.getDate() - daysAgo);
    const from = new Date(to);
    from.setDate(from.getDate() - (length - 1));
    onChange({ ...filters, from: toDateInputValue(from), to: toDateInputValue(to) });
  }

  function toggleType(eventType: ActivityEventType) {
    setOpenPanel(null);
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
    <div className="timeline-quick-views" aria-label="Quick and saved views" ref={rootRef}>
      <span className="timeline-quick-views-label">Quick views</span>
      <div className="timeline-quick-view-scroll" tabIndex={0} aria-label="Quick and saved view shortcuts">
        <button type="button" aria-pressed={filters.from === today && filters.to === today} onClick={() => setDateRange(0, 1)}>Today</button>
        <button type="button" aria-pressed={filters.from === yesterday && filters.to === yesterday} onClick={() => setDateRange(1, 1)}>Yesterday</button>
        <button type="button" aria-pressed={filters.from === weekStart && filters.to === today} onClick={() => setDateRange(0, 7)}>Last 7 days</button>
        {typeFilters.map((filter) => (
          <button type="button" key={filter.value} aria-pressed={filters.eventTypes.includes(filter.value)} onClick={() => toggleType(filter.value)}>{filter.label}</button>
        ))}
        {orderedSavedViews.map((savedView) => (
          <button
            type="button"
            key={savedView.id}
            className="timeline-saved-view-chip"
            aria-pressed={isSavedViewActive(savedView, { columns, filters, groupBy, search, sort, view })}
            onClick={() => { setOpenPanel(null); onApplySavedView(savedView); }}
          >
            {savedView.pinned ? <span aria-hidden="true">★</span> : null}{savedView.name}
          </button>
        ))}
      </div>

      <div className="timeline-quick-view-actions">
        <button ref={saveTriggerRef} type="button" className="timeline-save-view-button" aria-expanded={openPanel === "save"} onClick={() => setOpenPanel(openPanel === "save" ? null : "save")}>+ Save View</button>
        {savedViews.length ? <button ref={manageTriggerRef} type="button" className="timeline-manage-views-button" aria-label="Manage saved views" aria-expanded={openPanel === "manage"} onClick={() => setOpenPanel(openPanel === "manage" ? null : "manage")}>Manage</button> : null}

        {openPanel === "save" ? (
          <form
            className="timeline-saved-views-panel timeline-save-view-panel"
            onSubmit={(event) => {
              event.preventDefault();
              const value = name.trim();
              if (!value) return;
              onSaveView(value);
              setName("");
              setOpenPanel(null);
            }}
          >
            <strong>Save current view</strong>
            <label><span>View name</span><input ref={nameInputRef} value={name} maxLength={60} onChange={(event) => setName(event.target.value)} placeholder="Medical activity" /></label>
            <button type="submit">Save view</button>
          </form>
        ) : null}

        {openPanel === "manage" ? (
          <div className="timeline-saved-views-panel timeline-manage-views-panel" role="dialog" aria-label="Manage saved views">
            <strong>Saved Views</strong>
            <div className="timeline-saved-view-list">
              {orderedSavedViews.map((savedView) => (
                <div className="timeline-saved-view-row" key={savedView.id}>
                  {editingId === savedView.id ? (
                    <form onSubmit={(event) => { event.preventDefault(); const value = editingName.trim(); if (value) onRenameSavedView(savedView.id, value); setEditingId(null); }}>
                      <input autoFocus aria-label={`Rename ${savedView.name}`} value={editingName} maxLength={60} onChange={(event) => setEditingName(event.target.value)} />
                      <button type="submit">Save</button>
                    </form>
                  ) : <span title={savedView.name}>{savedView.name}</span>}
                  <div>
                    <button type="button" aria-label={`${savedView.pinned ? "Unpin" : "Pin"} ${savedView.name}`} onClick={() => onTogglePin(savedView.id)}>{savedView.pinned ? "★" : "☆"}</button>
                    <button type="button" aria-label={`Rename ${savedView.name}`} onClick={() => { setEditingId(savedView.id); setEditingName(savedView.name); }}>Rename</button>
                    <button type="button" aria-label={`Delete ${savedView.name}`} onClick={() => { onDeleteSavedView(savedView.id); if (savedViews.length === 1) setOpenPanel(null); }}>Delete</button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        ) : null}
      </div>
      {savedViewsError ? <span className="timeline-saved-views-error" role="status">{savedViewsError}</span> : null}
    </div>
  );
}

function isSavedViewActive(savedView: TimelineSavedView, current: {
  columns: TimelineColumnId[];
  filters: TimelineFilters;
  groupBy: RangeGroupBy;
  search: string;
  sort: ActivitySortDirection;
  view: TimelineView;
}) {
  const sameFilters = JSON.stringify(savedView.filters) === JSON.stringify(current.filters);
  const sameColumns = !savedView.columns || JSON.stringify(savedView.columns) === JSON.stringify(current.columns);
  return sameFilters && sameColumns && (savedView.search ?? "") === current.search && savedView.groupBy === current.groupBy && savedView.sort === current.sort && savedView.view === current.view;
}
