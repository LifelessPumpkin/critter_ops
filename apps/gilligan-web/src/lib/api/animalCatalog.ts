export type AnimalGroup = { id: number; name: string; sortOrder: number; legacyAnimalType: string };
export type CatalogSpecies = { id: number; commonName: string | null; scientificName: string; animalGroupId: number; animalGroupName: string; preferredVariantType: string | null };
export type AnimalVariant = { id: number; speciesId: number; name: string; variantType: string };
export type CatalogPage<T> = { items: T[]; page: number; pageSize: number; hasMore: boolean };

async function get<T>(path: string, signal?: AbortSignal): Promise<T> {
  const response = await fetch(`/api/skipper/api/${path}`, { signal });
  if (!response.ok) throw new Error("Catalog unavailable. You can still use a custom value.");
  return response.json();
}
export function fetchAnimalGroups(signal?: AbortSignal) { return get<AnimalGroup[]>("animal-groups", signal); }
export function searchSpecies(search: string, animalGroupId?: number, signal?: AbortSignal) {
  const query = new URLSearchParams({ search, pageSize: "20" });
  if (animalGroupId) query.set("animalGroupId", String(animalGroupId));
  return get<CatalogPage<CatalogSpecies>>(`species?${query}`, signal);
}
export function searchVariants(search: string, speciesId: number, signal?: AbortSignal) {
  const query = new URLSearchParams({ search, speciesId: String(speciesId), pageSize: "20" });
  return get<CatalogPage<AnimalVariant>>(`animal-variants?${query}`, signal);
}
