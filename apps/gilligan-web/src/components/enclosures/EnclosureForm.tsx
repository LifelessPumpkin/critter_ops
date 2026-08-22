"use client";

import { useRouter } from "next/navigation";
import { type FormEvent, type ReactNode, useState } from "react";
import { Button, ButtonLink, Input, Select, Textarea } from "@/components/ui";
import {
  EnclosureApiError,
  type CreateEnclosureRequest,
  createEnclosure,
} from "@/lib/api/enclosures";

type EnclosureFormState = {
  name: string;
  type: string;
  location: string;
  status: string;
  sizeLabel: string;
  length: string;
  width: string;
  height: string;
  dimensionUnit: string;
  volume: string;
  volumeUnit: string;
  material: string;
  maxAnimalCapacity: string;
  mobility: string;
  safetyRating: string;
  notes: string;
};

type FieldErrors = Partial<Record<keyof EnclosureFormState, string>>;

const enclosureTypes = [
  "Aquarium",
  "Pond",
  "Terrarium",
  "Vivarium",
  "Paludarium",
  "Aviary",
  "Kennel",
  "Cage",
  "Crate",
  "Hutch",
  "Beehive",
  "Stable",
  "Tub",
  "Rack",
  "OutdoorRun",
  "Other",
];

const enclosureTypeOptions = enclosureTypes.map((option) => ({
  label: formatEnumLabel(option),
  value: option,
}));

const mobilityOptions = ["Fixed", "Movable", "Portable", "Temporary", "OutdoorPermanent"];

const mobilitySelectOptions = mobilityOptions.map((option) => ({
  label: formatEnumLabel(option),
  value: option,
}));

const statusOptions = [
  "Active",
  "Empty",
  "Inactive",
  "UnderMaintenance",
  "CleaningRequired",
  "Quarantine",
  "Reserved",
  "Retired",
];

const statusSelectOptions = statusOptions.map((option) => ({
  label: formatEnumLabel(option),
  value: option,
}));

const initialFormState: EnclosureFormState = {
  name: "",
  type: "",
  location: "",
  status: "",
  sizeLabel: "",
  length: "",
  width: "",
  height: "",
  dimensionUnit: "",
  volume: "",
  volumeUnit: "",
  material: "",
  maxAnimalCapacity: "",
  mobility: "",
  safetyRating: "",
  notes: "",
};

