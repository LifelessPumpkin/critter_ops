"use client";

import { useEffect, useMemo, useState } from "react";
import type { CatalogPage } from "@/lib/api/animalCatalog";

// One page retained at a time. Invalidate immediately, debounce only the request.
export function useCatalogSearch<T>(key: string, search: string, enabled: boolean,
  fetchPage: (page: number, signal: AbortSignal) => Promise<CatalogPage<T>>, message: string) {
  const [cursor, setCursor] = useState({ key, page: 1 });
  const page = cursor.key === key ? cursor.page : 1;
  const [attempt, setAttempt] = useState(0);
  const request = useMemo(() => ({ key, page, enabled, fetchPage, attempt, search, message }),
    [key, page, enabled, fetchPage, attempt, search, message]);
  const [result, setResult] = useState<{ request: typeof request; items: T[]; hasMore: boolean; error: string }>();
  const current = result?.request === request;
  useEffect(() => {
    if (!request.enabled) return;
    let active = true;
    const controller = new AbortController();
    const timer = setTimeout(() => {
      request.fetchPage(request.page, controller.signal).then(data => {
        if (active) setResult({ request, items: data.items, hasMore: data.hasMore, error: "" });
      }).catch(() => {
        if (active && !controller.signal.aborted) setResult({ request, items: [], hasMore: false, error: request.message });
      });
    }, request.search ? 300 : 0);
    return () => { active = false; clearTimeout(timer); controller.abort(); };
  }, [request]);
  return {
    items: current ? result.items : [], error: current ? result.error : "",
    loading: enabled && !current, hasMore: current && result.hasMore, page,
    previous: () => setCursor({ key, page: Math.max(1, page - 1) }),
    next: () => setCursor({ key, page: page + 1 }), retry: () => setAttempt(n => n + 1),
  };
}
