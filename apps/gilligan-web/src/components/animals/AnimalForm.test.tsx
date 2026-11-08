import { act, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { AnimalForm } from "./AnimalForm";
import { EnclosureForm } from "@/components/enclosures/EnclosureForm";
import * as drafts from "./animalDraft";
import { clearAnimalDraft, loadAnimalDraft, saveAnimalDraft, animalReturnUrl } from "./animalDraft";
import { createAnimal, fetchAnimal, updateAnimal } from "@/lib/api/animals";
import { createEnclosure, fetchEnclosure, fetchEnclosures } from "@/lib/api/enclosures";
const { push, refresh } = vi.hoisted(() => ({ push: vi.fn(), refresh: vi.fn() }));
vi.mock("next/navigation", () => ({ useRouter: () => ({ push, refresh }) }));
vi.mock("@/lib/api/animals", () => ({ createAnimal: vi.fn(), updateAnimal: vi.fn(), fetchAnimal: vi.fn() }));
vi.mock("@/lib/api/enclosures", () => ({ fetchEnclosures: vi.fn(), fetchEnclosure: vi.fn(), createEnclosure: vi.fn(), EnclosureApiError: class extends Error {} }));
vi.mock("@/lib/api/animalCatalog", () => ({ fetchAnimalGroups: vi.fn().mockResolvedValue([]), searchSpecies: vi.fn().mockResolvedValue({ items: [] }), searchVariants: vi.fn().mockResolvedValue({ items: [] }) }));
const workflow = "00000000-0000-4000-8000-000000000001";
const draft = { variantAssignments: [{ customValue: "Local morph", displayName: "Local morph", variantType: "Morph", isCustom: true }], name: "Mango", species: "Legacy animal", subspeciesOrMorph: "Local morph", animalType: "Herptile", animalGroupId: "", selectedSpecies: null, selectedVariant: null, enclosureId: "1", acquiredDate: "2026-01-01", status: "Active", sex: "Unknown", notes: "Keep these notes", birthDate: "", birthDateIsEstimated: false };
const habitat = { id: 1, name: "Previous habitat", location: "Room" };
beforeEach(() => { sessionStorage.clear(); window.history.replaceState(null, "", "/animals/new"); vi.mocked(fetchEnclosures).mockResolvedValue([habitat] as never); vi.mocked(fetchEnclosure).mockResolvedValue({ ...habitat, id: 42, name: "New habitat" } as never); });
afterEach(() => { cleanup(); vi.clearAllMocks(); });
it("scopes drafts, rejects expired drafts and clears completed workflows", () => {
  saveAnimalDraft(workflow, draft);
  expect(loadAnimalDraft(workflow, draft)).toEqual(draft);
  expect(loadAnimalDraft("different", draft)).toBeNull();
  const now = Date.now(); vi.spyOn(Date, "now").mockReturnValue(now + 7 * 60 * 60 * 1000);
  expect(loadAnimalDraft(workflow, draft)).toBeNull(); vi.restoreAllMocks();
  saveAnimalDraft(workflow, draft); clearAnimalDraft(workflow); expect(loadAnimalDraft(workflow, draft)).toBeNull();
});
it("restores every field, selects a returned enclosure absent from the initial list and clears the draft on success", async () => {
  saveAnimalDraft(workflow, draft); vi.mocked(createAnimal).mockResolvedValue({ id: 12 } as never);
  render(<AnimalForm workflow={workflow} enclosureId={42} />);
  await waitFor(() => expect(screen.getByLabelText(/^Name/)).toHaveValue("Mango"));
  await waitFor(() => expect(screen.getByRole("combobox", { name: /^Enclosure/ })).toHaveTextContent("New habitat"));
  expect(screen.getByLabelText(/^Notes/)).toHaveValue("Keep these notes");
  fireEvent.click(screen.getByRole("button", { name: "Add Animal" }));
  await waitFor(() => expect(createAnimal).toHaveBeenCalledWith(expect.objectContaining({ name: "Mango", enclosureId: 42, customSpeciesName: "Legacy animal", variantAssignments: [expect.objectContaining({ customValue: "Local morph", variantType: "Morph" })], notes: "Keep these notes" })));
  await waitFor(() => expect(push).toHaveBeenCalledWith("/animals/12/activity"));
  expect(loadAnimalDraft(workflow, draft)).toBeNull();
});
it("preserves the previous enclosure on cancellation and clears animal draft on explicit cancel", async () => {
  saveAnimalDraft(workflow, draft); render(<AnimalForm workflow={workflow} />);
  await act(async () => {});
  expect(screen.getByRole("combobox", { name: /^Enclosure/ })).toHaveTextContent("Previous habitat");
  fireEvent.click(screen.getByRole("link", { name: "Cancel" }));
  expect(loadAnimalDraft(workflow, draft)).toBeNull();
});
it("saves the current animal before navigating to the existing enclosure form", async () => {
  saveAnimalDraft(workflow, draft); render(<AnimalForm workflow={workflow} />);
  await act(async () => {});
  fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: "Updated Mango" } });
  fireEvent.click(screen.getByRole("combobox", { name: /^Enclosure/ }));
  fireEvent.mouseDown(screen.getByRole("option", { name: "+ Create new enclosure" }));
  expect(push).toHaveBeenCalledWith(`/enclosures/new?animalWorkflow=${workflow}`);
  expect(loadAnimalDraft(workflow, draft)?.name).toBe("Updated Mango");
});
it("returns enclosure cancellation to the originating workflow and preserves direct navigation", () => {
  const first = render(<EnclosureForm animalWorkflow={workflow} />);
  expect(screen.getByRole("link", { name: "Cancel" })).toHaveAttribute("href", animalReturnUrl(workflow)); first.unmount();
  render(<EnclosureForm />); expect(screen.getByRole("link", { name: "Cancel" })).toHaveAttribute("href", "/enclosures");
});
it("returns the actual created enclosure ID after successful enclosure creation", async () => {
  vi.mocked(createEnclosure).mockResolvedValue({ id: 42 } as never);
  render(<EnclosureForm animalWorkflow={workflow} />);
  fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: "New habitat" } });
  fireEvent.change(screen.getByLabelText(/^Location/), { target: { value: "Room" } });
  for (const [label, option] of [[/^Type/, "Terrarium"], [/^Status/, "Active"], [/^Mobility/, "Fixed"]] as const) {
    fireEvent.click(screen.getByRole("combobox", { name: label }));
    fireEvent.mouseDown(screen.getByRole("option", { name: option }));
  }
  fireEvent.click(screen.getByRole("button", { name: "Create Enclosure" }));
  await waitFor(() => expect(push).toHaveBeenCalledWith(animalReturnUrl(workflow, 42)));
});

