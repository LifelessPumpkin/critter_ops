import { Button } from "@/components/ui";

export function TimelineEmptyState({ message, filtered, onClear }: { message: string; filtered: boolean; onClear: () => void }) {
  return (
    <div className="timeline-state">
      <strong>{filtered ? "No activities match the selected filters." : message}</strong>
      {filtered ? <Button variant="secondary" onClick={onClear}>Clear filters</Button> : null}
    </div>
  );
}
