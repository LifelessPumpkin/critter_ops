export type HusbandryTask = {
  id: number;
  title: string;
  description?: string | null;
  taskType: string;
  dueAt: string;
  recurrenceType: string;
  recurrenceInterval?: number | null;
  isCompleted: boolean;
  completedAt?: string | null;
  completionNotes?: string | null;
  completedBy?: string | null;
  animalId?: number | null;
  animalName?: string | null;
  enclosureId?: number | null;
  enclosureName?: string | null;
  isOverdue: boolean;
  isDueToday: boolean;
  createdAt: string;
  updatedAt: string;
};

export class TaskApiError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "TaskApiError";
  }
}

const apiBaseUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5198";

export async function fetchTasks(): Promise<HusbandryTask[]> {
  const response = await fetch(`${apiBaseUrl}/api/tasks`);

  if (!response.ok) {
    throw new TaskApiError(`Failed to fetch tasks: ${response.status}`);
  }

  return response.json();
}

export async function completeTask(id: number): Promise<HusbandryTask> {
  const response = await fetch(`${apiBaseUrl}/api/tasks/${id}/complete`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      completedBy: "Gilligan",
    }),
  });

  if (!response.ok) {
    throw new TaskApiError(await getTaskErrorMessage(response));
  }

  return response.json();
}

async function getTaskErrorMessage(response: Response) {
  try {
    const errorBody = await response.json();
    return errorBody.message ?? errorBody.title ?? `Task request failed: ${response.status}`;
  } catch {
    return `Task request failed: ${response.status}`;
  }
}
