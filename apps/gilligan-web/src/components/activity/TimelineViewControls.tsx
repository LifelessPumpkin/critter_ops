import { Button } from "@/components/ui";

type TimelineViewControlsProps = {
  label: string;
  jumpLabel: string;
  onNext: () => void;
  onPrevious: () => void;
  onJump: () => void;
  dateValue?: string;
  onDateChange?: (date: string) => void;
};

export function TimelineViewControls({
  label,
  jumpLabel,
  onNext,
  onPrevious,
  onJump,
  dateValue,
  onDateChange,
}: TimelineViewControlsProps) {
  return (
    <div className="timeline-navigation">
      <div className="timeline-navigation-stepper">
        <Button variant="secondary" className="timeline-navigation-arrow" aria-label="Previous period" onClick={onPrevious}>
          <ArrowIcon direction="left" />
        </Button>
        <strong>{label}</strong>
        <Button variant="secondary" className="timeline-navigation-arrow" aria-label="Next period" onClick={onNext}>
          <ArrowIcon direction="right" />
        </Button>
      </div>
      <div className="timeline-navigation-actions">
        {dateValue && onDateChange ? (
          <label className="timeline-jump-date">
            <span className="visually-hidden">Select date</span>
            <input type="date" value={dateValue} onChange={(event) => onDateChange(event.target.value)} />
          </label>
        ) : null}
        <Button variant="ghost" onClick={onJump}>{jumpLabel}</Button>
      </div>
    </div>
  );
}

function ArrowIcon({ direction }: { direction: "left" | "right" }) {
  return (
    <svg viewBox="0 0 16 16" aria-hidden="true" className={direction === "right" ? "timeline-arrow-right" : undefined}>
      <path d="m10 3-5 5 5 5" />
    </svg>
  );
}
