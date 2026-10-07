export type Enclosure = {
  id: number;
  name: string;
  type: string;
  location: string;
  sizeLabel?: string | null;
  length?: number | null;
  width?: number | null;
  height?: number | null;
  dimensionUnit?: string | null;
  volume?: number | null;
  volumeUnit?: string | null;
  material?: string | null;
  maxAnimalCapacity?: number | null;
  mobility?: string | null;
  status: string;
  safetyRating?: string | null;
  notes?: string | null;
};

export type CreateEnclosureRequest = {
  name: string;
  type: string;
  location: string;
  sizeLabel?: string | null;
  length?: number | null;
  width?: number | null;
  height?: number | null;
  dimensionUnit?: string | null;
  volume?: number | null;
  volumeUnit?: string | null;
  material?: string | null;
  maxAnimalCapacity?: number | null;
  mobility: string;
  status: string;
  safetyRating?: string | null;
  notes?: string | null;
};

export class EnclosureApiError extends Error {
  details: string[];

  constructor(message: string, details: string[] = []) {
    super(message);
    this.name = "EnclosureApiError";
    this.details = details;
  }
}

const apiBaseUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5198";

export async function fetchEnclosures(): Promise<Enclosure[]> {
  const response = await fetch(`${apiBaseUrl}/api/enclosures`);

  if (!response.ok) {
    throw new Error(`Failed to fetch enclosures: ${response.status}`);
  }

  return response.json();
}

export async function createEnclosure(request: CreateEnclosureRequest): Promise<Enclosure> {
  const response = await fetch(`${apiBaseUrl}/api/enclosures`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw await createApiError(response);
  }

  return response.json();
}

async function createApiError(response: Response) {
  const fallbackMessage = `Failed to create enclosure: ${response.status}`;

  try {
    const errorBody = await response.json();
    const details = getErrorDetails(errorBody);

    return new EnclosureApiError(errorBody.title ?? errorBody.message ?? fallbackMessage, details);
  } catch {
    return new EnclosureApiError(fallbackMessage);
  }
}

function getErrorDetails(errorBody: unknown) {
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
