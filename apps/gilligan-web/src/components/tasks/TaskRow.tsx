import { Badge, Button, getStatusBadgeVariant } from "@/components/ui";
import type { HusbandryTask } from "@/lib/api/tasks";

type TaskRowProps = {
  isCompleting: boolean;
  onComplete: (task: HusbandryTask) => void;
  task: HusbandryTask;
};

export function TaskRow({ isCompleting, onComplete, task }: TaskRowProps) {
  const isOverdue = !task.isCompleted && isTaskOverdue(task);

  return (
    <article className={getTaskRowClassName(task, isOverdue)}>
      <div className="task-row-main">
        <div className="task-row-title-line">
          <h3>{task.isCompleted ? `✓ ${task.title}` : task.title}</h3>
          <Badge variant={task.isCompleted ? "neutral" : isOverdue ? "warning" : getStatusBadgeVariant("Active")}>
            {task.isCompleted ? "Completed" : isOverdue ? "Overdue" : formatEnumLabel(task.taskType)}
          </Badge>
        </div>
        <p className="task-row-meta">{getTaskTimingText(task, isOverdue)}</p>
        <div className="task-row-details">
          <span>{formatEnumLabel(task.taskType)}</span>
          {task.animalName ? <span>Animal: {task.animalName}</span> : null}
          {task.enclosureName ? <span>Enclosure: {task.enclosureName}</span> : null}
          {task.recurrenceType !== "None" ? <span>{formatEnumLabel(task.recurrenceType)}</span> : null}
        </div>
        {task.description ? <p className="task-row-description">{task.description}</p> : null}
      </div>

      {!task.isCompleted ? (
        <Button
          className="task-complete-button"
          disabled={isCompleting}
          onClick={() => onComplete(task)}
          type="button"
          variant="secondary"
        >
          {isCompleting ? "Completing..." : "Complete"}
        </Button>
      ) : null}
    </article>
  );
}

function getTaskRowClassName(task: HusbandryTask, isOverdue: boolean) {
  return [
    "task-row",
    task.isCompleted ? "task-row-completed" : null,
    isOverdue ? "task-row-overdue" : null,
  ]
    .filter(Boolean)
    .join(" ");
}

function getTaskTimingText(task: HusbandryTask, isOverdue: boolean) {
  if (task.isCompleted) {
    return `Completed ${formatDateTime(task.completedAt)}${task.completedBy ? ` by ${task.completedBy}` : ""}`;
  }

  if (isOverdue) {
    return `Due ${formatRelativeDueDate(task.dueAt)}`;
  }

  if (isSameLocalDay(new Date(task.dueAt), new Date())) {
    return `Due ${formatTime(task.dueAt)}`;
  }

  return `Due ${formatDateTime(task.dueAt)}`;
}

function isTaskOverdue(task: HusbandryTask) {
  return new Date(task.dueAt).getTime() < startOfToday().getTime();
}

function formatRelativeDueDate(date: string) {
  const dueDate = new Date(date);
  const today = startOfToday();
  const yesterday = new Date(today);
  yesterday.setDate(today.getDate() - 1);

  if (isSameLocalDay(dueDate, yesterday)) {
    return `yesterday at ${formatTime(date)}`;
  }

  return formatDateTime(date);
}

function formatDateTime(date?: string | null) {
  if (!date) {
    return "unknown";
  }

  return new Intl.DateTimeFormat("en", {
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
  }).format(new Date(date));
}

function formatTime(date: string) {
  return new Intl.DateTimeFormat("en", {
    hour: "numeric",
    minute: "2-digit",
  }).format(new Date(date));
}

function formatEnumLabel(value: string) {
  return value.replace(/([a-z])([A-Z])/g, "$1 $2");
}

function isSameLocalDay(firstDate: Date, secondDate: Date) {
  return firstDate.toDateString() === secondDate.toDateString();
}

function startOfToday() {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  return today;
}
