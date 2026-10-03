import { SkeletonTable, SkeletonText, type SkeletonTableColumn } from "@/components/ui";

const columns: SkeletonTableColumn[] = [
  { header: "Name", width: "72%" },
  { header: "Type", width: "64%" },
  { header: "Status", width: "62%" },
  { header: "Size", width: "55%" },
  { header: "Capacity", width: "42%" },
  { header: "Material", width: "68%" },
];

export function EnclosureTableSkeleton() {
  return (
    <section className="location-section-list" role="status" aria-label="Loading enclosures">
      {[3, 2].map((rowCount, groupIndex) => (
        <div className="collapsible-section" key={rowCount} aria-hidden="true">
          <div className="collapsible-section-header skeleton-collapsible-header">
            <SkeletonText variant="heading" width={groupIndex === 0 ? "8rem" : "10rem"} />
            <SkeletonText variant="metadata" width="5.5rem" />
          </div>
          <div className="collapsible-section-content">
            <SkeletonTable columns={columns} rows={rowCount} tableClassName="enclosure-table" />
          </div>
        </div>
      ))}
      <span className="visually-hidden">Loading enclosures…</span>
    </section>
  );
}