export function EnclosureForm() {
  const router = useRouter();
  const [formState, setFormState] = useState(initialFormState);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [apiErrors, setApiErrors] = useState<string[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const validationErrors = validateForm(formState);
    setFieldErrors(validationErrors);
    setFormError(null);
    setApiErrors([]);

    if (Object.keys(validationErrors).length > 0) {
      setFormError("Please fix the highlighted fields before creating the enclosure.");
      return;
    }

    setIsSubmitting(true);

    try {
      await createEnclosure(toCreateRequest(formState));
      router.push("/enclosures");
      router.refresh();
    } catch (error) {
      if (error instanceof EnclosureApiError) {
        setFormError(error.message);
        setApiErrors(error.details);
      } else {
        setFormError("Unable to create enclosure. Please try again.");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  function updateField(fieldName: keyof EnclosureFormState, value: string) {
    setFormState((currentState) => ({
      ...currentState,
      [fieldName]: value,
    }));

    if (fieldErrors[fieldName]) {
      setFieldErrors((currentErrors) => {
        const nextErrors = { ...currentErrors };
        delete nextErrors[fieldName];
        return nextErrors;
      });
    }
  }

  return (
    <form className="entity-form animated-fade-in" onSubmit={handleSubmit} noValidate>
      {formError ? (
        <div className="form-error-panel" role="alert">
          <p>{formError}</p>
          {apiErrors.length > 0 ? (
            <ul>
              {apiErrors.map((apiError) => (
                <li key={apiError}>{apiError}</li>
              ))}
            </ul>
          ) : null}
        </div>
      ) : null}

      <FormSection title="General Information">
        <Input
          label="Name"
          name="name"
          value={formState.name}
          required
          error={fieldErrors.name}
          onChange={(event) => updateField("name", event.target.value)}
        />
        <Select
          label="Type"
          name="type"
          value={formState.type}
          options={enclosureTypeOptions}
          placeholder="Select type"
          required
          error={fieldErrors.type}
          onChange={(event) => updateField("type", event.target.value)}
        />
        <Input
          label="Location"
          name="location"
          value={formState.location}
          required
          error={fieldErrors.location}
          onChange={(event) => updateField("location", event.target.value)}
        />
        <Select
          label="Status"
          name="status"
          value={formState.status}
          options={statusSelectOptions}
          placeholder="Select status"
          required
          error={fieldErrors.status}
          onChange={(event) => updateField("status", event.target.value)}
        />
      </FormSection>

      <FormSection title="Size & Construction">
        <Input
          label="Size Label"
          name="sizeLabel"
          value={formState.sizeLabel}
          onChange={(event) => updateField("sizeLabel", event.target.value)}
        />
        <Input
          label="Length"
          name="length"
          value={formState.length}
          type="number"
          min="0"
          step="0.01"
          error={fieldErrors.length}
          onChange={(event) => updateField("length", event.target.value)}
        />
        <Input
          label="Width"
          name="width"
          value={formState.width}
          type="number"
          min="0"
          step="0.01"
          error={fieldErrors.width}
          onChange={(event) => updateField("width", event.target.value)}
        />
        <Input
          label="Height"
          name="height"
          value={formState.height}
          type="number"
          min="0"
          step="0.01"
          error={fieldErrors.height}
          onChange={(event) => updateField("height", event.target.value)}
        />
        <Input
          label="Dimension Unit"
          name="dimensionUnit"
          value={formState.dimensionUnit}
          placeholder="in, ft, cm"
          onChange={(event) => updateField("dimensionUnit", event.target.value)}
        />
        <Input
          label="Volume"
          name="volume"
          value={formState.volume}
          type="number"
          min="0"
          step="0.01"
          error={fieldErrors.volume}
          onChange={(event) => updateField("volume", event.target.value)}
        />
        <Input
          label="Volume Unit"
          name="volumeUnit"
          value={formState.volumeUnit}
          placeholder="gal, L"
          onChange={(event) => updateField("volumeUnit", event.target.value)}
        />
        <Input
          label="Material"
          name="material"
          value={formState.material}
          onChange={(event) => updateField("material", event.target.value)}
        />
      </FormSection>

      <FormSection title="Capacity & Safety">
        <Input
          label="Max Animal Capacity"
          name="maxAnimalCapacity"
          value={formState.maxAnimalCapacity}
          type="number"
          min="0"
          step="1"
          error={fieldErrors.maxAnimalCapacity}
          onChange={(event) => updateField("maxAnimalCapacity", event.target.value)}
        />
        <Select
          label="Mobility"
          name="mobility"
          value={formState.mobility}
          options={mobilitySelectOptions}
          placeholder="Select mobility"
          required
          error={fieldErrors.mobility}
          onChange={(event) => updateField("mobility", event.target.value)}
        />
        <Input
          label="Safety Rating"
          name="safetyRating"
          value={formState.safetyRating}
          onChange={(event) => updateField("safetyRating", event.target.value)}
        />
      </FormSection>

      <FormSection title="Notes">
        <Textarea
          label="Notes"
          name="notes"
          value={formState.notes}
          rows={5}
          onChange={(event) => updateField("notes", event.target.value)}
        />
      </FormSection>

      <div className="form-actions">
        <ButtonLink href="/enclosures" variant="secondary">
          Cancel
        </ButtonLink>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Creating..." : "Create Enclosure"}
        </Button>
      </div>
    </form>
  );
}

type FormSectionProps = {
  title: string;
  children: ReactNode;
};

function FormSection({ title, children }: FormSectionProps) {
  return (
    <section className="form-section">
      <h2>{title}</h2>
      <div className="form-grid">{children}</div>
    </section>
  );
}

function validateForm(formState: EnclosureFormState) {
  const errors: FieldErrors = {};

  if (!formState.name.trim()) {
    errors.name = "Name is required.";
  }

  if (!formState.type) {
    errors.type = "Type is required.";
  }

  if (!formState.location.trim()) {
    errors.location = "Location is required.";
  }

  if (!formState.status) {
    errors.status = "Status is required.";
  }

  if (!formState.mobility) {
    errors.mobility = "Mobility is required.";
  }

  validateNonnegativeNumber(formState.length, "length", errors);
  validateNonnegativeNumber(formState.width, "width", errors);
  validateNonnegativeNumber(formState.height, "height", errors);
  validateNonnegativeNumber(formState.volume, "volume", errors);
  validateNonnegativeNumber(formState.maxAnimalCapacity, "maxAnimalCapacity", errors, true);

  return errors;
}

function validateNonnegativeNumber(
  value: string,
  fieldName: keyof EnclosureFormState,
  errors: FieldErrors,
  mustBeInteger = false,
) {
  if (!value) {
    return;
  }

  const numericValue = Number(value);

  if (Number.isNaN(numericValue)) {
    errors[fieldName] = "Enter a valid number.";
    return;
  }

  if (numericValue < 0) {
    errors[fieldName] = "Value cannot be negative.";
    return;
  }

  if (mustBeInteger && !Number.isInteger(numericValue)) {
    errors[fieldName] = "Enter a whole number.";
  }
}

function toCreateRequest(formState: EnclosureFormState): CreateEnclosureRequest {
  return {
    name: formState.name.trim(),
    type: formState.type,
    location: formState.location.trim(),
    sizeLabel: toOptionalString(formState.sizeLabel),
    length: toOptionalNumber(formState.length),
    width: toOptionalNumber(formState.width),
    height: toOptionalNumber(formState.height),
    dimensionUnit: toOptionalString(formState.dimensionUnit),
    volume: toOptionalNumber(formState.volume),
    volumeUnit: toOptionalString(formState.volumeUnit),
    material: toOptionalString(formState.material),
    maxAnimalCapacity: toOptionalNumber(formState.maxAnimalCapacity),
    mobility: formState.mobility,
    status: formState.status,
    safetyRating: toOptionalString(formState.safetyRating),
    notes: toOptionalString(formState.notes),
  };
}

function toOptionalString(value: string) {
  const trimmedValue = value.trim();
  return trimmedValue ? trimmedValue : null;
}

function toOptionalNumber(value: string) {
  return value ? Number(value) : null;
}

function formatEnumLabel(value: string) {
  return value.replace(/([a-z])([A-Z])/g, "$1 $2");
}
