import { SkeletonInput, SkeletonTable, SkeletonText, type SkeletonTableColumn } from "@/components/ui";

const columns: SkeletonTableColumn[] = [
  { header: "Name", width: "72%" },
  { header: "Type", width: "64%" },
  { header: "Species", width: "78%" },
  { header: "Status", width: "62%" },
  { header: "Sex", width: "48%" },
  { header: "Enclosure", width: "75%" },
  { header: "Acquired", width: "68%" },
];

export function AnimalTableSkeleton() {
  return (
    <div className="animal-overview-skeleton" role="status" aria-label="Loading animals">
      <div className="toolbar-row">
        <SkeletonInput width="min(100%, 26rem)" />
      </div>
      <div className="tab-list" aria-hidden="true">
        <SkeletonText className="skeleton-tab-control" width="9.5rem" />
        <SkeletonText className="skeleton-tab-control" width="10.5rem" />
        <SkeletonText className="skeleton-tab-control" width="10rem" />
      </div>
      <SkeletonTable ariaLabel="Loading animals table" columns={columns} rows={6} tableClassName="animal-table" />
      <span className="visually-hidden">Loading animals…</span>
    </div>
  );
}
