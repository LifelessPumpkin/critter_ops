"use client";

import { useEffect, useMemo, useState } from "react";
import { Input } from "@/components/ui";
import { TaskQueueSkeleton } from "@/components/tasks/TaskQueueSkeleton";
import { TaskRow } from "@/components/tasks/TaskRow";
import { TaskSection } from "@/components/tasks/TaskSection";
import { type TaskTab, TaskTabs } from "@/components/tasks/TaskTabs";
import { type HusbandryTask, completeTask, fetchTasks } from "@/lib/api/tasks";

type LoadState = "loading" | "loaded" | "error";

type TaskGroups = {
  completed: HusbandryTask[];
  overdue: HusbandryTask[];
  today: HusbandryTask[];
};

const todayInputValue = formatDateInputValue(new Date());

export function TaskQueue() {
  const [activeTab, setActiveTab] = useState<TaskTab>("today");
  const [tasks, setTasks] = useState<HusbandryTask[]>([]);
  const [loadState, setLoadState] = useState<LoadState>("loading");
  const [completingTaskId, setCompletingTaskId] = useState<number | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [customFrom, setCustomFrom] = useState(todayInputValue);
  const [customTo, setCustomTo] = useState(todayInputValue);

  useEffect(() => {
    let isMounted = true;

    async function loadTasks() {
      try {
        const taskData = await fetchTasks();

        if (isMounted) {
          setTasks(taskData);
          setLoadState("loaded");
        }
      } catch (error) {
        console.error(error);

        if (isMounted) {
          setLoadState("error");
        }
      }
    }

    loadTasks();

    return () => {
      isMounted = false;
    };
  }, []);

  const todayGroups = useMemo(() => getTodayGroups(tasks), [tasks]);
  const weekGroups = useMemo(() => getWeekGroups(tasks), [tasks]);
  const customTasks = useMemo(() => getCustomTasks(tasks, customFrom, customTo), [customFrom, customTo, tasks]);

  async function handleCompleteTask(task: HusbandryTask) {
    setCompletingTaskId(task.id);
    setActionError(null);

    try {
      const completedTask = await completeTask(task.id);
      setTasks((currentTasks) =>
        currentTasks.map((currentTask) => (currentTask.id === completedTask.id ? completedTask : currentTask)),
      );
    } catch (error) {
      console.error(error);
      setActionError("Unable to complete task. Please try again.");
    } finally {
      setCompletingTaskId(null);
    }
  }

  if (loadState === "loading") {
    return <TaskQueueSkeleton />;
  }

  if (loadState === "error") {
    return (
      <div className="state-panel" role="alert">
        <h2>Unable to load tasks.</h2>
        <p>Please try again.</p>
      </div>
    );
  }

  return (
    <div className="task-queue animated-fade-in">
      <TaskTabs activeTab={activeTab} onSelectTab={setActiveTab} />

      {actionError ? (
        <div className="form-error-panel" role="alert">
          <p>{actionError}</p>
        </div>
      ) : null}

      {activeTab === "today" ? (
        <TodayQueue groups={todayGroups} completingTaskId={completingTaskId} onCompleteTask={handleCompleteTask} />
      ) : null}

      {activeTab === "week" ? (
        <WeekQueue groups={weekGroups} completingTaskId={completingTaskId} onCompleteTask={handleCompleteTask} />
      ) : null}

      {activeTab === "custom" ? (
        <CustomQueue
          completingTaskId={completingTaskId}
          customFrom={customFrom}
          customTasks={customTasks}
          customTo={customTo}
          onChangeFrom={setCustomFrom}
          onChangeTo={setCustomTo}
          onCompleteTask={handleCompleteTask}
        />
      ) : null}
    </div>
  );
}

function TodayQueue({
  completingTaskId,
  groups,
  onCompleteTask,
}: {
  completingTaskId: number | null;
  groups: TaskGroups;
  onCompleteTask: (task: HusbandryTask) => void;
}) {
  const isCaughtUp = groups.overdue.length === 0 && groups.today.length === 0 && groups.completed.length === 0;

  if (isCaughtUp) {
    return (
      <div className="state-panel">
        <h2>You&apos;re all caught up.</h2>
        <p>No tasks are due today.</p>
      </div>
    );
  }

  return (
    <>
      <TaskSection title="Overdue" count={groups.overdue.length}>
        {groups.overdue.length > 0 ? (
          groups.overdue.map((task) => (
            <TaskRow key={task.id} task={task} isCompleting={completingTaskId === task.id} onComplete={onCompleteTask} />
          ))
        ) : (
          <p className="task-empty-message">No overdue tasks.</p>
        )}
      </TaskSection>

      <TaskSection title="Today" count={groups.today.length}>
        {groups.today.length > 0 ? (
          groups.today.map((task) => (
            <TaskRow key={task.id} task={task} isCompleting={completingTaskId === task.id} onComplete={onCompleteTask} />
          ))
        ) : (
          <p className="task-empty-message">No remaining tasks due today.</p>
        )}
      </TaskSection>

      <TaskSection title="Completed" count={groups.completed.length}>
        {groups.completed.length > 0 ? (
          groups.completed.map((task) => (
            <TaskRow key={task.id} task={task} isCompleting={false} onComplete={onCompleteTask} />
          ))
        ) : (
          <p className="task-empty-message">No completed tasks yet today.</p>
        )}
      </TaskSection>
    </>
  );
}

