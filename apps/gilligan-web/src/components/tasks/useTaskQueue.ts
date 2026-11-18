"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { completeTask, fetchTasks, type HusbandryTask } from "@/lib/api/tasks";

export function useTaskQueue() {
  const [tasks, setTasks] = useState<HusbandryTask[]>([]);
  const [loadState, setLoadState] = useState<"loading" | "loaded" | "error">("loading");
  const [completingTaskIds, setCompletingTaskIds] = useState<ReadonlySet<number>>(new Set());
  const [actionError, setActionError] = useState<string | null>(null);
  const [refreshError, setRefreshError] = useState<string | null>(null);
  const pendingIds = useRef(new Set<number>());
  const requestVersion = useRef(0);
  const mounted = useRef(false);

  const refreshTasks = useCallback((initial = false) => {
    const version = ++requestVersion.current;
    return fetchTasks().then((refreshedTasks) => {
      if (!mounted.current || version !== requestVersion.current) return;
      setTasks(refreshedTasks);
      setLoadState("loaded");
      setRefreshError(null);
    }).catch((error) => {
      console.error(error);
      if (!mounted.current || version !== requestVersion.current) return;
      if (initial) {
        setLoadState("error");
      } else {
        setRefreshError("Changes saved, but the task list could not be refreshed.");
      }
    });
  }, []);

  useEffect(() => {
    mounted.current = true;
    void refreshTasks(true);
    return () => {
      mounted.current = false;
    };
  }, [refreshTasks]);

  async function handleCompleteTask(task: HusbandryTask) {
    if (task.isCompleted || pendingIds.current.has(task.id)) return;
    pendingIds.current.add(task.id);
    setCompletingTaskIds(new Set(pendingIds.current));
    setActionError(null);

    try {
      const completedTask = await completeTask(task.id);
      if (!mounted.current) return;
      setTasks((current) => current.map((item) => item.id === completedTask.id ? completedTask : item));
      // Starting this refresh invalidates any read begun before the mutation committed.
      void refreshTasks();
    } catch (error) {
      console.error(error);
      if (mounted.current) setActionError("Unable to complete task. Please try again.");
    } finally {
      pendingIds.current.delete(task.id);
      if (mounted.current) setCompletingTaskIds(new Set(pendingIds.current));
    }
  }

  function handleTaskCreated(task: HusbandryTask) {
    setTasks((current) => [task, ...current]);
    setActionError(null);
    void refreshTasks();
  }

  return { tasks, loadState, completingTaskIds, actionError, refreshError, refreshTasks, handleCompleteTask, handleTaskCreated };
}
