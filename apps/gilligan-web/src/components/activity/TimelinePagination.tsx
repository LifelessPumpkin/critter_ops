import { Button } from "@/components/ui";

type TimelinePaginationProps = {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  onPageChange: (page: number) => void;
};

export function TimelinePagination({ page, pageSize, totalCount, totalPages, onPageChange }: TimelinePaginationProps) {
  if (totalPages <= 1) return null;
  const first = (page - 1) * pageSize + 1;
  const last = Math.min(totalCount, page * pageSize);
  return (
    <nav className="timeline-pagination" aria-label="Activity pages">
      <span>Showing {first}–{last} of {totalCount}</span>
      <div>
        <Button variant="secondary" disabled={page <= 1} onClick={() => onPageChange(page - 1)}>Previous page</Button>
        <span>Page {page} of {totalPages}</span>
        <Button variant="secondary" disabled={page >= totalPages} onClick={() => onPageChange(page + 1)}>Next page</Button>
      </div>
    </nav>
  );
}
