import { afterEach, expect, it, vi } from "vitest";
import { readPreferences, readSavedViews, writeSavedViews } from "./timelineStorage";
import { defaultTimelineFilters, type TimelineSavedView } from "./timelineTypes";

const key = "critterops.activityTimeline.savedViews.v1";
const saved: TimelineSavedView = { id: "view", name: "Feedings", filters: { ...defaultTimelineFilters, eventTypes: ["Feeding"] }, sort: "Oldest", groupBy: "animal", view: "week", columns: ["time", "animal"], search: "keeper", pinned: true };
afterEach(() => { vi.restoreAllMocks(); localStorage.clear(); });

it("round trips saved views, updates and deletion using existing storage keys", () => {
  expect(readSavedViews()).toEqual([]);
  expect(writeSavedViews([saved])).toBe(true);
  expect(readSavedViews()).toEqual([saved]);
  const renamed = { ...saved, name: "Morning", pinned: false };
  writeSavedViews([renamed]);
  expect(readSavedViews()).toEqual([renamed]);
  writeSavedViews([]);
  expect(readSavedViews()).toEqual([]);
  localStorage.removeItem(key);
  localStorage.setItem("critterops.activityTimeline.filterPresets.v1", JSON.stringify([saved]));
  expect(readSavedViews()).toEqual([saved]);
});

it("recovers valid entries and fields without rewriting malformed stored data", () => {
  const raw = JSON.stringify([null, {}, { ...saved, filters: { performer: "Keeper", eventTypes: "obsolete", from: "2026-99-99" }, columns: "obsolete" }, saved]);
  localStorage.setItem(key, raw);
  expect(readSavedViews()).toEqual([
    { ...saved, filters: { ...defaultTimelineFilters, performer: "Keeper" }, columns: undefined }, saved,
  ]);
  expect(localStorage.getItem(key)).toBe(raw);
  for (const value of ["null", "{}", "42"]) {
    localStorage.setItem(key, value);
    expect(readSavedViews()).toEqual([]);
  }
  localStorage.setItem(key, "{");
  expect(readSavedViews()).toBeNull();
  expect(localStorage.getItem(key)).toBe("{");
  localStorage.setItem("critterops.activityTimeline.preferences.v1", JSON.stringify({ columns: { global: "bad" }, view: "week" }));
  expect(readPreferences().view).toBe("week");
});

it("handles blocked storage reads and writes without throwing", () => {
  vi.spyOn(Storage.prototype, "getItem").mockImplementation(() => { throw new Error("Blocked"); });
  vi.spyOn(Storage.prototype, "setItem").mockImplementation(() => { throw new Error("Full"); });
  expect(readSavedViews()).toBeNull();
  expect(readPreferences()).toEqual({});
  expect(writeSavedViews([saved])).toBe(false);
});
