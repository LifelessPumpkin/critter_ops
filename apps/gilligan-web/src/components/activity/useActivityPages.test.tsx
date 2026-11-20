import { act, cleanup, renderHook, waitFor } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { fetchActivities, type ActivityRecord, type ActivitySearchResponse } from "@/lib/api/activity";
import { useActivityPages } from "./useActivityPages";

vi.mock("@/lib/api/activity", async importOriginal => ({ ...await importOriginal<typeof import("@/lib/api/activity")>(), fetchActivities: vi.fn() }));
afterEach(() => { cleanup(); vi.resetAllMocks(); vi.restoreAllMocks(); });
const query = { search: "feeding", pageSize: 50 };
function page(number: number, ids: number[]): ActivitySearchResponse {
  return { page: number, pageSize: 50, totalCount: 100, totalPages: 2, items: ids.map(id => ({ id, type: "Feeding", title: `Activity ${id}`, occurredAt: "2026-01-01", createdAt: "2026-01-01", description: "Feeding", animals: [], enclosures: [], details: {} } satisfies ActivityRecord)) };
}
function deferred() {
  let resolve!: (value: ActivitySearchResponse) => void;
  let reject!: (reason: Error) => void;
  const promise = new Promise<ActivitySearchResponse>((yes, no) => { resolve = yes; reject = no; });
  return { promise, resolve, reject };
}

it("ignores stale first-page successes and errors and retries current failures", async () => {
  vi.spyOn(console, "error").mockImplementation(() => {});
  const old = deferred();
  const staleError = deferred();
  vi.mocked(fetchActivities).mockReturnValueOnce(old.promise).mockReturnValueOnce(staleError.promise).mockResolvedValueOnce(page(1, [3]));
  const { result, rerender } = renderHook(({ query }) => useActivityPages(query), { initialProps: { query } });
  const signal = vi.mocked(fetchActivities).mock.calls[0][1];
  rerender({ query: { ...query, search: "medical" } });
  rerender({ query: { ...query, search: "current" } });
  await waitFor(() => expect(result.current.loadState).toBe("loaded"));
  await act(async () => { old.resolve(page(1, [1])); staleError.reject(new Error("Old error")); });
  expect(signal?.aborted).toBe(true);
  expect(result.current.activityPage.items.map(item => item.id)).toEqual([3]);
  expect(result.current.loadState).toBe("loaded");
  vi.mocked(fetchActivities).mockRejectedValueOnce(new Error("Offline")).mockResolvedValueOnce(page(1, [4]));
  act(() => result.current.retry());
  await waitFor(() => expect(result.current.loadState).toBe("error"));
  act(() => result.current.retry());
  await waitFor(() => expect(result.current.activityPage.items[0].id).toBe(4));
});

it("appends unique activities, blocks duplicate scrolling requests and retries without losing page one", async () => {
  vi.spyOn(console, "error").mockImplementation(() => {});
  const next = deferred();
  vi.mocked(fetchActivities).mockResolvedValueOnce(page(1, [1])).mockReturnValueOnce(next.promise).mockResolvedValueOnce(page(2, [1, 2]));
  const { result } = renderHook(() => useActivityPages(query));
  await waitFor(() => expect(result.current.loadState).toBe("loaded"));
  act(() => { result.current.loadNextPage(); result.current.loadNextPage(); });
  expect(fetchActivities).toHaveBeenCalledTimes(2);
  await act(async () => next.reject(new Error("Offline")));
  expect(result.current.additionalLoadState).toBe("error");
  expect(result.current.activityPage.items.map(item => item.id)).toEqual([1]);
  act(() => result.current.loadNextPage());
  await waitFor(() => expect(result.current.hasMore).toBe(false));
  expect(result.current.activityPage.items.map(item => item.id)).toEqual([1, 2]);
  expect(fetchActivities).toHaveBeenLastCalledWith({ ...query, page: 2 }, expect.any(AbortSignal));
});

it("does not let an old page completion clear a new page request, even when queries repeat", async () => {
  const old = deferred();
  const current = deferred();
  vi.mocked(fetchActivities).mockResolvedValueOnce(page(1, [1])).mockReturnValueOnce(old.promise)
    .mockResolvedValueOnce(page(1, [2])).mockResolvedValueOnce(page(1, [3])).mockReturnValueOnce(current.promise);
  const { result, rerender, unmount } = renderHook(({ query }) => useActivityPages(query), { initialProps: { query } });
  await waitFor(() => expect(result.current.loadState).toBe("loaded"));
  act(() => result.current.loadNextPage());
  const oldSignal = vi.mocked(fetchActivities).mock.calls[1][1];
  rerender({ query: { ...query, search: "other" } });
  await waitFor(() => expect(result.current.activityPage.items[0].id).toBe(2));
  rerender({ query });
  await waitFor(() => expect(result.current.activityPage.items[0].id).toBe(3));
  act(() => result.current.loadNextPage());
  await act(async () => old.resolve(page(2, [99])));
  expect(oldSignal?.aborted).toBe(true);
  expect(result.current.additionalLoadState).toBe("loading");
  expect(result.current.activityPage.items.map(item => item.id)).toEqual([3]);
  act(() => result.current.loadNextPage());
  expect(fetchActivities).toHaveBeenCalledTimes(5);
  await act(async () => current.resolve(page(2, [4])));
  expect(result.current.activityPage.items.map(item => item.id)).toEqual([3, 4]);
  unmount();
  expect(vi.mocked(fetchActivities).mock.calls[4][1]?.aborted).toBe(true);
});