function WeekQueue({
  completingTaskId,
  groups,
  onCompleteTask,
}: {
  completingTaskId: number | null;
  groups: Array<{ date: Date; label: string; tasks: HusbandryTask[] }>;
  onCompleteTask: (task: HusbandryTask) => void;
}) {
  const hasTasks = groups.some((group) => group.tasks.length > 0);

  if (!hasTasks) {
    return (
      <div className="state-panel">
        <h2>No tasks are scheduled for this week.</h2>
      </div>
    );
  }

  return (
    <>
      {groups.map((group) =>
        group.tasks.length > 0 ? (
          <TaskSection key={group.label} title={group.label} count={group.tasks.length}>
            {group.tasks.map((task) => (
              <TaskRow key={task.id} task={task} isCompleting={completingTaskId === task.id} onComplete={onCompleteTask} />
            ))}
          </TaskSection>
        ) : null,
      )}
    </>
  );
}

function CustomQueue({
  completingTaskId,
  customFrom,
  customTasks,
  customTo,
  onChangeFrom,
  onChangeTo,
  onCompleteTask,
}: {
  completingTaskId: number | null;
  customFrom: string;
  customTasks: HusbandryTask[];
  customTo: string;
  onChangeFrom: (value: string) => void;
  onChangeTo: (value: string) => void;
  onCompleteTask: (task: HusbandryTask) => void;
}) {
  return (
    <>
      <div className="custom-range-controls">
        <Input label="From" type="date" value={customFrom} onChange={(event) => onChangeFrom(event.target.value)} />
        <Input label="To" type="date" value={customTo} onChange={(event) => onChangeTo(event.target.value)} />
      </div>

      {customTasks.length > 0 ? (
        <TaskSection title="Custom Range" count={customTasks.length}>
          {customTasks.map((task) => (
            <TaskRow key={task.id} task={task} isCompleting={completingTaskId === task.id} onComplete={onCompleteTask} />
          ))}
        </TaskSection>
      ) : (
        <div className="state-panel">
          <h2>No tasks found in this date range.</h2>
        </div>
      )}
    </>
  );
}

function getTodayGroups(tasks: HusbandryTask[]): TaskGroups {
  const today = new Date();

  return {
    overdue: sortByDueAt(tasks.filter((task) => !task.isCompleted && isBeforeLocalDay(new Date(task.dueAt), today))),
    today: sortByDueAt(tasks.filter((task) => !task.isCompleted && isSameLocalDay(new Date(task.dueAt), today))),
    completed: sortByCompletedAt(tasks.filter((task) => task.isCompleted && isSameLocalDay(new Date(task.completedAt ?? task.dueAt), today))),
  };
}

function getWeekGroups(tasks: HusbandryTask[]) {
  const weekStart = startOfWeek(new Date());

  return Array.from({ length: 7 }, (_, index) => {
    const date = new Date(weekStart);
    date.setDate(weekStart.getDate() + index);

    return {
      date,
      label: new Intl.DateTimeFormat("en", { weekday: "long", month: "short", day: "numeric" }).format(date),
      tasks: sortByDueAt(tasks.filter((task) => isSameLocalDay(new Date(task.dueAt), date))),
    };
  });
}

function getCustomTasks(tasks: HusbandryTask[], from: string, to: string) {
  const fromDate = parseDateInput(from);
  const toDate = parseDateInput(to);

  if (!fromDate || !toDate) {
    return [];
  }

  toDate.setHours(23, 59, 59, 999);

  return sortByDueAt(tasks.filter((task) => {
    const dueAt = new Date(task.dueAt);
    return dueAt >= fromDate && dueAt <= toDate;
  }));
}

function sortByDueAt(tasks: HusbandryTask[]) {
  return [...tasks].sort((firstTask, secondTask) => new Date(firstTask.dueAt).getTime() - new Date(secondTask.dueAt).getTime());
}

function sortByCompletedAt(tasks: HusbandryTask[]) {
  return [...tasks].sort(
    (firstTask, secondTask) =>
      new Date(secondTask.completedAt ?? secondTask.updatedAt).getTime() -
      new Date(firstTask.completedAt ?? firstTask.updatedAt).getTime(),
  );
}

function startOfWeek(date: Date) {
  const weekStart = new Date(date);
  const day = weekStart.getDay();
  const distanceToMonday = day === 0 ? -6 : 1 - day;
  weekStart.setDate(weekStart.getDate() + distanceToMonday);
  weekStart.setHours(0, 0, 0, 0);
  return weekStart;
}

function isBeforeLocalDay(firstDate: Date, secondDate: Date) {
  const firstStart = new Date(firstDate);
  firstStart.setHours(0, 0, 0, 0);
  const secondStart = new Date(secondDate);
  secondStart.setHours(0, 0, 0, 0);
  return firstStart < secondStart;
}

function isSameLocalDay(firstDate: Date, secondDate: Date) {
  return firstDate.toDateString() === secondDate.toDateString();
}

function formatDateInputValue(date: Date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function parseDateInput(value: string) {
  if (!value) {
    return null;
  }

  return new Date(`${value}T00:00:00`);
}
