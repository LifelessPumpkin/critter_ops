import { act, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useState } from "react";
import { AnimalReferenceFields, type AnimalReferences } from "./AnimalReferenceFields";
import { fetchAnimalGroups, searchSpecies, searchVariants } from "@/lib/api/animalCatalog";
vi.mock("@/lib/api/animalCatalog", () => ({ fetchAnimalGroups: vi.fn(), searchSpecies: vi.fn(), searchVariants: vi.fn() }));
const dog = { id: 1, commonName: "Dog", scientificName: "Canis lupus familiaris", animalGroupId: 1, animalGroupName: "Mammal", preferredVariantType: "Breed" };
const python = { id: 4, commonName: "Ball python", scientificName: "Python regius", animalGroupId: 3, animalGroupName: "Reptile", preferredVariantType: "Morph" };
const empty: AnimalReferences = { animalGroupId: "", animalType: "", species: "", subspeciesOrMorph: "", selectedSpecies: null, selectedVariant: null };
function Harness({ initial = empty }: { initial?: AnimalReferences }) {
  const [value, setValue] = useState(initial);
  return <><AnimalReferenceFields value={value} onChange={setValue} /><output data-testid="value">{JSON.stringify(value)}</output></>;
}
function choose(label: RegExp, option: RegExp) { fireEvent.click(screen.getByRole("combobox", { name: label })); fireEvent.mouseDown(screen.getByRole("option", { name: option })); }
beforeEach(() => {
  vi.mocked(fetchAnimalGroups).mockResolvedValue([{ id: 1, name: "Mammal", sortOrder: 10, legacyAnimalType: "Mammal" }, { id: 3, name: "Reptile", sortOrder: 30, legacyAnimalType: "Reptile" }]);
  vi.mocked(searchSpecies).mockResolvedValue({ items: [dog, python], page: 1, pageSize: 20, hasMore: false });
  vi.mocked(searchVariants).mockResolvedValue({ items: [{ id: 1, speciesId: 1, name: "Poodle", variantType: "Breed" }], page: 1, pageSize: 20, hasMore: false });
});
afterEach(() => { cleanup(); vi.clearAllMocks(); vi.useRealTimers(); });
describe("adaptive animal references", () => {
  it("loads initial suggestions, narrows by group, selects species, adapts labels and filters variants", async () => {
    render(<Harness />);
    await waitFor(() => expect(searchSpecies).toHaveBeenCalledWith("", undefined, expect.any(AbortSignal)));
    await waitFor(() => expect(screen.queryByText("Loading options…")).not.toBeInTheDocument());
    choose(/^Animal Group/, /^Mammal$/);
    await waitFor(() => expect(searchSpecies).toHaveBeenCalledWith("", 1, expect.any(AbortSignal)));
    await act(async () => {});
    choose(/^Species/, /^Dog · Canis/);
    await waitFor(() => expect(searchVariants).toHaveBeenCalledWith("", 1, expect.any(AbortSignal)));
    expect(screen.getByRole("combobox", { name: /^Breed/ })).toBeInTheDocument();
    choose(/^Breed/, /^Poodle/);
    expect(screen.getByTestId("value")).toHaveTextContent('"selectedVariant":{"id":1');
    choose(/^Animal Group/, /^Reptile$/);
    expect(screen.getByTestId("value")).toHaveTextContent('"selectedSpecies":null');
    expect(screen.getByTestId("value")).toHaveTextContent('"selectedVariant":null');
    expect(screen.getByTestId("value")).toHaveTextContent('"species":"Dog"');
    expect(screen.getByTestId("value")).toHaveTextContent('"subspeciesOrMorph":"Poodle"');
    expect(screen.getByText(/names were kept as custom values/)).toBeInTheDocument();
  });
  it("clears variants when species changes", async () => {
    render(<Harness initial={{ ...empty, species: "Dog", selectedSpecies: dog, selectedVariant: { id: 1, speciesId: 1, name: "Poodle", variantType: "Breed" } }} />);
    await act(async () => {});
    choose(/^Species/, /^Ball python/);
    expect(screen.getByRole("combobox", { name: /^Morph/ })).toBeInTheDocument();
    expect(screen.getByTestId("value")).toHaveTextContent('"selectedVariant":null');
  });
  it("debounces scientific search and ignores out-of-order responses", async () => {
    let finishOld!: (value: Awaited<ReturnType<typeof searchSpecies>>) => void;
    vi.mocked(searchSpecies).mockImplementationOnce(() => new Promise((resolve) => { finishOld = resolve; }));
    render(<Harness />);
    fireEvent.click(screen.getByRole("combobox", { name: /^Species/ }));
    vi.useFakeTimers();
    fireEvent.change(screen.getByRole("searchbox"), { target: { value: "Python regius" } });
    await act(async () => { vi.advanceTimersByTime(299); });
    expect(searchSpecies).toHaveBeenCalledTimes(1);
    await act(async () => { vi.advanceTimersByTime(1); });
    expect(searchSpecies).toHaveBeenLastCalledWith("Python regius", undefined, expect.any(AbortSignal));
    await act(async () => finishOld({ items: [{ ...dog, commonName: "Stale result" }], page: 1, pageSize: 20, hasMore: false }));
    expect(screen.queryByRole("option", { name: /Stale result/ })).not.toBeInTheDocument();
  });
  it("supports explicit custom species and custom variant even when catalog fails", async () => {
    vi.mocked(searchSpecies).mockRejectedValue(new Error("Offline"));
    render(<Harness />);
    await act(async () => {});
    fireEvent.click(screen.getByRole("combobox", { name: /^Species/ }));
    fireEvent.change(screen.getByRole("searchbox"), { target: { value: "Unlisted animal" } });
    await waitFor(() => expect(screen.getByRole("option", { name: /Use "Unlisted animal"/ })).toBeInTheDocument());
    fireEvent.mouseDown(screen.getByRole("option", { name: /Use "Unlisted animal"/ }));
    expect(screen.getByTestId("value")).toHaveTextContent('"species":"Unlisted animal"');
    expect(screen.getByTestId("value")).toHaveTextContent('"selectedSpecies":null');
    fireEvent.click(screen.getByRole("combobox", { name: /^Variant/ }));
    fireEvent.change(screen.getByRole("searchbox"), { target: { value: "Local morph" } });
    await waitFor(() => expect(screen.getByRole("option", { name: /Use "Local morph"/ })).toBeInTheDocument());
    fireEvent.mouseDown(screen.getByRole("option", { name: /Use "Local morph"/ }));
    expect(screen.getByTestId("value")).toHaveTextContent('"subspeciesOrMorph":"Local morph"');
  });
});
it("supports keyboard selection, Escape, outside dismissal and custom values during pending requests", async () => {
  render(<Harness />);
  await act(async () => {});
  fireEvent.click(screen.getByRole("combobox", { name: /^Species/ }));
  const input = screen.getByRole("searchbox");
  fireEvent.keyDown(input, { key: "ArrowDown" });
  fireEvent.keyDown(input, { key: "Enter" });
  expect(screen.getByTestId("value")).toHaveTextContent('"species":"Ball python"');
  fireEvent.click(screen.getByRole("combobox", { name: /^Species/ }));
  fireEvent.keyDown(screen.getByRole("searchbox"), { key: "Escape" });
  expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
  fireEvent.click(screen.getByRole("combobox", { name: /^Species/ }));
  fireEvent.pointerDown(document.body);
  expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
  vi.mocked(searchSpecies).mockImplementation(() => new Promise(() => {}));
  fireEvent.click(screen.getByRole("combobox", { name: /^Species/ }));
  fireEvent.change(screen.getByRole("searchbox"), { target: { value: "Slow catalog fallback" } });
  await waitFor(() => expect(screen.getByRole("option", { name: /Use "Slow catalog fallback"/ })).toBeInTheDocument());
  fireEvent.mouseDown(screen.getByRole("option", { name: /Use "Slow catalog fallback"/ }));
  expect(screen.getByTestId("value")).toHaveTextContent('"species":"Slow catalog fallback"');
});
