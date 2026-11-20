import { expect, it } from "vitest";
import { assignmentError, type AnimalReferences } from "./AnimalReferenceFields";

const references: AnimalReferences = { animalGroupId: "", animalType: "Other", species: "Custom", selectedSpecies: null };
it("normalizes duplicates within a type and retains different descriptive types", () => {
  const values = [{ customValue: " Local  Line ", displayName: "Local Line", variantType: "Morph", isCustom: true }, { customValue: "local line", displayName: "local line", variantType: "Morph", isCustom: true }];
  expect(assignmentError({ ...references, variantAssignments: values })).toMatch(/Duplicate/);
  expect(assignmentError({ ...references, variantAssignments: [values[0], { ...values[1], variantType: "Strain" }] })).toBeNull();
  expect(assignmentError({ ...references, variantAssignments: [{ ...values[0], customValue: " " }] })).toMatch(/require/);
});
