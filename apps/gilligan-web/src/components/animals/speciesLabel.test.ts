import { expect, it } from "vitest";
import { speciesLabel } from "./AnimalReferenceFields";

it("shows the common name and avoids duplicate scientific names", () => {
  const species = { id: 1, commonName: "Sumatran short-tailed python", scientificName: "Python curtus", animalGroupId: 3, animalGroupName: "Reptile", preferredVariantType: null };
  expect(speciesLabel(species)).toBe("Sumatran short-tailed python · Python curtus");
  expect(speciesLabel({ ...species, commonName: null })).toBe("Python curtus");
  expect(speciesLabel({ ...species, commonName: " python CURTUS " })).toBe("python CURTUS");
});
