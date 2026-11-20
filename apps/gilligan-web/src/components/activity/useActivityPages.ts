"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { activityPageSize, fetchActivities, type ActivityRecord, type ActivitySearchQuery } from "@/lib/api/activity";

type LoadState = "loading" | "loaded" | "error";
type RequestState = { key: string; status: LoadState };
export type AdditionalLoadState = "idle" | "loading" | "error";

const defaultActivityPage = { page: 0, pageSize: activityPageSize, totalCount: 0, totalPages: 0, items: [] as ActivityRecord[] };
export function useActivityPages(query: ActivitySearchQuery) {
  const [activityPage, setActivityPage] = useState(defaultActivityPage);
  const [performers, setPerformers] = useState<string[]>([]);
  const [requestState, setRequestState] = useState<RequestState>({ key: "", status: "loading" });
  const [additionalLoadState, setAdditionalLoadState] = useState<AdditionalLoadState>("idle");
  const [reloadKey, setReloadKey] = useState(0);
  const loadMoreAbortRef = useRef<AbortController | null>(null);
  const inFlightPageRef = useRef<number | null>(null);
  const loadedPagesRef = useRef(new Set<number>());
  const activeRequestKeyRef = useRef("");
  const requestKey = useMemo(() => `${JSON.stringify(query)}:${reloadKey}`, [query, reloadKey]);
  const loadState: LoadState = requestState.key === requestKey ? requestState.status : "loading";

  const hasMore = activityPage.page > 0 && activityPage.page < activityPage.totalPages;
  useEffect(() => {
    const abortController = new AbortController();
    let isActive = true;
    loadMoreAbortRef.current?.abort();
    loadMoreAbortRef.current = null;
    inFlightPageRef.current = null;
    loadedPagesRef.current = new Set();
    activeRequestKeyRef.current = requestKey;

    fetchActivities({ ...query, page: 1 }, abortController.signal)
      .then((response) => {
        if (!isActive || activeRequestKeyRef.current !== requestKey) return;
        loadedPagesRef.current.add(1);
        setPerformers((current) => mergePerformers(
          current,
          response.items.map((activity) => activity.performer?.trim()).filter((performer): performer is string => Boolean(performer)),
        ));
        setActivityPage({ ...response, items: deduplicateActivities(response.items) });
        setAdditionalLoadState("idle");
        setRequestState({ key: requestKey, status: "loaded" });
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }
        if (!isActive) return;
        console.error(error);
        setRequestState({ key: requestKey, status: "error" });
      });

    return () => {
      isActive = false;
      abortController.abort();
      loadMoreAbortRef.current?.abort();
    };
  }, [query, requestKey]);

  const loadNextPage = useCallback(() => {
    if (loadState !== "loaded" || !hasMore || additionalLoadState === "loading") return;
    const nextPage = activityPage.page + 1;
    if (loadedPagesRef.current.has(nextPage) || inFlightPageRef.current === nextPage) return;

    const abortController = new AbortController();
    loadMoreAbortRef.current?.abort();
    loadMoreAbortRef.current = abortController;
    inFlightPageRef.current = nextPage;
    setAdditionalLoadState("loading");

    fetchActivities({ ...query, page: nextPage }, abortController.signal)
      .then((response) => {
        if (abortController.signal.aborted || loadMoreAbortRef.current !== abortController) return;
        loadedPagesRef.current.add(nextPage);
        setPerformers((current) => mergePerformers(
          current,
          response.items.map((activity) => activity.performer?.trim()).filter((performer): performer is string => Boolean(performer)),
        ));
        setActivityPage((current) => ({
          ...response,
          page: Math.max(current.page, response.page),
          items: mergeActivityPages(current.items, response.items),
        }));
        setAdditionalLoadState("idle");
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") return;
        if (abortController.signal.aborted || loadMoreAbortRef.current !== abortController) return;
        console.error(error);
        setAdditionalLoadState("error");
      })
      .finally(() => {
        if (loadMoreAbortRef.current === abortController) inFlightPageRef.current = null;
      });
  }, [activityPage.page, additionalLoadState, hasMore, loadState, query]);

  return { activityPage, performers, loadState, additionalLoadState, hasMore, loadNextPage, retry: () => setReloadKey(value => value + 1) };
}

function mergePerformers(current: string[], incoming: string[]) {
  const performers = new Map<string, string>();
  [...current, ...incoming].forEach((performer) => {
    const key = performer.toLocaleLowerCase();
    if (!performers.has(key)) performers.set(key, performer);
  });
  return Array.from(performers.values()).sort((first, second) => first.localeCompare(second, undefined, { sensitivity: "base" }));
}

function deduplicateActivities(activities: ActivityRecord[]) {
  return mergeActivityPages([], activities);
}

function mergeActivityPages(current: ActivityRecord[], incoming: ActivityRecord[]) {
  const activities = new Map(current.map((activity) => [activity.id, activity]));
  incoming.forEach((activity) => activities.set(activity.id, activity));
  return Array.from(activities.values());
}

