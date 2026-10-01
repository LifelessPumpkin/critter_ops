export type Animal = {
  id: number;
  enclosureId: number;
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

const apiBaseUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5198";

export async function fetchAnimals(): Promise<Animal[]> {
  const response = await fetch(`${apiBaseUrl}/api/animals`);

  if (!response.ok) {
    throw new Error(`Failed to fetch animals: ${response.status}`);
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
