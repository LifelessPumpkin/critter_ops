"use client";

import { useEffect, useState, useSyncExternalStore, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { Button, ButtonLink, Combobox, Input, Select } from "@/components/ui";
import { createAnimal, updateAnimal, fetchAnimal } from "@/lib/api/animals";
import { fetchEnclosure, fetchEnclosures, type Enclosure } from "@/lib/api/enclosures";

import { AnimalReferenceFields, type AnimalReferences } from "./AnimalReferenceFields";
import { animalReturnUrl, clearAnimalDraft, loadAnimalDraft, saveAnimalDraft, validWorkflow } from "./animalDraft";

const statuses = ["Active", "Quarantined", "Medical", "Breeding", "OnHold", "Transferred", "Sold", "Deceased", "Released", "Surrendered", "Inactive"];
const sexes = ["Male", "Female", "Unknown", "Hermaphrodite", "Mixed", "NotApplicable"];

type FormState = AnimalReferences & { name: string; enclosureId: string; status: string; sex: string; birthDate: string; acquiredDate: string; birthDateIsEstimated: boolean; notes: string; dispositionDate: string; dispositionReason: string; microchipNumber: string; tagIdentifier: string; source: string };
function initialForm(): FormState { return { name: "", species: "", subspeciesOrMorph: "", enclosureId: "", animalType: "", animalGroupId: "", animalGroupName: "", selectedSpecies: null, selectedVariant: null, status: "Active", sex: "Unknown", birthDate: "", acquiredDate: toLocalDateInputValue(new Date()), birthDateIsEstimated: false, notes: "", dispositionDate: "", dispositionReason: "", microchipNumber: "", tagIdentifier: "", source: "" }; }

type AnimalFormProps = { workflow?: string; enclosureId?: number; animalId?: number };
const subscribeHydration = () => () => {};
export function AnimalForm(props: AnimalFormProps) {
  const hydrated = useSyncExternalStore(subscribeHydration, () => true, () => false);
  if (!hydrated) return <p role="status">Loading animal form…</p>;
  return <AnimalFormContent key={`${props.animalId ?? "new"}:${props.workflow ?? ""}:${props.enclosureId ?? ""}`} {...props} />;
}

function AnimalFormContent({ workflow, enclosureId, animalId }: AnimalFormProps) {
  const router = useRouter();
  const [enclosures, setEnclosures] = useState<Enclosure[]>([]);
  const [enclosuresLoading, setEnclosuresLoading] = useState(true);
  const [restored] = useState(() => !animalId ? loadAnimalDraft(workflow, initialForm()) : null);
  const [form, setForm] = useState<FormState>(() => ({ ...(restored ?? initialForm()), ...(restored && enclosureId ? { enclosureId: String(enclosureId) } : {}) }));
  const [originalReferences, setOriginalReferences] = useState<string | null>(null);
  const [loadingAnimal, setLoadingAnimal] = useState(Boolean(animalId));
  const [activeWorkflow, setActiveWorkflow] = useState<string | undefined>(workflow);
  const [error, setError] = useState(workflow && !restored ? "This animal draft is unavailable or expired. Start a new animal entry." : "");
  const [animalLoadFailed, setAnimalLoadFailed] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    let active = true;
    fetchEnclosures()
      .then((values) => { if (active) setEnclosures((current) => [...values, ...current.filter((item) => !values.some((x) => x.id === item.id))]); })
      .catch(() => { if (active) setError("Unable to load enclosures."); })
      .finally(() => { if (active) setEnclosuresLoading(false); });
    return () => { active = false; };
  }, []);

  useEffect(() => {
    if (!enclosureId || animalId) return;
    let active = true;
    fetchEnclosure(enclosureId).then((enclosure) => {
      if (active) setEnclosures((items) => items.some((x) => x.id === enclosure.id) ? items : [...items, enclosure]);
    }).catch(() => { if (active) setError("The new enclosure is unavailable. Choose another enclosure; your animal information is preserved."); });
    return () => { active = false; };
  }, [enclosureId, animalId]);

  useEffect(() => {
    if (!animalId) return;
    let active = true;
    fetchAnimal(animalId).then((animal) => {
      if (!active) return;
      const loaded = { ...initialForm(), ...animal, enclosureId: String(animal.enclosureId), animalGroupId: animal.animalGroupId ? String(animal.animalGroupId) : "", species: animal.species, subspeciesOrMorph: animal.subspeciesOrMorph ?? "", birthDate: animal.birthDate ?? "", birthDateIsEstimated: animal.birthDateIsEstimated ?? false, notes: animal.notes ?? "", dispositionDate: animal.dispositionDate ?? "", dispositionReason: animal.dispositionReason ?? "", microchipNumber: animal.microchipNumber ?? "", tagIdentifier: animal.tagIdentifier ?? "", source: animal.source ?? "", selectedSpecies: animal.speciesId ? { id: animal.speciesId, commonName: animal.species, scientificName: animal.speciesScientificName ?? "", animalGroupId: animal.animalGroupId!, animalGroupName: animal.animalGroupName ?? "", preferredVariantType: animal.preferredVariantType ?? null } : null, selectedVariant: animal.animalVariantId ? { id: animal.animalVariantId, speciesId: animal.speciesId!, name: animal.subspeciesOrMorph ?? "", variantType: animal.variantType ?? "Other" } : null };
      setForm(loaded);
      setOriginalReferences(referenceKey(loaded));
    }).catch(() => { if (active) { setError("Unable to load animal for editing."); setAnimalLoadFailed(true); } }).finally(() => { if (active) setLoadingAnimal(false); });
    return () => { active = false; };
  }, [animalId]);

  function createNewEnclosure() {
    const id = validWorkflow(activeWorkflow) ? activeWorkflow : crypto.randomUUID();
    try {
      saveAnimalDraft(id, form);
      setActiveWorkflow(id);
      window.history.replaceState(window.history.state, "", animalReturnUrl(id));
      router.push(`/enclosures/new?animalWorkflow=${id}`);
    } catch {
      setError("Unable to preserve this draft in browser storage. Your fields remain here. Enable session storage before creating an enclosure from this form.");
    }
  }

  function update(name: string, value: string | boolean) {
    setForm((current) => ({ ...current, [name]: value }));
    setError("");
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!form.name.trim() || form.name.trim().length > 100 || (!animalId && !form.selectedSpecies && !form.species.trim()) || form.species.trim().length > 100 || form.subspeciesOrMorph.trim().length > 100 || !form.enclosureId || !form.acquiredDate) {
      setError("Complete all required fields before creating the animal.");
      return;
    }
    setSubmitting(true);
    try {
      const request = {
        enclosureId: Number(form.enclosureId),
        name: form.name.trim(),
        ...(animalId && referenceKey(form) === originalReferences ? {} : {
          customSpeciesName: form.selectedSpecies ? null : form.species || null,
          customVariantName: form.selectedVariant ? null : form.subspeciesOrMorph || null,
          animalType: form.animalType || undefined,
          animalGroupId: Number(form.animalGroupId) || null,
          speciesId: form.selectedSpecies?.id ?? null,
          animalVariantId: form.selectedVariant?.id ?? null,
        }),
        notes: form.notes || null,
        dispositionDate: form.dispositionDate || null,
        dispositionReason: form.dispositionReason || null,
        microchipNumber: form.microchipNumber || null,
        tagIdentifier: form.tagIdentifier || null,
        source: form.source || null,
        status: form.status,
        sex: form.sex,
        birthDate: form.birthDate || null,
        birthDateIsEstimated: form.birthDateIsEstimated,
        acquiredDate: form.acquiredDate,
      };
      const animal = animalId ? await updateAnimal(animalId, request) : await createAnimal(request);
      clearAnimalDraft(activeWorkflow);
      if (!animalId) setForm(initialForm());
      router.push(`/animals/${animal.id}/activity`);
      router.refresh();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Unable to create animal. Please try again.");
    } finally {
      setSubmitting(false);
    }
  }

  if (animalLoadFailed) return <p role="alert">{error}</p>;
  if (loadingAnimal) return <p role="status">Loading animal…</p>;

  return (
    <form className="entity-form animated-fade-in" onSubmit={submit} noValidate>
      {error ? <div className="form-error-panel" role="alert">{error}</div> : null}
      <section className="form-section">
        <h2>Animal details</h2>
        <div className="form-grid">
          <Input label="Name" required maxLength={100} value={form.name} onChange={(event) => update("name", event.target.value)} />
          <AnimalReferenceFields value={form} onChange={(references) => setForm((current) => ({ ...current, ...references }))} />
          <Select label="Status" required value={form.status} options={toOptions(statuses)} onValueChange={(value) => update("status", value)} />
          <Select label="Sex" required value={form.sex} options={toOptions(sexes)} onValueChange={(value) => update("sex", value)} />
          <Combobox allowSelectionWhileLoading label="Enclosure" required value={form.enclosureId} placeholder="Search or select enclosure" searchPlaceholder="Search enclosures..." loading={enclosuresLoading} emptyMessage="No enclosures available." options={[...enclosures.map((enclosure) => ({ label: `${enclosure.name} · ${enclosure.location}`, value: String(enclosure.id) })), ...(!animalId ? [{ value: "create", label: "+ Create new enclosure" }] : [])]} onValueChange={(value) => value === "create" ? createNewEnclosure() : update("enclosureId", value)} />
          <Input label="Notes" value={form.notes} onChange={(event) => update("notes", event.target.value)} />
          <Input label="Acquired Date" required type="date" value={form.acquiredDate} onChange={(event) => update("acquiredDate", event.target.value)} />
          <Input label="Date of Birth" type="date" value={form.birthDate} onChange={(event) => update("birthDate", event.target.value)} />
          <label className="animal-checkbox-field"><input type="checkbox" checked={form.birthDateIsEstimated} onChange={(event) => update("birthDateIsEstimated", event.target.checked)} /> Birth date is estimated</label>
        </div>
      </section>
      <div className="form-actions"><Button type="submit" disabled={submitting}>{submitting ? "Saving…" : animalId ? "Save Animal" : "Add Animal"}</Button><ButtonLink href="/animals" variant="secondary" onClick={() => { clearAnimalDraft(activeWorkflow); if (!animalId) setForm(initialForm()); }}>Cancel</ButtonLink></div>
    </form>
  );
}

function toOptions(values: string[]) {
  return values.map((value) => ({ value, label: value.replace(/([a-z])([A-Z])/g, "$1 $2") }));
}

function toLocalDateInputValue(date: Date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function referenceKey(form: AnimalReferences) {
  return JSON.stringify([form.animalGroupId, form.animalType, form.selectedSpecies?.id ?? null,
    form.species, form.selectedVariant?.id ?? null, form.subspeciesOrMorph]);
}
