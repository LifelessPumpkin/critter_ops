export type Animal = {
  id: number | string;
  enclosureId: number | string;
  enclosureName: string;
  name: string;
  species: string;
  subspeciesOrMorph?: string | null;
  animalType: string;
  status: string;
  sex: string;
  birthDate?: string | null;
  acquiredDate: string;
  dispositionDate?: string | null;
  dispositionReason?: string | null;
};

export type AnimalListItem = Pick<
  Animal,
  "id" | "enclosureId" | "enclosureName" | "name" | "species" | "subspeciesOrMorph" | "animalType" | "status" | "sex" | "birthDate"
> & {
  enclosureLocation: string;
  birthDateIsEstimated: boolean;
};

export type AnimalLifecycle = "All" | "InCare" | "OutOfCare";

export type AnimalSearchQuery = {
  lifecycle: AnimalLifecycle;
  search?: string;
  statuses?: string[];
  animalTypes?: string[];
  enclosureId?: number;
  page?: number;
  pageSize?: number;
};

export type AnimalSearchResponse = {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  allCount: number;
  inCareCount: number;
  outOfCareCount: number;
  items: AnimalListItem[];
};

export const animalPageSize = 40;

export type CreateAnimalRequest = {
  enclosureId: number;
  name: string;
  species: string;
  subspeciesOrMorph?: string | null;
  animalType: string;
  status: string;
  sex: string;
  birthDate?: string | null;
  birthDateIsEstimated: boolean;
  acquiredDate: string;
};

const apiBaseUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5198";

export async function fetchAnimals(): Promise<Animal[]> {
  const response = await fetch(`${apiBaseUrl}/api/animals`);

  if (!response.ok) {
    throw new Error(`Failed to fetch animals: ${response.status}`);
  }

  return response.json();
}

export async function searchAnimals(
  query: AnimalSearchQuery,
  signal?: AbortSignal,
): Promise<AnimalSearchResponse> {
  const parameters = new URLSearchParams({
    lifecycle: query.lifecycle,
    page: String(query.page ?? 1),
    pageSize: String(query.pageSize ?? animalPageSize),
  });

  if (query.search) parameters.set("search", query.search);
  if (query.enclosureId) parameters.set("enclosureId", String(query.enclosureId));
  query.statuses?.forEach((status) => parameters.append("statuses", status));
  query.animalTypes?.forEach((animalType) => parameters.append("animalTypes", animalType));

  const response = await fetch(`${apiBaseUrl}/api/animals/search?${parameters}`, { signal });

  if (!response.ok) {
    throw new Error(`Failed to search animals: ${response.status}`);
  }

  return response.json();
}

export async function fetchAnimal(id: number): Promise<Animal> {
  const response = await fetch(`${apiBaseUrl}/api/animals/${id}`);

  if (!response.ok) {
    throw new Error(`Failed to fetch animal: ${response.status}`);
  }

  return response.json();
}

export async function createAnimal(request: CreateAnimalRequest): Promise<Animal> {
  const response = await fetch(`${apiBaseUrl}/api/animals`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    let message = `Failed to create animal: ${response.status}`;
    try {
      const body = await response.json();
      message = body.message ?? body.title ?? message;
    } catch {
      // Fall back to the status-based message when the response is not JSON.
    }
    throw new Error(message);
  }

  return response.json();
}
