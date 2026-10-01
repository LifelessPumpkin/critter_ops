import { Button } from "@/components/ui";

export function TimelineEmptyState({ message, filtered, filteredMessage, onClear }: { message: string; filtered: boolean; filteredMessage?: string; onClear: () => void }) {
  return (
    <div className="timeline-state">
      <strong>{filtered ? filteredMessage ?? "No activity matches the selected filters." : message}</strong>
      {filtered ? <Button variant="secondary" onClick={onClear}>Clear filters</Button> : null}
    </div>
  );
}