it("edits existing references without losing undisplayed legacy metadata", async () => {
  vi.mocked(fetchAnimal).mockResolvedValue({ id: 7, enclosureId: 1, enclosureName: "Previous habitat", name: "Existing python", species: "Ball python", speciesId: 4, speciesScientificName: "Python regius", animalGroupId: 3, animalGroupName: "Reptile", animalType: "Reptile", variantAssignments: [{ assignmentId: 10, animalVariantId: 3, speciesId: 4, displayName: "Albino", variantType: "Morph", isCustom: false }], animalVariantId: 3, subspeciesOrMorph: "Albino", variantType: "Morph", preferredVariantType: "Morph", status: "Active", sex: "Unknown", acquiredDate: "2026-01-01", source: "Historical source", microchipNumber: "Historical chip", tagIdentifier: "Historical tag", dispositionReason: "Historical reason" });
  vi.mocked(updateAnimal).mockResolvedValue({ id: 7 } as never);
  render(<AnimalForm animalId={7} />);
  await waitFor(() => expect(screen.getByLabelText(/^Name/)).toHaveValue("Existing python"));
  expect(screen.getByRole("combobox", { name: /^Species/ })).toHaveTextContent("Python regius");
  fireEvent.change(screen.getByLabelText(/^Notes/), { target: { value: "Updated notes" } });
  fireEvent.click(screen.getByRole("button", { name: "Save Animal" }));
  await waitFor(() => expect(updateAnimal).toHaveBeenCalledWith(7, expect.objectContaining({ source: "Historical source", microchipNumber: "Historical chip", tagIdentifier: "Historical tag", dispositionReason: "Historical reason", notes: "Updated notes" })));
  expect(vi.mocked(updateAnimal).mock.calls[0][1].variantAssignments).toEqual([expect.objectContaining({ assignmentId: 10, animalVariantId: 3 })]);
  expect(vi.mocked(updateAnimal).mock.calls[0][1]).not.toHaveProperty("speciesId");
  expect(vi.mocked(updateAnimal).mock.calls[0][1]).not.toHaveProperty("animalVariantId");
  expect(vi.mocked(updateAnimal).mock.calls[0][1]).not.toHaveProperty("customSpeciesName");
});
it("keeps the animal form intact when draft storage fails", async () => {
  saveAnimalDraft(workflow, draft);
  render(<AnimalForm workflow={workflow} />);
  await act(async () => {});
  const storage = vi.spyOn(drafts, "saveAnimalDraft").mockImplementation(() => { throw new Error("Disabled storage"); });
  fireEvent.click(screen.getByRole("combobox", { name: /^Enclosure/ }));
  fireEvent.mouseDown(screen.getByRole("option", { name: "+ Create new enclosure" }));
  expect(push).not.toHaveBeenCalled();
  expect(screen.getByLabelText(/^Name/)).toHaveValue("Mango");
  expect(screen.getByRole("alert")).toHaveTextContent("Unable to preserve this draft");
  storage.mockRestore();
});

