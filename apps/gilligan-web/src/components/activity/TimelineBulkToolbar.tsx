"use client";

import { useState } from "react";
import { Button } from "@/components/ui";

export function TimelineBulkToolbar({ count, onClear }: { count: number; onClear: () => void }) {
  const [message, setMessage] = useState("");
  const actions = ["Export", "Future edit", "Future delete", "Future tag"];

  return (
    <div className="timeline-bulk-toolbar" role="toolbar" aria-label={`${count} selected activities`}>
      <strong>{count} selected</strong>
      {actions.map((action) => (
        <Button key={action} variant="secondary" onClick={() => setMessage(`${action} is not available yet.`)}>{action}</Button>
      ))}
      <Button variant="ghost" onClick={onClear}>Clear selection</Button>
      <span role="status" className="timeline-bulk-message">{message}</span>
    </div>
  );
}
