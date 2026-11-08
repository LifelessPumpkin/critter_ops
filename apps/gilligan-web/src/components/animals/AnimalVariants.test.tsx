import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, expect, it } from "vitest";
import { AnimalTable } from "./AnimalTable";
import { assignmentError, type AnimalReferences } from "./AnimalReferenceFields";
afterEach(cleanup);
const references: AnimalReferences = { animalGroupId: "", animalType: "Other", species: "Custom", selectedSpecies: null };
it("normalizes duplicates within a type and retains different descriptive types", () => {
  const values = [{ customValue: " Local  Line ", displayName: "Local Line", variantType: "Morph", isCustom: true }, { customValue: "local line", displayName: "local line", variantType: "Morph", isCustom: true }];
  expect(assignmentError({ ...references, variantAssignments: values })).toMatch(/Duplicate/);
  expect(assignmentError({ ...references, variantAssignments: [values[0], { ...values[1], variantType: "Strain" }] })).toBeNull();
  expect(assignmentError({ ...references, variantAssignments: [{ ...values[0], customValue: " " }] })).toMatch(/require/);
});
it("compacts multiple assignments while retaining the complete tooltip", () => {
  render(<AnimalTable ariaLabel="Animals" animals={[{ id: 1, name: "Mixed animal", enclosureId: 1, enclosureName: "Habitat", enclosureLocation: "Room", species: "Python", animalType: "Reptile", status: "Active", sex: "Unknown", birthDateIsEstimated: false,
    variantAssignments: ["Albino", "Pastel", "Local line", "Long name"].map(displayName => ({ displayName, customValue: displayName, variantType: "Morph", isCustom: true })) }]} />);
  expect(screen.getByText("Albino, Pastel +2")).toHaveAttribute("title", "Albino, Pastel, Local line, Long name");
});
