"use client";

import { useCallback, useRef, useState } from "react";
import { Button, Combobox, Input, type SelectionOption } from "@/components/ui";
import { searchVariants, type CatalogSpecies } from "@/lib/api/animalCatalog";
import type { VariantAssignment } from "@/lib/api/animals";

import { useCatalogSearch } from "./useCatalogSearch";
import { CatalogNavigation } from "./CatalogNavigation";

const preferredTypes = new Set(["Breed", "Morph", "Variety", "Strain", "Mutation", "Subspecies"]);
const normalize = (value: string) => value.trim().replace(/\s+/g, " ").toLowerCase();

export function VariantPicker({ species, assignments, onChange }: {
  species: CatalogSpecies | null;
  assignments: VariantAssignment[];
  onChange: (assignments: VariantAssignment[]) => void;
}) {
  const type = preferredTypes.has(species?.preferredVariantType ?? "") ? species!.preferredVariantType! : "Other";
  const label = type === "Other" ? "Variant" : type;
  const [search, setSearch] = useState("");
  const [notice, setNotice] = useState("");
  const [editing, setEditing] = useState<{ index: number; text: string } | null>(null);
  const [editError, setEditError] = useState("");
  const container = useRef<HTMLDivElement>(null);
  const [open, setOpen] = useState(false);
  const speciesId = species?.id;
  const fetchPage = useCallback((page: number, signal: AbortSignal) => searchVariants(search, speciesId!, signal, page), [search, speciesId]);
  const catalog = useCatalogSearch(`${speciesId}:${search}`, search, open && Boolean(speciesId), fetchPage, "Unable to load variants. Please try again.");
  const { items, loading, error } = catalog;

  const duplicate = (text: string, variantType: string, except = -1) => assignments.some((item, index) => index !== except && item.isCustom && item.variantType === variantType && normalize(item.customValue ?? "") === normalize(text));
  const options: SelectionOption[] = items.filter(item => item.speciesId === species?.id).map(item => {
    const selected = assignments.some(value => value.animalVariantId === item.id);
    return { value: String(item.id), label: `${item.name}${selected ? " — Selected" : ""}`, disabled: selected };
  });
  const custom = search.trim();
  if (custom && custom.length <= 100) options.push({ value: "custom", label: `Add "${custom}" as a custom ${label.toLowerCase()}` });
  const feedback = custom.length > 100 ? "Use 100 characters or fewer." : notice || error;
  function focusSelector() { requestAnimationFrame(() => container.current?.querySelector<HTMLButtonElement>('[role="combobox"]')?.focus()); }
  function saveEdit() {
    if (!editing) return;
    const item = assignments[editing.index];
    if (!editing.text.trim() || editing.text.length > 100) { setEditError("Enter 1–100 characters."); return; }
    if (duplicate(editing.text, item.variantType, editing.index)) { setEditError("That value is already selected."); return; }
    onChange(assignments.map((value, index) => index === editing.index ? { ...value, customValue: editing.text, displayName: editing.text } : value));
    setEditing(null); setEditError(""); focusSelector();
  }
  return <div className="animal-variant-picker form-field-full" ref={container}>
    <Combobox debounceMs={0} onOpenChange={setOpen} footer={<CatalogNavigation catalog={catalog} noun="variants" />} label={label} value="" placeholder={`Search or add ${label.toLowerCase()}…`} searchPlaceholder={`Search ${label.toLowerCase()}`} allowSelectionWhileLoading options={options} loading={loading}
      emptyMessage="No catalog variants found. You can add a custom value." onSearchChange={setSearch}
      onValueChange={id => {
        if (id === "custom") {
          if (duplicate(custom, type)) { setNotice("That value is already selected."); return; }
          onChange([...assignments, { customValue: custom, displayName: custom, variantType: type, isCustom: true }]);
        } else {
          const item = items.find(value => String(value.id) === id && value.speciesId === species?.id);
          if (!item || assignments.some(value => value.animalVariantId === item.id)) return;
          onChange([...assignments, { animalVariantId: item.id, speciesId: item.speciesId, displayName: item.name, variantType: item.variantType, isCustom: false }]);
        }
        setNotice("");
      }} />
    <p className="animal-selection-help">Optional · Multiple allowed</p>
    <p className="animal-selection-feedback" role="status">{feedback}</p>
    {assignments.length ? <ul className="animal-variant-chips" aria-label={`Selected ${label.toLowerCase()} values`}>
      {assignments.map((item, index) => {
        const incompatible = !item.isCustom && item.speciesId !== species?.id;
        return <li className={`animal-filter-chip animal-variant-chip${incompatible ? " animal-variant-chip-invalid" : ""}`} key={item.assignmentId ?? `${item.animalVariantId ?? "custom"}:${index}`}>
          {item.isCustom ? <button className="animal-chip-edit" type="button" aria-label={`Edit ${item.displayName}`} title="Edit value" onClick={() => { setEditing({ index, text: item.customValue ?? "" }); setEditError(""); }}>{item.displayName}</button> : <span>{item.displayName}</span>}
          {incompatible ? <span className="visually-hidden">Incompatible with selected species</span> : null}
          <button type="button" aria-label={`Remove ${item.displayName}`} onClick={() => { onChange(assignments.filter((_, i) => i !== index)); setEditing(null); setNotice(""); focusSelector(); }}>×</button>
        </li>;
      })}
    </ul> : null}
    {editing ? <div className="animal-variant-edit">
      <Input autoFocus label="Edit selected value" maxLength={100} value={editing.text} error={editError} onChange={event => { setEditing({ ...editing, text: event.target.value }); setEditError(""); }} onKeyDown={event => {
        if (event.key === "Enter") { event.preventDefault(); saveEdit(); }
        if (event.key === "Escape") { event.preventDefault(); setEditing(null); focusSelector(); }
      }} />
      <div><Button type="button" onClick={saveEdit}>Save value</Button><Button type="button" variant="secondary" onClick={() => { setEditing(null); focusSelector(); }}>Cancel edit</Button></div>
    </div> : null}
  </div>;
}
