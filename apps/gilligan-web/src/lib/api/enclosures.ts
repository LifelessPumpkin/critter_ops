export type Enclosure = {
  id: number;
  name: string;
  type: string;
  location: string;
  sizeLabel?: string | null;
  material?: string | null;
  maxAnimalCapacity?: number | null;
  status: string;
};

const apiBaseUrl = "http://localhost:5198";

export async function fetchEnclosures(): Promise<Enclosure[]> {
  const response = await fetch(`${apiBaseUrl}/api/enclosures`);

  if (!response.ok) {
    throw new Error(`Failed to fetch enclosures: ${response.status}`);
  }

  return response.json();
}
