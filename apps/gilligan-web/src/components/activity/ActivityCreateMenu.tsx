"use client";

import Link from "next/link";
import { useState } from "react";

const activityOptions = ["Feeding", "Treatment", "Movement", "Cleaning", "Water Change", "Inspection", "Maintenance", "Note", "General"] as const;

export function ActivityCreateMenu() {
  const [placeholder, setPlaceholder] = useState("");

  return (
    <details className="timeline-popover activity-create-menu">
      <summary className="button button-primary">+ Add Activity</summary>
      <div className="timeline-popover-panel timeline-popover-panel-right activity-create-panel">
        <strong className="timeline-popover-title">Add activity</strong>
        <p>Choose the activity workflow to start.</p>
        <div className="activity-create-options">
          {activityOptions.map((option) => (
            <button type="button" aria-label={`Add ${option} activity`} key={option} onClick={() => setPlaceholder(`${option} creation will be available in a future workflow.`)}>
              <span className="activity-type-mark" aria-hidden="true">{option.charAt(0)}</span>
              <span>{option}</span>
            </button>
          ))}
          <Link href="/tasks" aria-label="Add Task activity">
            <span className="activity-type-mark" aria-hidden="true">T</span>
            <span>Task</span>
          </Link>
        </div>
        {placeholder ? <p className="activity-create-placeholder" role="status">{placeholder}</p> : null}
      </div>
    </details>
  );
}
