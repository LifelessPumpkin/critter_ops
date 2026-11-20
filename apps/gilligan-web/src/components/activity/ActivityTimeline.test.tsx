import { act, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { ActivityTimeline } from "./ActivityTimeline";
import { parseTimelineInitialState } from "./timelineUrlState";
import { fetchActivities, type ActivitySearchQuery } from "@/lib/api/activity";

vi.mock("@/lib/api/activity", async importOriginal => ({ ...await importOriginal<typeof import("@/lib/api/activity")>(), fetchActivities: vi.fn() }));
vi.mock("@/lib/api/animals", () => ({ fetchAnimals: vi.fn().mockResolvedValue([]) }));
vi.mock("@/lib/api/enclosures", () => ({ fetchEnclosures: vi.fn().mockResolvedValue([]) }));
let intersect: () => void;
beforeEach(() => {
  localStorage.clear();
  window.history.replaceState(null, "", "/activity");
  vi.stubGlobal("IntersectionObserver", class {
    constructor(callback: (entries: { isIntersecting: boolean }[]) => void) { intersect = () => callback([{ isIntersecting: true }]); }
    observe() {}
    disconnect() {}
  });
  vi.mocked(fetchActivities).mockImplementation(async (query: ActivitySearchQuery) => ({
    page: query.page ?? 1, pageSize: 50, totalPages: 2, totalCount: 2,
    items: [{ id: query.page ?? 1, type: "Feeding", title: "Feeding", occurredAt: "2026-01-01T12:00:00Z", createdAt: "2026-01-01T12:00:00Z", description: "Feeding", animals: [], enclosures: [], details: {} }],
  }));
});
afterEach(() => { cleanup(); vi.clearAllMocks(); vi.unstubAllGlobals(); });

it("restores URL filters, synchronizes edits and restores browser navigation safely", async () => {
  const params = { type: "Feeding,invalid", animal: "7", sort: "oldest", from: "2026-99-99", date: "2026-02-30", view: "invalid" };
  window.history.replaceState(null, "", `/activity?${new URLSearchParams(params)}`);
  render(<ActivityTimeline context={{ kind: "global" }} initialState={parseTimelineInitialState(params)} />);
  await screen.findByRole("checkbox", { name: "Select Feeding activity 1" });
  expect(fetchActivities).toHaveBeenLastCalledWith(expect.objectContaining({ eventTypes: ["Feeding"], animalId: 7, sort: "Oldest", from: undefined, page: 1, pageSize: 50 }), expect.any(AbortSignal));
  fireEvent.click(screen.getByRole("button", { name: "Treatments" }));
  await waitFor(() => expect(new URLSearchParams(location.search).get("type")).toBe("Feeding,Treatment"));
  act(() => {
    window.history.pushState(null, "", "/activity?type=Medication&performer=Keeper&sort=newest");
    window.dispatchEvent(new PopStateEvent("popstate"));
  });
  await waitFor(() => expect(fetchActivities).toHaveBeenLastCalledWith(expect.objectContaining({ eventTypes: ["Medication"], performedBy: "Keeper", animalId: undefined, sort: "Newest", page: 1 }), expect.any(AbortSignal)));
});

it("preserves selection across appended pages, clears it on filters, and reapplies saved views after remount", async () => {
  const first = render(<ActivityTimeline context={{ kind: "global" }} />);
  fireEvent.click(await screen.findByRole("checkbox", { name: "Select Feeding activity 1" }));
  act(() => intersect());
  const second = await screen.findByRole("checkbox", { name: "Select Feeding activity 2" });
  expect(screen.getByRole("checkbox", { name: "Select Feeding activity 1" })).toBeChecked();
  fireEvent.click(second, { ctrlKey: true });
  expect(screen.getByRole("toolbar", { name: "2 selected activities" })).toBeInTheDocument();
  fireEvent.click(second, { ctrlKey: true });
  expect(screen.getByRole("toolbar", { name: "1 selected activities" })).toBeInTheDocument();
  fireEvent.click(screen.getByRole("checkbox", { name: "Select all activities on this page" }));
  expect(screen.getByRole("toolbar", { name: "2 selected activities" })).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Feedings" }));
  expect(screen.queryByRole("toolbar", { name: /selected activities/ })).not.toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "+ Save View" }));
  fireEvent.change(screen.getByLabelText("View name"), { target: { value: "My feedings" } });
  fireEvent.click(screen.getByRole("button", { name: "Save view" }));
  first.unmount();
  render(<ActivityTimeline context={{ kind: "global" }} />);
  fireEvent.click(await screen.findByRole("button", { name: "My feedings" }));
  await waitFor(() => expect(fetchActivities).toHaveBeenLastCalledWith(expect.objectContaining({ eventTypes: ["Feeding"], page: 1 }), expect.any(AbortSignal)));
  expect(new URLSearchParams(location.search).get("type")).toBe("Feeding");
});
