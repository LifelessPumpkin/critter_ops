import { act, cleanup, renderHook, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { useTaskQueue } from "./useTaskQueue";
import { completeTask, fetchTasks, type HusbandryTask } from "@/lib/api/tasks";

vi.mock("@/lib/api/tasks", () => ({ completeTask: vi.fn(), fetchTasks: vi.fn() }));
const task: HusbandryTask = {
  id: 1, title: "Inspect habitat", taskType: "Inspection", dueAt: new Date().toISOString(),
  recurrenceType: "Daily", isCompleted: false, isOverdue: false, isDueToday: true,
  createdAt: new Date().toISOString(), updatedAt: new Date().toISOString(),
};
const completed = { ...task, isCompleted: true, completedAt: new Date().toISOString() };
const successor = { ...task, id: 2, dueAt: new Date(Date.now() + 86400000).toISOString() };

function deferred<T>() {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>((done) => { resolve = done; });
  return { promise, resolve };
}

beforeEach(() => {
  vi.resetAllMocks();
  vi.spyOn(console, "error").mockImplementation(() => {});
  vi.mocked(fetchTasks).mockResolvedValueOnce([task]);
});
afterEach(() => { cleanup(); vi.restoreAllMocks(); });

it("acknowledges completion before refresh and then includes the server's successor", async () => {
  const refresh = deferred<HusbandryTask[]>();
  vi.mocked(fetchTasks).mockReturnValueOnce(refresh.promise);
  vi.mocked(completeTask).mockResolvedValue(completed);
  const { result } = renderHook(() => useTaskQueue());
  await waitFor(() => expect(result.current.loadState).toBe("loaded"));
  await act(() => result.current.handleCompleteTask(task));
  expect(result.current.tasks).toEqual([completed]);
  expect(result.current.loadState).toBe("loaded");
  expect(fetchTasks).toHaveBeenCalledTimes(2);
  await act(async () => refresh.resolve([completed, successor]));
  expect(result.current.tasks).toEqual([completed, successor]);
});

it("preserves completion on refresh failure and retries only the collection read", async () => {
  vi.mocked(completeTask).mockResolvedValue(completed);
  vi.mocked(fetchTasks).mockRejectedValueOnce(new Error("Refresh unavailable"));
  const { result } = renderHook(() => useTaskQueue());
  await waitFor(() => expect(result.current.loadState).toBe("loaded"));
  await act(() => result.current.handleCompleteTask(task));
  expect(result.current.tasks).toEqual([completed]);
  expect(result.current.actionError).toBeNull();
  expect(result.current.refreshError).toContain("task list could not be refreshed");
  vi.mocked(fetchTasks).mockResolvedValueOnce([completed, successor]);
  await act(() => result.current.refreshTasks());
  expect(result.current.tasks).toEqual([completed, successor]);
  expect(result.current.refreshError).toBeNull();
  expect(completeTask).toHaveBeenCalledTimes(1);
});

it("leaves failed completions incomplete and allows a mutation retry", async () => {
  vi.mocked(completeTask).mockRejectedValueOnce(new Error("Completion unavailable"));
  const { result } = renderHook(() => useTaskQueue());
  await waitFor(() => expect(result.current.loadState).toBe("loaded"));
  await act(() => result.current.handleCompleteTask(task));
  expect(result.current.tasks).toEqual([task]);
  expect(result.current.actionError).toContain("Unable to complete");
  expect(result.current.completingTaskIds.size).toBe(0);
  expect(fetchTasks).toHaveBeenCalledTimes(1);
  vi.mocked(completeTask).mockResolvedValueOnce(completed);
  vi.mocked(fetchTasks).mockResolvedValueOnce([completed, successor]);
  await act(() => result.current.handleCompleteTask(task));
  expect(result.current.tasks).toEqual([completed, successor]);
});

it("allows independent completions, blocks duplicate clicks, and ignores older refresh responses", async () => {
  const other = { ...task, id: 3 };
  const otherCompleted = { ...other, isCompleted: true };
  const firstMutation = deferred<HusbandryTask>();
  const secondMutation = deferred<HusbandryTask>();
  const olderRefresh = deferred<HusbandryTask[]>();
  const newerRefresh = deferred<HusbandryTask[]>();
  vi.mocked(completeTask).mockReturnValueOnce(firstMutation.promise).mockReturnValueOnce(secondMutation.promise);
  vi.mocked(fetchTasks).mockReturnValueOnce(olderRefresh.promise).mockReturnValueOnce(newerRefresh.promise);
  const { result } = renderHook(() => useTaskQueue());
  await waitFor(() => expect(result.current.loadState).toBe("loaded"));
  let first: Promise<void>;
  let second: Promise<void>;
  act(() => {
    first = result.current.handleCompleteTask(task);
    void result.current.handleCompleteTask(task);
    second = result.current.handleCompleteTask(other);
  });
  expect(completeTask).toHaveBeenCalledTimes(2);
  expect([...result.current.completingTaskIds]).toEqual([1, 3]);
  await act(async () => { firstMutation.resolve(completed); await first; });
  expect([...result.current.completingTaskIds]).toEqual([3]);
  await act(async () => { secondMutation.resolve(otherCompleted); await second; });
  const authoritative = [completed, successor, otherCompleted];
  await act(async () => newerRefresh.resolve(authoritative));
  await act(async () => olderRefresh.resolve([completed, other]));
  expect(result.current.tasks).toEqual(authoritative);
});
