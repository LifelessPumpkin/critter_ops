"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { AnimalTableSkeleton } from "@/components/animals/AnimalTableSkeleton";
import { AnimalTable } from "@/components/animals/AnimalTable";
import { useDebouncedValue } from "@/components/activity/useDebouncedValue";
import { Button, Combobox, Input, MultiSelect } from "@/components/ui";
import {
  animalPageSize,
  type AnimalLifecycle,
  type AnimalListItem,
  type AnimalSearchResponse,
  searchAnimals,
} from "@/lib/api/animals";
import { type Enclosure, fetchEnclosures } from "@/lib/api/enclosures";

type LoadState = "loading" | "loaded" | "error";
type AdditionalLoadState = "idle" | "loading" | "error";

const statusOptions = ["Active", "Quarantined", "Medical", "Breeding", "OnHold", "Transferred", "Sold", "Deceased", "Released", "Surrendered", "Inactive"];
const animalTypeOptions = ["Mammal", "Avian", "Herptile", "Aquatic", "Invertebrate", "Amphibian", "Reptile", "Fish", "Other"];
const defaultPage: AnimalSearchResponse = {
  page: 0,
  pageSize: animalPageSize,
  totalCount: 0,
  totalPages: 0,
  allCount: 0,
  inCareCount: 0,
  outOfCareCount: 0,
  items: [],
};

export type AnimalOverviewInitialState = {
  lifecycle?: string;
  search?: string;
  status?: string;
  type?: string;
  enclosure?: string;
};