it("creates structured references without duplicating catalog labels into custom fields", async () => {
  saveAnimalDraft(workflow, { ...draft, species: "Ball python", subspeciesOrMorph: "Albino", animalGroupId: "3", animalType: "Reptile",
    selectedSpecies: { id: 4, commonName: "Ball python", scientificName: "Python regius", animalGroupId: 3, animalGroupName: "Reptile" },
    variantAssignments: [{ animalVariantId: 3, speciesId: 4, displayName: "Albino", variantType: "Morph", isCustom: false }] });
  vi.mocked(createAnimal).mockResolvedValue({ id: 12 } as never);
  render(<AnimalForm workflow={workflow} />);
  await act(async () => {});
  fireEvent.click(screen.getByRole("button", { name: "Add Animal" }));
  await waitFor(() => expect(createAnimal).toHaveBeenCalledWith(expect.objectContaining({ speciesId: 4, variantAssignments: [expect.objectContaining({ animalVariantId: 3, customValue: null })], customSpeciesName: null })));
  expect(vi.mocked(createAnimal).mock.calls[0][0]).not.toHaveProperty("species");
  expect(vi.mocked(createAnimal).mock.calls[0][0]).not.toHaveProperty("subspeciesOrMorph");
});

it.each(["  Unlisted hybrid  ", ""])("preserves migrated custom or incomplete species on unrelated edits: %s", async (species) => {
  vi.mocked(fetchAnimal).mockResolvedValue({ id: 7, enclosureId: 1, enclosureName: habitat.name, name: "Migrated", species,
    customSpeciesName: species, variantAssignments: [{ assignmentId: 11, customValue: "Local strain", displayName: "Local strain", variantType: "Strain", isCustom: true }], subspeciesOrMorph: "Local strain", animalType: "Herptile", status: "Active", sex: "Unknown", acquiredDate: "2026-01-01" });
  vi.mocked(updateAnimal).mockResolvedValue({ id: 7 } as never);
  render(<AnimalForm animalId={7} />);
  await waitFor(() => expect(screen.getByLabelText(/^Name/)).toHaveValue("Migrated"));
  fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: "Renamed" } });
  fireEvent.click(screen.getByRole("button", { name: "Save Animal" }));
  await waitFor(() => expect(updateAnimal).toHaveBeenCalled());
  expect(vi.mocked(updateAnimal).mock.calls[0][1]).not.toHaveProperty("customSpeciesName");
  expect(vi.mocked(updateAnimal).mock.calls[0][1]).not.toHaveProperty("customVariantName");
});

it("removes a migrated custom variant explicitly while preserving exact custom species text", async () => {
  vi.mocked(fetchAnimal).mockResolvedValue({ id: 7, enclosureId: 1, enclosureName: habitat.name, name: "Migrated", species: "  Hybrid  ",
    variantAssignments: [{ assignmentId: 11, customValue: "Local strain", displayName: "Local strain", variantType: "Strain", isCustom: true }], subspeciesOrMorph: "Local strain", animalType: "Herptile", status: "Active", sex: "Unknown", acquiredDate: "2026-01-01" });
  vi.mocked(updateAnimal).mockResolvedValue({ id: 7 } as never);
  render(<AnimalForm animalId={7} />);
  await waitFor(() => expect(screen.getByLabelText(/^Name/)).toHaveValue("Migrated"));
  fireEvent.click(screen.getByRole("button", { name: "Remove Local strain" }));
  fireEvent.click(screen.getByRole("button", { name: "Save Animal" }));
  await waitFor(() => expect(updateAnimal).toHaveBeenCalledWith(7, expect.objectContaining({ variantAssignments: [] })));
});

