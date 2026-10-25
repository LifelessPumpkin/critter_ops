"use client";

import { useEffect, useState } from "react";
import { Combobox, Select } from "@/components/ui";
import { fetchAnimalGroups, searchSpecies, searchVariants, type AnimalGroup, type AnimalVariant, type CatalogSpecies } from "@/lib/api/animalCatalog";

export type AnimalReferences = {
  animalGroupId: string;
  animalGroupName?: string | null;
  animalType: string;
  species: string;
  selectedSpecies: CatalogSpecies | null;
  subspeciesOrMorph: string;
  selectedVariant: AnimalVariant | null;
};

export function AnimalReferenceFields({ value, onChange }: { value: AnimalReferences; onChange: (value: AnimalReferences) => void }) {
  const [groups, setGroups] = useState<AnimalGroup[]>([]);
  const [referenceNotice, setReferenceNotice] = useState("");
  const [groupError, setGroupError] = useState("");
  useEffect(() => {
    const controller = new AbortController();
    fetchAnimalGroups(controller.signal).then(setGroups).catch(() => { if (!controller.signal.aborted) setGroupError("Groups unavailable. Continue with a custom species."); });
    return () => controller.abort();
  }, []);
  const variantLabel = value.selectedSpecies?.preferredVariantType ?? "Variant";
  return <>
    <Select label="Animal Group" value={value.animalGroupId || (value.animalType ? "legacy" : "")} error={groupError} placeholder="Any group" options={[
      { value: "", label: "Any group" },
      ...groups.map((group) => ({ value: String(group.id), label: group.name })),
      ...(value.animalGroupId && !groups.some((group) => String(group.id) === value.animalGroupId) ? [{ value: value.animalGroupId, label: value.animalGroupName ?? value.selectedSpecies?.animalGroupName ?? value.animalType ?? "Selected group" }] : []),
      ...(!value.animalGroupId && value.animalType ? [{ value: "legacy", label: `${value.animalType} (legacy)` }] : []),
    ]} onValueChange={(id) => {
      if (id === "legacy") return;
      const group = groups.find((item) => String(item.id) === id);
      const incompatible = value.selectedSpecies && id && String(value.selectedSpecies.animalGroupId) !== id;
      if (incompatible) setReferenceNotice("Species and variant names were kept as custom values after changing the group. Review them before saving.");
      onChange({ ...value, animalGroupId: id, animalGroupName: group?.name ?? null, animalType: group?.legacyAnimalType ?? "", ...(incompatible ? { selectedSpecies: null, species: value.species || value.selectedSpecies?.commonName || value.selectedSpecies?.scientificName || "", selectedVariant: null, subspeciesOrMorph: value.subspeciesOrMorph || value.selectedVariant?.name || "" } : {}) });
    }} />
    {referenceNotice ? <p role="status">{referenceNotice}</p> : null}
    <SpeciesSelector groupId={value.animalGroupId} selected={value.selectedSpecies} custom={value.species} onChange={(selected, custom) => {
      const changed = (selected?.id ?? null) !== (value.selectedSpecies?.id ?? null) || custom !== value.species;
      const variantText = value.subspeciesOrMorph || value.selectedVariant?.name || "";
      if (changed && variantText) setReferenceNotice("The previous variant name was kept as a custom value. Review it for the selected species before saving.");
      onChange({ ...value, selectedSpecies: selected, species: custom,
        ...(changed ? { selectedVariant: null, subspeciesOrMorph: variantText } : {}),
        ...(selected ? { animalGroupId: String(selected.animalGroupId), animalGroupName: selected.animalGroupName,
          animalType: groups.find((g) => g.id === selected.animalGroupId)?.legacyAnimalType ?? value.animalType } : {}) });
    }} />
    <VariantSelector key={value.selectedSpecies?.id ?? "custom"} speciesId={value.selectedSpecies?.id} label={variantLabel} selected={value.selectedVariant} custom={value.subspeciesOrMorph} onChange={(selected, custom) => onChange({ ...value, selectedVariant: selected, subspeciesOrMorph: custom })} />
  </>;
}

function SpeciesSelector({ groupId, selected, custom, onChange }: { groupId: string; selected: CatalogSpecies | null; custom: string; onChange: (value: CatalogSpecies | null, custom: string) => void }) {
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
  return <Combobox allowSelectionWhileLoading label="Species" required value={selected ? String(selected.id) : custom ? "custom" : ""} options={options} loading={loading} error={error} emptyMessage="No species found. Type a name to use a custom species." searchPlaceholder="Search common or scientific name" onSearchChange={setSearch} onValueChange={(id) => {
    if (id === "use-custom") onChange(null, search.trim());
    else if (id !== "custom") { const item = items.find((x) => String(x.id) === id); if (item) onChange(item, item.commonName ?? item.scientificName); }
  }} />;
}

function VariantSelector({ speciesId, label, selected, custom, onChange }: { speciesId?: number; label: string; selected: AnimalVariant | null; custom: string; onChange: (value: AnimalVariant | null, custom: string) => void }) {
  const [search, setSearch] = useState("");
  const key = `${speciesId ?? "custom"}:${search}`;
  const [result, setResult] = useState<{ key: string; items: AnimalVariant[]; error: string }>({ key: "initial", items: [], error: "" });
  const loading = Boolean(speciesId) && result.key !== key;
  const items = !speciesId || loading ? [] : result.items;
  const error = !speciesId || loading ? "" : result.error;
  useEffect(() => {
    let active = true;
    const controller = new AbortController();
    if (!speciesId) return () => { active = false; controller.abort(); };
    searchVariants(search, speciesId, controller.signal).then((page) => { if (active) setResult({ key, items: page.items, error: "" }); })
      .catch(() => { if (active) setResult({ key, items: [], error: "Variants unavailable. Use a custom variant." }); });
    return () => { active = false; controller.abort(); };
  }, [search, speciesId, key]);
  const options = [{ value: "", label: "None" }, ...items.map((item) => ({ value: String(item.id), label: `${item.name} · ${item.variantType}` }))];
  if (selected && !items.some((x) => x.id === selected.id)) options.push({ value: String(selected.id), label: `${selected.name} · ${selected.variantType}` });
  if (!selected && custom) options.push({ value: "custom", label: `${custom} (custom)` });
  if (search.trim() && search.trim().length <= 100) options.push({ value: "use-custom", label: `Use "${search.trim()}" as custom ${label.toLowerCase()}` });
  return <Combobox allowSelectionWhileLoading label={label} value={selected ? String(selected.id) : custom ? "custom" : ""} options={options} loading={loading} error={error} emptyMessage="No variants found. Type a custom value." searchPlaceholder={`Search ${label.toLowerCase()}`} onSearchChange={setSearch} onValueChange={(id) => {
    if (!id) onChange(null, "");
    else if (id === "use-custom") onChange(null, search.trim());
    else if (id !== "custom") { const item = items.find((x) => String(x.id) === id); if (item) onChange(item, item.name); }
  }} />;
}
