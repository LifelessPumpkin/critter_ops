"use client";

import { useState } from "react";
import type { TimelinePreset } from "@/components/activity/timelineTypes";

type TimelinePresetControlProps = {
  presets: TimelinePreset[];
  onApply: (preset: TimelinePreset) => void;
  onDelete: (id: string) => void;
  onSave: (name: string) => void;
};

export function TimelinePresetControl({ presets, onApply, onDelete, onSave }: TimelinePresetControlProps) {
  const [name, setName] = useState("");

  return (
    <details className="timeline-popover">
      <summary className="timeline-control">Presets <ChevronIcon /></summary>
      <div className="timeline-popover-panel timeline-popover-panel-right timeline-presets-panel">
        <strong className="timeline-popover-title">Saved filter presets</strong>
        {presets.length ? (
          <div className="timeline-preset-list">
            {presets.map((preset) => (
              <div key={preset.id}>
                <button type="button" aria-label={`Apply ${preset.name} preset`} onClick={() => onApply(preset)}>{preset.name}</button>
                <button type="button" aria-label={`Delete ${preset.name} preset`} onClick={() => onDelete(preset.id)}>×</button>
              </div>
            ))}
          </div>
        ) : <p className="timeline-option-empty">No saved presets.</p>}
        <form
          className="timeline-preset-form"
          onSubmit={(event) => {
            event.preventDefault();
            const value = name.trim();
            if (!value) return;
            onSave(value);
            setName("");
          }}
        >
          <label>
            <span className="visually-hidden">Preset name</span>
            <input aria-label="Preset name" value={name} maxLength={60} onChange={(event) => setName(event.target.value)} placeholder="Preset name..." />
          </label>
          <button type="submit" aria-label="Save current filters as preset">Save current</button>
        </form>
      </div>
    </details>
  );
}

function ChevronIcon() {
  return <svg viewBox="0 0 16 16" aria-hidden="true"><path d="m4 6 4 4 4-4" /></svg>;
}
