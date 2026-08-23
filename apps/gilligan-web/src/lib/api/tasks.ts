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

export type CreateTaskRequest = {
  title: string;
  description?: string | null;
  taskType: string;
  dueAt: string;
  recurrenceType: string;
  recurrenceInterval?: number | null;
  animalId?: number | null;
  enclosureId?: number | null;
};

export class TaskApiError extends Error {
  details: string[];

  constructor(message: string, details: string[] = []) {
    super(message);
    this.name = "TaskApiError";
    this.details = details;
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

export async function createTask(request: CreateTaskRequest): Promise<HusbandryTask> {
  const response = await fetch(`${apiBaseUrl}/api/tasks`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw await createTaskApiError(response);
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

async function createTaskApiError(response: Response) {
  const fallbackMessage = `Task request failed: ${response.status}`;

  try {
    const errorBody = await response.json();
    return new TaskApiError(errorBody.message ?? errorBody.title ?? fallbackMessage, getTaskErrorDetails(errorBody));
  } catch {
    return new TaskApiError(fallbackMessage);
  }
}

function getTaskErrorDetails(errorBody: unknown) {
  if (!errorBody || typeof errorBody !== "object" || !("errors" in errorBody)) {
    return [];
  }

  const errors = (errorBody as { errors?: Record<string, string[]> }).errors;

  if (!errors) {
    return [];
  }

  return Object.entries(errors).flatMap(([fieldName, messages]) =>
    messages.map((message) => `${fieldName}: ${message}`),
  );
}
