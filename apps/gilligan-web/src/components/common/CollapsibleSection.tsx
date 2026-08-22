"use client";

import type { ReactNode } from "react";
import { useState } from "react";

type CollapsibleSectionProps = {
  title: string;
  children: ReactNode;
  defaultExpanded?: boolean;
  summary?: ReactNode;
};

export function CollapsibleSection({ title, children, defaultExpanded = true, summary }: CollapsibleSectionProps) {
  const [isExpanded, setIsExpanded] = useState(defaultExpanded);

  return (
    <section className="collapsible-section">
      <button
        type="button"
        className="collapsible-section-header"
        aria-expanded={isExpanded}
        onClick={() => setIsExpanded((currentValue) => !currentValue)}
      >
        <span className="collapsible-section-indicator" aria-hidden="true">
          {isExpanded ? "v" : ">"}
        </span>
        <span className="collapsible-section-title">{title}</span>
        {summary ? <span className="collapsible-section-summary">{summary}</span> : null}
      </button>

      {isExpanded ? <div className="collapsible-section-content">{children}</div> : null}
    </section>
  );
}
