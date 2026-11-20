import { afterEach, expect, it, vi } from "vitest";
import { fetchActivities } from "./activity";
afterEach(() => vi.unstubAllGlobals());
it("encodes bounded filter requests, forwards cancellation, transforms responses and reports failures", async () => {
  const fetch = vi.fn().mockResolvedValue(new Response(JSON.stringify({ page: 2, pageSize: 50, totalCount: 51, totalPages: 2, items: [{ id: 7, eventType: "Task", title: "Check water", notes: "Morning", sourceType: "Task", performedBy: "Keeper", occurredAt: "2026-01-01", createdAt: "2026-01-01", animals: [{ animalId: 3, animalName: "Mango" }], enclosures: [], details: {} }] })));
  vi.stubGlobal("fetch", fetch);
  const signal = new AbortController().signal;
  const result = await fetchActivities({ search: "water & food", eventTypes: ["Task", "Feeding"], animalId: 3, enclosureId: 4, performedBy: "Keeper", from: "2026-01-01", to: "2026-01-02", sort: "Oldest", page: 2 }, signal);
  const url = new URL(fetch.mock.calls[0][0], "http://localhost");
  expect(Object.fromEntries(url.searchParams)).toEqual({ Search: "water & food", EventTypes: "Feeding", AnimalId: "3", EnclosureId: "4", PerformedBy: "Keeper", From: "2026-01-01", To: "2026-01-02", Sort: "Oldest", Page: "2", PageSize: "50" });
  expect(url.searchParams.getAll("EventTypes")).toEqual(["Task", "Feeding"]);
  expect(fetch.mock.calls[0][1]).toEqual({ signal });
  expect(result.items[0]).toMatchObject({ id: 7, type: "Task", description: "Check water — Morning", performer: "Keeper", animals: [{ id: 3, name: "Mango" }] });
  fetch.mockResolvedValue(new Response(null, { status: 503 }));
  await expect(fetchActivities({})).rejects.toThrow("503");
});
