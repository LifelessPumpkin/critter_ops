"use client";

import { useEffect, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { Button, ButtonLink, Combobox, Input, Select } from "@/components/ui";
import { createAnimal } from "@/lib/api/animals";
import { fetchEnclosures, type Enclosure } from "@/lib/api/enclosures";

const types = ["Mammal", "Avian", "Herptile", "Aquatic", "Invertebrate", "Amphibian", "Reptile", "Fish", "Other"];
const statuses = ["Active", "Quarantined", "Medical", "Breeding", "OnHold", "Transferred", "Sold", "Deceased", "Released", "Surrendered", "Inactive"];
const sexes = ["Male", "Female", "Unknown", "Hermaphrodite", "Mixed", "NotApplicable"];

export function AnimalForm() {
  const router = useRouter();
  const [enclosures, setEnclosures] = useState<Enclosure[]>([]);
  const [enclosuresLoading, setEnclosuresLoading] = useState(true);
  const [form, setForm] = useState({ name: "", species: "", subspeciesOrMorph: "", enclosureId: "", animalType: "", status: "Active", sex: "Unknown", birthDate: "", acquiredDate: toLocalDateInputValue(new Date()), birthDateIsEstimated: false });
  const [error, setError] = useState("");
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    let active = true;
    fetchEnclosures()
      .then((values) => { if (active) setEnclosures(values); })
      .catch(() => { if (active) setError("Unable to load enclosures."); })
      .finally(() => { if (active) setEnclosuresLoading(false); });
    return () => { active = false; };
  }, []);

  function update(name: string, value: string | boolean) {
    setForm((current) => ({ ...current, [name]: value }));
    setError("");
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!form.name.trim() || !form.species.trim() || !form.enclosureId || !form.animalType || !form.acquiredDate) {
      setError("Complete all required fields before creating the animal.");
      return;
    }
    setSubmitting(true);
    try {
      const animal = await createAnimal({
        enclosureId: Number(form.enclosureId),
        name: form.name.trim(),
        species: form.species.trim(),
        subspeciesOrMorph: form.subspeciesOrMorph.trim() || null,
        animalType: form.animalType,
        status: form.status,
        sex: form.sex,
        birthDate: form.birthDate || null,
        birthDateIsEstimated: form.birthDateIsEstimated,
        acquiredDate: form.acquiredDate,
      });
      router.push(`/animals/${animal.id}/activity`);
      router.refresh();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Unable to create animal. Please try again.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <form className="entity-form animated-fade-in" onSubmit={submit} noValidate>
      {error ? <div className="form-error-panel" role="alert">{error}</div> : null}
      <section className="form-section">
        <h2>Animal details</h2>
        <div className="form-grid">
          <Input label="Name" required value={form.name} onChange={(event) => update("name", event.target.value)} />
          <Input label="Species" required value={form.species} onChange={(event) => update("species", event.target.value)} />
          <Input label="Morph / Breed" value={form.subspeciesOrMorph} onChange={(event) => update("subspeciesOrMorph", event.target.value)} />
          <Select label="Animal Type" required value={form.animalType} placeholder="Select type" options={toOptions(types)} onValueChange={(value) => update("animalType", value)} />
          <Select label="Status" required value={form.status} options={toOptions(statuses)} onValueChange={(value) => update("status", value)} />
          <Select label="Sex" required value={form.sex} options={toOptions(sexes)} onValueChange={(value) => update("sex", value)} />
          <Combobox label="Enclosure" required value={form.enclosureId} placeholder="Search or select enclosure" searchPlaceholder="Search enclosures..." loading={enclosuresLoading} emptyMessage="No enclosures available." options={enclosures.map((enclosure) => ({ label: `${enclosure.name} · ${enclosure.location}`, value: String(enclosure.id) }))} onValueChange={(value) => update("enclosureId", value)} />
          <Input label="Acquired Date" required type="date" value={form.acquiredDate} onChange={(event) => update("acquiredDate", event.target.value)} />
          <Input label="Date of Birth" type="date" value={form.birthDate} onChange={(event) => update("birthDate", event.target.value)} />
          <label className="animal-checkbox-field"><input type="checkbox" checked={form.birthDateIsEstimated} onChange={(event) => update("birthDateIsEstimated", event.target.checked)} /> Birth date is estimated</label>
        </div>
      </section>
      <div className="form-actions"><Button type="submit" disabled={submitting}>{submitting ? "Adding animal…" : "Add Animal"}</Button><ButtonLink href="/animals" variant="secondary">Cancel</ButtonLink></div>
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
