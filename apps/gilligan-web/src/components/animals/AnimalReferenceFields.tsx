"use client";

import { useEffect, useState } from "react";
import { Combobox, Select } from "@/components/ui";
import { fetchAnimalGroups, searchSpecies, type AnimalGroup, type CatalogSpecies } from "@/lib/api/animalCatalog";
import type { VariantAssignment } from "@/lib/api/animals";
import { VariantPicker } from "./VariantPicker";

export type AnimalReferences = {
  variantAssignments?: VariantAssignment[];
  animalGroupId: string;
  animalGroupName?: string | null;
  animalType: string;
  species: string;
  selectedSpecies: CatalogSpecies | null;
};

export function AnimalReferenceFields({ value, onChange, speciesError }: { value: AnimalReferences; onChange: (value: AnimalReferences) => void; speciesError?: string }) {
  const [groups, setGroups] = useState<AnimalGroup[]>([]);
  const [groupError, setGroupError] = useState("");
  useEffect(() => {
    const controller = new AbortController();
    fetchAnimalGroups(controller.signal).then(setGroups).catch(() => { if (!controller.signal.aborted) setGroupError("Categories unavailable. You can still choose a species."); });
    return () => controller.abort();
  }, []);
  const categoryId = value.selectedSpecies ? String(value.selectedSpecies.animalGroupId ?? value.animalGroupId) : value.animalGroupId;
  return <>
    <div>
      <Select label="Animal Category" disabled={Boolean(value.selectedSpecies)} value={categoryId || (value.animalType ? "legacy" : "")} error={groupError} placeholder="Any category" options={[
        { value: "", label: "Any category" },
        ...groups.map(group => ({ value: String(group.id), label: group.name })),
        ...(categoryId && !groups.some(group => String(group.id) === categoryId) ? [{ value: categoryId, label: value.selectedSpecies?.animalGroupName || value.animalGroupName || value.animalType || "Selected category" }] : []),
        ...(!categoryId && value.animalType ? [{ value: "legacy", label: value.animalType === "Avian" ? "Bird" : value.animalType }] : []),
      ]} onValueChange={id => {
        if (id === "legacy" || value.selectedSpecies) return;
        const group = groups.find(item => String(item.id) === id);
        onChange({ ...value, animalGroupId: id, animalGroupName: group?.name ?? null, animalType: group?.legacyAnimalType ?? "" });
      }} />
      {value.selectedSpecies ? <p className="animal-selection-help">Determined by species.</p> : null}
    </div>
    <div className="form-field-full">
      <SpeciesSelector groupId={value.selectedSpecies ? "" : value.animalGroupId} selected={value.selectedSpecies} custom={value.species} validationError={speciesError} onChange={(selected, custom) => {
        onChange({ ...value, selectedSpecies: selected, species: custom,
          ...(selected ? { animalGroupId: String(selected.animalGroupId), animalGroupName: selected.animalGroupName,
            animalType: groups.find(g => g.id === selected.animalGroupId)?.legacyAnimalType ?? "" } : {}) });
      }} />
    </div>
    <VariantPicker key={value.selectedSpecies?.id ?? "custom"} species={value.selectedSpecies} assignments={value.variantAssignments ?? []} onChange={variantAssignments => onChange({ ...value, variantAssignments })} />
  </>;
}

function SpeciesSelector({ groupId, selected, custom, onChange, validationError }: { validationError?: string; groupId: string; selected: CatalogSpecies | null; custom: string; onChange: (value: CatalogSpecies | null, custom: string) => void }) {
  const [search, setSearch] = useState("");
  const key = `${groupId}:${search}`;
  const [result, setResult] = useState<{ key: string; items: CatalogSpecies[]; error: string }>({ key: "initial", items: [], error: "" });
  const loading = result.key !== key;
  const items = loading ? [] : result.items;
  const error = loading ? "" : result.error;
  useEffect(() => {
    let active = true;
    const controller = new AbortController();
    searchSpecies(search, Number(groupId) || undefined, controller.signal)
      .then((page) => { if (active) setResult({ key, items: page.items, error: "" }); })
      .catch(() => { if (active) setResult({ key, items: [], error: "Catalog unavailable. Use a custom species." }); });
    return () => { active = false; controller.abort(); };
  }, [groupId, search, key]);
  const options = items.map((item) => ({ value: String(item.id), label: `${item.commonName ?? item.scientificName} · ${item.scientificName}` }));
  if (selected && !items.some((x) => x.id === selected.id)) options.unshift({ value: String(selected.id), label: `${selected.commonName ?? selected.scientificName} · ${selected.scientificName}` });
  if (!selected && custom) options.unshift({ value: "custom", label: `${custom} (custom)` });
  if (search.trim() && search.trim().length <= 100) options.push({ value: "use-custom", label: `Use "${search.trim()}" as custom species` });
  return <Combobox allowSelectionWhileLoading label="Species" required value={selected ? String(selected.id) : custom ? "custom" : ""} options={options} loading={loading} error={validationError || error} emptyMessage="No species found. Type a name to use a custom species." searchPlaceholder="Search common or scientific name" onSearchChange={setSearch} onValueChange={(id) => {
    if (id === "use-custom") onChange(null, search.trim());
    else if (id !== "custom") { const item = items.find((x) => String(x.id) === id); if (item) onChange(item, item.commonName ?? item.scientificName); }
  }} />;
}

export function normalizeVariant(value: string) { return value.trim().replace(/\s+/g, " ").toLowerCase(); }
export function assignmentError(value: AnimalReferences): string | null {
  const keys = new Set<string>();
  for (const item of value.variantAssignments ?? []) {
    if (!item.isCustom && item.speciesId !== value.selectedSpecies?.id) return `Remove incompatible variant ${item.displayName} before saving.`;
    if (item.isCustom && (!item.customValue?.trim() || item.customValue.length > 100)) return "Custom variants require 1–100 characters.";
    const key = item.isCustom ? `${item.variantType}:${normalizeVariant(item.customValue ?? "")}` : `id:${item.animalVariantId}`;
    if (keys.has(key)) return "Duplicate variant assignments are not allowed.";
    keys.add(key);
  }
  return null;
}