export function AnimalOverview({ initialState = {} }: { initialState?: AnimalOverviewInitialState }) {
  const [lifecycle, setLifecycle] = useState<AnimalLifecycle>(() => parseLifecycle(initialState.lifecycle));
  const [search, setSearch] = useState(initialState.search ?? "");
  const [statuses, setStatuses] = useState<string[]>(() => parseList(initialState.status, statusOptions));
  const [animalTypes, setAnimalTypes] = useState<string[]>(() => parseList(initialState.type, animalTypeOptions));
  const [enclosureId, setEnclosureId] = useState<number | undefined>(() => parseNumber(initialState.enclosure));
  const [enclosures, setEnclosures] = useState<Enclosure[]>([]);
  const [enclosuresLoading, setEnclosuresLoading] = useState(true);
  const [animalPage, setAnimalPage] = useState(defaultPage);
  const [loadState, setLoadState] = useState<LoadState>("loading");
  const [additionalLoadState, setAdditionalLoadState] = useState<AdditionalLoadState>("idle");
  const [reloadKey, setReloadKey] = useState(0);
  const sentinelRef = useRef<HTMLDivElement>(null);
  const activeRequestKeyRef = useRef("");
  const inFlightPageRef = useRef<number | null>(null);
  const loadMoreAbortRef = useRef<AbortController | null>(null);
  const deferredSearch = useDebouncedValue(search.trim(), 300);

  const query = useMemo(() => ({
    lifecycle,
    search: deferredSearch || undefined,
    statuses,
    animalTypes,
    enclosureId,
    pageSize: animalPageSize,
  }), [animalTypes, deferredSearch, enclosureId, lifecycle, statuses]);
  const requestKey = useMemo(() => `${JSON.stringify(query)}:${reloadKey}`, [query, reloadKey]);

  useEffect(() => {
    const controller = new AbortController();
    activeRequestKeyRef.current = requestKey;
    loadMoreAbortRef.current?.abort();
    loadMoreAbortRef.current = null;
    inFlightPageRef.current = null;
    setAdditionalLoadState("idle");
    setLoadState("loading");

    searchAnimals({ ...query, page: 1 }, controller.signal)
      .then((response) => {
        if (activeRequestKeyRef.current !== requestKey) return;
        setAnimalPage({ ...response, items: deduplicateAnimals(response.items) });
        setLoadState("loaded");
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") return;
        if (activeRequestKeyRef.current !== requestKey) return;
        console.error(error);
        setLoadState("error");
      });

    return () => controller.abort();
  }, [query, requestKey]);

  useEffect(() => {
    let active = true;
    fetchEnclosures()
      .then((data) => { if (active) setEnclosures(data); })
      .catch((error) => console.error("Unable to load enclosure filter options", error))
      .finally(() => { if (active) setEnclosuresLoading(false); });
    return () => { active = false; };
  }, []);

  useEffect(() => {
    const url = new URL(window.location.href);
    setUrlParameter(url, "lifecycle", lifecycle === "InCare" ? undefined : lifecycle === "OutOfCare" ? "out-of-care" : "all");
    setUrlParameter(url, "search", search.trim() || undefined);
    setUrlParameter(url, "status", statuses.length ? statuses.join(",") : undefined);
    setUrlParameter(url, "type", animalTypes.length ? animalTypes.join(",") : undefined);
    setUrlParameter(url, "enclosure", enclosureId);
    window.history.replaceState(window.history.state, "", url);
  }, [animalTypes, enclosureId, lifecycle, search, statuses]);

  const hasMore = animalPage.page > 0 && animalPage.page < animalPage.totalPages;
  const loadNextPage = useCallback(() => {
    if (loadState !== "loaded" || !hasMore || additionalLoadState === "loading") return;
    const nextPage = animalPage.page + 1;
    if (inFlightPageRef.current === nextPage) return;
    const controller = new AbortController();
    loadMoreAbortRef.current?.abort();
    loadMoreAbortRef.current = controller;
    inFlightPageRef.current = nextPage;
    setAdditionalLoadState("loading");

    searchAnimals({ ...query, page: nextPage }, controller.signal)
      .then((response) => {
        if (activeRequestKeyRef.current !== requestKey || inFlightPageRef.current !== nextPage) return;
        setAnimalPage((current) => ({
          ...response,
          page: Math.max(current.page, response.page),
          items: deduplicateAnimals([...current.items, ...response.items]),
        }));
        setAdditionalLoadState("idle");
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") return;
        if (activeRequestKeyRef.current !== requestKey) return;
        console.error(error);
        setAdditionalLoadState("error");
      })
      .finally(() => {
        if (inFlightPageRef.current === nextPage) inFlightPageRef.current = null;
      });
  }, [additionalLoadState, animalPage.page, hasMore, loadState, query, requestKey]);

  useEffect(() => {
    const sentinel = sentinelRef.current;
    if (!sentinel || !hasMore || loadState !== "loaded" || additionalLoadState === "error") return;
    const observer = new IntersectionObserver(
      ([entry]) => { if (entry.isIntersecting) loadNextPage(); },
      { rootMargin: "0px 0px 480px", threshold: 0 },
    );
    observer.observe(sentinel);
    return () => observer.disconnect();
  }, [additionalLoadState, hasMore, loadNextPage, loadState]);

  const selectedEnclosure = enclosures.find((enclosure) => enclosure.id === enclosureId);
  const hasFilters = Boolean(search.trim() || statuses.length || animalTypes.length || enclosureId);

  if (loadState === "loading") return <AnimalTableSkeleton />;

  if (loadState === "error") {
    return (
      <div className="state-panel animal-error-state" role="alert">
        <div><h2>Unable to load animals.</h2><p>Check the connection and try again.</p></div>
        <Button type="button" onClick={() => setReloadKey((current) => current + 1)}>Retry</Button>
      </div>
    );
  }

  return (
    <section className="animal-console" aria-label="Animal management">
      <LifecycleTabs lifecycle={lifecycle} counts={{ All: animalPage.allCount, InCare: animalPage.inCareCount, OutOfCare: animalPage.outOfCareCount }} onChange={setLifecycle} />

      <div className="animal-toolbar">
        <Input className="animal-search-input" label="Search animals" labelHidden placeholder="Search animals..." type="search" value={search} onChange={(event) => setSearch(event.target.value)} />
        <MultiSelect label="Status" labelHidden value={statuses} options={statusOptions.map(toSelectionOption)} onValueChange={setStatuses} />
        <MultiSelect label="Animal Type" labelHidden value={animalTypes} options={animalTypeOptions.map(toSelectionOption)} onValueChange={setAnimalTypes} />
        <Combobox
          className="animal-filter-select"
          label="Enclosure"
          labelHidden
          value={enclosureId ? String(enclosureId) : ""}
          placeholder="Search enclosures"
          searchPlaceholder="Search enclosures..."
          loading={enclosuresLoading}
          emptyMessage="No enclosures available."
          options={enclosures.slice().sort((first, second) => first.name.localeCompare(second.name)).map((enclosure) => ({ label: enclosure.name, value: String(enclosure.id) }))}
          onValueChange={(value) => setEnclosureId(value ? Number(value) : undefined)}
        />
      </div>

      {hasFilters ? (
        <div className="animal-active-filters" aria-label="Active animal filters">
          {search.trim() ? <FilterChip label={`Search: ${search.trim()}`} onRemove={() => setSearch("")} /> : null}
          {statuses.map((status) => <FilterChip key={status} label={`Status: ${formatEnumLabel(status)}`} onRemove={() => setStatuses(statuses.filter((value) => value !== status))} />)}
          {animalTypes.map((type) => <FilterChip key={type} label={`Type: ${formatEnumLabel(type)}`} onRemove={() => setAnimalTypes(animalTypes.filter((value) => value !== type))} />)}
          {enclosureId ? <FilterChip label={`Enclosure: ${selectedEnclosure?.name ?? `#${enclosureId}`}`} onRemove={() => setEnclosureId(undefined)} /> : null}
          <Button variant="ghost" className="animal-clear-filters" type="button" onClick={() => { setSearch(""); setStatuses([]); setAnimalTypes([]); setEnclosureId(undefined); }}>Clear all</Button>
        </div>
      ) : null}

      {animalPage.items.length ? (
        <>
          <div className="animal-table-frame">
            <AnimalTable animals={animalPage.items} ariaLabel={`${formatLifecycle(lifecycle)} animals`} loadingMore={additionalLoadState === "loading"} />
          </div>
          {additionalLoadState === "error" ? <div className="animal-load-more-error" role="alert"><span>Couldn’t load more animals.</span><Button variant="secondary" type="button" onClick={loadNextPage}>Retry</Button></div> : null}
          <div ref={sentinelRef} className="animal-scroll-sentinel" aria-hidden="true" />
          {!hasMore && animalPage.totalCount > animalPage.pageSize ? <p className="animal-results-end">All {animalPage.totalCount} matching animals are shown.</p> : null}
        </>
      ) : <EmptyState lifecycle={lifecycle} hasFilters={hasFilters} onClear={() => { setSearch(""); setStatuses([]); setAnimalTypes([]); setEnclosureId(undefined); }} />}
    </section>
  );
}

function LifecycleTabs({ lifecycle, counts, onChange }: { lifecycle: AnimalLifecycle; counts: Record<AnimalLifecycle, number>; onChange: (value: AnimalLifecycle) => void }) {
  const tabs: { value: AnimalLifecycle; label: string }[] = [{ value: "All", label: "All" }, { value: "InCare", label: "In Care" }, { value: "OutOfCare", label: "Out of Care" }];
  return <div className="tab-list animal-lifecycle-tabs" role="tablist" aria-label="Animal lifecycle">{tabs.map((tab) => <button key={tab.value} className={lifecycle === tab.value ? "tab-button tab-button-active" : "tab-button"} type="button" role="tab" aria-selected={lifecycle === tab.value} onClick={() => onChange(tab.value)}>{tab.label} <span>{counts[tab.value]}</span></button>)}</div>;
}

function FilterChip({ label, onRemove }: { label: string; onRemove: () => void }) {
  return <span className="animal-filter-chip"><span>{label}</span><button type="button" aria-label={`Remove ${label}`} onClick={onRemove}>×</button></span>;
}

function EmptyState({ lifecycle, hasFilters, onClear }: { lifecycle: AnimalLifecycle; hasFilters: boolean; onClear: () => void }) {
  let title = "No animals have been added yet.";
  let description = "Add your first animal to begin managing your collection.";
  if (hasFilters) { title = "No animals match the selected filters."; description = "Adjust or clear the filters to broaden your results."; }
  else if (lifecycle === "InCare") { title = "No animals are currently in care."; description = "Try All to view historical animal records."; }
  else if (lifecycle === "OutOfCare") { title = "No animals are currently out of care."; description = "Transferred, released, sold, surrendered, inactive, and deceased animals appear here."; }
  return <div className="state-panel animal-empty-state"><h2>{title}</h2><p>{description}</p>{hasFilters ? <Button variant="secondary" type="button" onClick={onClear}>Clear filters</Button> : null}</div>;
}

function parseLifecycle(value?: string): AnimalLifecycle { return value === "all" ? "All" : value === "out-of-care" ? "OutOfCare" : "InCare"; }
function parseList(value: string | undefined, allowed: string[]) { return (value ?? "").split(",").filter((item) => allowed.includes(item)); }
function parseNumber(raw?: string) { const value = Number(raw); return Number.isInteger(value) && value > 0 ? value : undefined; }
function setUrlParameter(url: URL, name: string, value?: string | number) { if (value === undefined || value === "") url.searchParams.delete(name); else url.searchParams.set(name, String(value)); }
function deduplicateAnimals(animals: AnimalListItem[]) { return Array.from(new Map(animals.map((animal) => [animal.id, animal])).values()); }
function formatEnumLabel(value: string) { return value.replace(/([a-z])([A-Z])/g, "$1 $2"); }
function formatLifecycle(value: AnimalLifecycle) { return value === "InCare" ? "In care" : value === "OutOfCare" ? "Out of care" : "All"; }
function toSelectionOption(value: string) { return { value, label: formatEnumLabel(value) }; }
