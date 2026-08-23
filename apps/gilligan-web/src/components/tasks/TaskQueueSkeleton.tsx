export function TaskQueueSkeleton() {
  return (
    <div className="task-queue animated-fade-in" aria-label="Loading tasks">
      <div className="tab-list task-tabs">
        <span className="skeleton skeleton-tab" />
        <span className="skeleton skeleton-tab" />
        <span className="skeleton skeleton-tab" />
      </div>
      <div className="task-section">
        <div className="task-section-heading">
          <span className="skeleton skeleton-heading" />
        </div>
        <div className="task-list">
          <span className="skeleton skeleton-row" />
          <span className="skeleton skeleton-row" />
          <span className="skeleton skeleton-row" />
        </div>
      </div>
    </div>
  );
}
