import { SkeletonButton, SkeletonInput, SkeletonTable, SkeletonText, type SkeletonTableColumn } from "@/components/ui";

const columns: SkeletonTableColumn[] = [
  { header: "Animal", width: "72%" },
  { header: "Sex", width: "54%" },
  { header: "Species", width: "78%" },
  { header: "Age / DOB", width: "68%" },
  { header: "Morph / Breed", width: "64%" },
  { header: "Status", width: "62%" },
  { header: "Enclosure", width: "75%" },
];

export function AnimalTableSkeleton() {
  return (
    <div className="animal-overview-skeleton" role="status" aria-label="Loading animals">
      <div className="tab-list" aria-hidden="true">
        <SkeletonText className="skeleton-tab-control" width="6rem" />
        <SkeletonText className="skeleton-tab-control" width="7rem" />
        <SkeletonText className="skeleton-tab-control" width="8rem" />
      </div>
      <div className="animal-toolbar" aria-hidden="true">
        <SkeletonInput width="min(100%, 24rem)" />
        <SkeletonButton width="7rem" />
        <SkeletonButton width="7rem" />
        <SkeletonButton width="8rem" />
      </div>
      <SkeletonTable ariaLabel="Loading animals table" columns={columns} rows={8} tableClassName="animal-table" />
      <span className="visually-hidden">Loading animals…</span>
    </div>
  );
}
