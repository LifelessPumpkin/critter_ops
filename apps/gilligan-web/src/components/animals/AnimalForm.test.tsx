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
const draft = { name: "Mango", species: "Legacy animal", subspeciesOrMorph: "Local morph", animalType: "Herptile", animalGroupId: "", selectedSpecies: null, selectedVariant: null, enclosureId: "1", acquiredDate: "2026-01-01", status: "Active", sex: "Unknown", notes: "Keep these notes", birthDate: "", birthDateIsEstimated: false };
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
  await waitFor(() => expect(createAnimal).toHaveBeenCalledWith(expect.objectContaining({ name: "Mango", enclosureId: 42, species: "Legacy animal", subspeciesOrMorph: "Local morph", notes: "Keep these notes" })));
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
  vi.mocked(fetchAnimal).mockResolvedValue({ id: 7, enclosureId: 1, enclosureName: "Previous habitat", name: "Existing python", species: "Ball python", speciesId: 4, speciesScientificName: "Python regius", animalGroupId: 3, animalGroupName: "Reptile", animalType: "Reptile", animalVariantId: 3, subspeciesOrMorph: "Albino", variantType: "Morph", preferredVariantType: "Morph", status: "Active", sex: "Unknown", acquiredDate: "2026-01-01", source: "Historical source", microchipNumber: "Historical chip", tagIdentifier: "Historical tag", dispositionReason: "Historical reason" });
  vi.mocked(updateAnimal).mockResolvedValue({ id: 7 } as never);
  render(<AnimalForm animalId={7} />);
  await waitFor(() => expect(screen.getByLabelText(/^Name/)).toHaveValue("Existing python"));
  expect(screen.getByRole("combobox", { name: /^Species/ })).toHaveTextContent("Python regius");
  fireEvent.change(screen.getByLabelText(/^Notes/), { target: { value: "Updated notes" } });
  fireEvent.click(screen.getByRole("button", { name: "Save Animal" }));
  await waitFor(() => expect(updateAnimal).toHaveBeenCalledWith(7, expect.objectContaining({ speciesId: 4, animalGroupId: 3, animalVariantId: 3, source: "Historical source", microchipNumber: "Historical chip", tagIdentifier: "Historical tag", dispositionReason: "Historical reason", notes: "Updated notes" })));
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