it("restores structured and custom assignments in order across the enclosure workflow", () => {
  const mixed = { ...draft, variantAssignments: [{ animalVariantId: 3, speciesId: 4, displayName: "Albino", variantType: "Morph", isCustom: false }, { customValue: "Local line", displayName: "Local line", variantType: "Strain", isCustom: true }] };
  saveAnimalDraft(workflow, mixed);
  expect(loadAnimalDraft(workflow, mixed)?.variantAssignments).toEqual(mixed.variantAssignments);
});

it("upgrades a previous draft's custom variant without dropping it", () => {
  saveAnimalDraft(workflow, { ...draft, variantAssignments: undefined, subspeciesOrMorph: "Old local line" });
  expect(loadAnimalDraft(workflow, { ...draft, variantAssignments: [] })?.variantAssignments).toEqual([expect.objectContaining({ customValue: "Old local line", isCustom: true })]);
});

it("keeps edited custom assignments and displays API validation errors", async () => {
  vi.mocked(fetchAnimal).mockResolvedValue({ id: 7, enclosureId: 1, enclosureName: habitat.name, name: "Custom", species: "Hybrid", animalType: "Other", status: "Active", sex: "Unknown", acquiredDate: "2026-01-01",
    variantAssignments: [{ assignmentId: 12, displayName: "Original", customValue: "Original", variantType: "Other", isCustom: true }] });
  vi.mocked(updateAnimal).mockRejectedValue(new Error("Duplicate variant assignments are not allowed."));
  render(<AnimalForm animalId={7} />);
  await waitFor(() => expect(screen.getByRole("button", { name: "Edit Original" })).toBeInTheDocument());
  fireEvent.click(screen.getByRole("button", { name: "Edit Original" }));
  fireEvent.change(screen.getByLabelText("Edit selected value"), { target: { value: "Edited" } });
  fireEvent.click(screen.getByRole("button", { name: "Save value" }));
  fireEvent.click(screen.getByRole("button", { name: "Save Animal" }));
  await waitFor(() => expect(screen.getByRole("alert")).toHaveTextContent("Duplicate variant"));
  expect(vi.mocked(updateAnimal).mock.calls[0][1].variantAssignments).toEqual([expect.objectContaining({ assignmentId: 12, customValue: "Edited" })]);
  expect(screen.getByRole("button", { name: "Edit Edited" })).toBeInTheDocument();
});

it("places required-field errors above the card and clears them as fields are corrected", async () => {
  render(<AnimalForm />);
  await act(async () => {});
  fireEvent.click(screen.getByRole("button", { name: "Add Animal" }));
  const alert = screen.getByRole("alert");
  expect(alert).toHaveTextContent("Check the highlighted fields");
  expect(alert.nextElementSibling).toHaveClass("form-section");
  expect(screen.getByLabelText(/^Name/)).toHaveAttribute("aria-invalid", "true");
  fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: "New animal" } });
  expect(screen.getByLabelText(/^Name/)).toHaveAttribute("aria-invalid", "false");
  expect(createAnimal).not.toHaveBeenCalled();
});
it("shows incompatible selections in the banner and clears it only after explicit removal", async () => {
  saveAnimalDraft(workflow, { ...draft, variantAssignments: [{ animalVariantId: 1, speciesId: 1, displayName: "Poodle", variantType: "Breed", isCustom: false }, ...draft.variantAssignments] });
  render(<AnimalForm workflow={workflow} />);
  await act(async () => {});
  expect(screen.getByRole("alert")).toHaveTextContent("Poodle");
  expect(screen.getByRole("alert").nextElementSibling).toHaveClass("form-section");
  fireEvent.click(screen.getByRole("button", { name: "Add Animal" }));
  expect(createAnimal).not.toHaveBeenCalled();
  fireEvent.click(screen.getByRole("button", { name: "Remove Poodle" }));
  expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Edit Local morph" })).toBeInTheDocument();
});
