"use client";

import { useEffect, useRef } from "react";
import { Button } from "@/components/ui";
import { formatAssociationNames } from "@/components/activity/timelineDateUtils";
import { formatEnumLabel, type ActivityRecord } from "@/lib/api/activity";

const timestampFormatter = new Intl.DateTimeFormat("en", {
  dateStyle: "medium",
  timeStyle: "short",
});

type ActivityDetailDrawerProps = {
  activity: ActivityRecord;
  hasNext: boolean;
  hasPrevious: boolean;
  onClose: () => void;
  onNext: () => void;
  onPrevious: () => void;
};

export function ActivityDetailDrawer({
  activity,
  hasNext,
  hasPrevious,
  onClose,
  onNext,
  onPrevious,
}: ActivityDetailDrawerProps) {
  const closeButtonRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    closeButtonRef.current?.focus();
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        onClose();
      }
    }
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [onClose]);

  const structuredDetails = getStructuredDetails(activity);

  return (
    <>
      <button className="activity-drawer-scrim" type="button" aria-label="Close activity details" onClick={onClose} />
      <aside className="activity-detail-drawer" role="dialog" aria-modal="false" aria-labelledby="activity-detail-title">
        <header className="activity-detail-header">
          <div>
            <span className="activity-timeline-eyebrow">Activity #{activity.id}</span>
            <h2 id="activity-detail-title">{formatEnumLabel(activity.type)}</h2>
          </div>
          <Button ref={closeButtonRef} variant="ghost" className="activity-detail-close" aria-label="Close activity details" onClick={onClose}>×</Button>
        </header>

        <div className="activity-detail-navigation" aria-label="Activity navigation">
          <Button variant="secondary" disabled={!hasPrevious} onClick={onPrevious}>Previous</Button>
          <Button variant="secondary" disabled={!hasNext} onClick={onNext}>Next</Button>
        </div>

        <div className="activity-detail-body">
          <DetailSection title="Overview">
            <DetailList
              entries={[
                ["Activity type", formatEnumLabel(activity.type)],
                ["Occurred", timestampFormatter.format(new Date(activity.occurredAt))],
                ["Created", timestampFormatter.format(new Date(activity.createdAt))],
                ["Animal", formatAssociationNames(activity.animals)],
                ["Enclosure", formatAssociationNames(activity.enclosures)],
                ["Performed by", activity.performer || "—"],
                ["Source", activity.sourceType || "—"],
              ]}
            />
          </DetailSection>

          <DetailSection title="Description">
            <p>{activity.description}</p>
            {activity.notes && !activity.description.includes(activity.notes) ? <p className="activity-detail-notes">{activity.notes}</p> : null}
          </DetailSection>

          {structuredDetails.length ? (
            <DetailSection title="Activity details">
              <DetailList entries={structuredDetails} />
            </DetailSection>
          ) : null}

          {activity.metadata != null ? (
            <DetailSection title="Metadata">
              <pre className="activity-detail-metadata">{formatMetadata(activity.metadata)}</pre>
            </DetailSection>
          ) : null}
        </div>
      </aside>
    </>
  );
}

function DetailSection({ children, title }: { children: React.ReactNode; title: string }) {
  return (
    <section className="activity-detail-section">
      <h3>{title}</h3>
      {children}
    </section>
  );
}

function DetailList({ entries }: { entries: Array<[string, string]> }) {
  return (
    <dl className="activity-detail-list">
      {entries.map(([label, value]) => (
        <div key={label}>
          <dt>{label}</dt>
          <dd>{value}</dd>
        </div>
      ))}
    </dl>
  );
}

function getStructuredDetails(activity: ActivityRecord): Array<[string, string]> {
  const detail = Object.values(activity.details).find(Boolean);
  if (!detail || typeof detail !== "object") {
    return [];
  }
  return Object.entries(detail)
    .filter(([, value]) => value !== null && value !== undefined && value !== "")
    .map(([key, value]) => [formatFieldLabel(key), formatDetailValue(value)]);
}

function formatFieldLabel(value: string) {
  const label = formatEnumLabel(value);
  return label.charAt(0).toUpperCase() + label.slice(1);
}

function formatDetailValue(value: unknown) {
  if (typeof value === "boolean") return value ? "Yes" : "No";
  if (typeof value === "string") return formatEnumLabel(value);
  return String(value);
}

function formatMetadata(metadata: unknown) {
  if (typeof metadata === "string") return metadata;
  try {
    return JSON.stringify(metadata, null, 2);
  } catch {
    return String(metadata);
  }
}
