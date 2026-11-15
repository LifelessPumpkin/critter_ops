import { Button } from "@/components/ui";

export function CatalogNavigation({ catalog, noun }: { noun: string; catalog: {
  loading: boolean; error: string; page: number; hasMore: boolean | undefined;
  previous: () => void; next: () => void; retry: () => void;
} }) {
  return <div className="animal-selection-help">
    <p role="status">{catalog.loading ? `Searching ${noun}...` : catalog.error || `Page ${catalog.page}. Search to narrow results.`}</p>
    {catalog.error ? <Button type="button" variant="secondary" onClick={catalog.retry}>Retry</Button> : null}
    {catalog.page > 1 ? <Button type="button" variant="secondary" disabled={catalog.loading} onClick={catalog.previous}>Previous page</Button> : null}
    {catalog.hasMore ? <Button type="button" variant="secondary" disabled={catalog.loading} onClick={catalog.next}>Next page</Button> : null}
  </div>;
}
