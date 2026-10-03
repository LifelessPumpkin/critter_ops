import { SkeletonBadge, SkeletonButton, SkeletonText } from "@/components/ui";

export function TaskQueueSkeleton() {
  return (
    <div className="task-queue" role="status" aria-label="Loading tasks">
      <div className="task-queue-toolbar" aria-hidden="true">
        <div className="tab-list task-tabs">
          <SkeletonText className="skeleton-tab-control" width="5rem" />
          <SkeletonText className="skeleton-tab-control" width="7rem" />
          <SkeletonText className="skeleton-tab-control" width="6rem" />
        </div>
        <SkeletonButton width="6.5rem" />
      </div>
      <TaskSectionSkeleton titleWidth="5rem" rows={2} />
      <TaskSectionSkeleton titleWidth="4rem" rows={3} />
      <TaskSectionSkeleton titleWidth="6.5rem" rows={2} />
      <span className="visually-hidden">Loading tasks…</span>
    </div>
  );
}

function TaskSectionSkeleton({ rows, titleWidth }: { rows: number; titleWidth: string }) {
  return (
    <div className="task-section" aria-hidden="true">
      <div className="task-section-heading">
        <SkeletonText variant="heading" width={titleWidth} />
        <SkeletonText variant="metadata" width="1rem" />
      </div>
      <div className="task-list">
        {Array.from({ length: rows }, (_, rowIndex) => (
          <div className="task-row task-skeleton-row" key={rowIndex}>
            <div className="task-row-main">
              <div className="task-row-title-line">
                <SkeletonText variant="body" width={rowIndex % 2 === 0 ? "14rem" : "18rem"} />
                <SkeletonBadge width="4.5rem" />
              </div>
              <SkeletonText variant="metadata" width="9rem" />
              <div className="task-row-details">
                <SkeletonBadge width="4rem" />
                <SkeletonBadge width="6rem" />
              </div>
            </div>
            <SkeletonButton size="small" width="5.5rem" />
          </div>
        ))}
      </div>
    </div>
  );
}
