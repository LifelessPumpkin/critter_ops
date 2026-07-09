export type Animal = {
  id: number;
  enclosureId: number;
  name: string;
  species: string;
  subspeciesOrMorph?: string | null;
  animalType: string;
  status: string;
  sex: string;
  acquiredDate: string;
};

const apiBaseUrl = "http://localhost:5198";

export async function fetchAnimals(): Promise<Animal[]> {
  const response = await fetch(`${apiBaseUrl}/api/animals`);

  if (!response.ok) {
    throw new Error(`Failed to fetch animals: ${response.status}`);
  }

  return response.json();
}
