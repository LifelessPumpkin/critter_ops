"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { type FormEvent, type ReactNode, useState } from "react";
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

const mobilityOptions = ["Fixed", "Movable", "Portable", "Temporary", "OutdoorPermanent"];

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
        <TextField
          label="Name"
          name="name"
          value={formState.name}
          required
          error={fieldErrors.name}
          onChange={updateField}
        />
        <SelectField
          label="Type"
          name="type"
          value={formState.type}
          options={enclosureTypes}
          required
          error={fieldErrors.type}
          onChange={updateField}
        />
        <TextField
          label="Location"
          name="location"
          value={formState.location}
          required
          error={fieldErrors.location}
          onChange={updateField}
        />
        <SelectField
          label="Status"
          name="status"
          value={formState.status}
          options={statusOptions}
          required
          error={fieldErrors.status}
          onChange={updateField}
        />
      </FormSection>

      <FormSection title="Size & Construction">
        <TextField label="Size Label" name="sizeLabel" value={formState.sizeLabel} onChange={updateField} />
        <NumberField label="Length" name="length" value={formState.length} error={fieldErrors.length} onChange={updateField} />
        <NumberField label="Width" name="width" value={formState.width} error={fieldErrors.width} onChange={updateField} />
        <NumberField label="Height" name="height" value={formState.height} error={fieldErrors.height} onChange={updateField} />
        <TextField
          label="Dimension Unit"
          name="dimensionUnit"
          value={formState.dimensionUnit}
          placeholder="in, ft, cm"
          onChange={updateField}
        />
        <NumberField label="Volume" name="volume" value={formState.volume} error={fieldErrors.volume} onChange={updateField} />
        <TextField
          label="Volume Unit"
          name="volumeUnit"
          value={formState.volumeUnit}
          placeholder="gal, L"
          onChange={updateField}
        />
        <TextField label="Material" name="material" value={formState.material} onChange={updateField} />
      </FormSection>

      <FormSection title="Capacity & Safety">
        <NumberField
          label="Max Animal Capacity"
          name="maxAnimalCapacity"
          value={formState.maxAnimalCapacity}
          step="1"
          error={fieldErrors.maxAnimalCapacity}
          onChange={updateField}
        />
        <SelectField
          label="Mobility"
          name="mobility"
          value={formState.mobility}
          options={mobilityOptions}
          required
          error={fieldErrors.mobility}
          onChange={updateField}
        />
        <TextField label="Safety Rating" name="safetyRating" value={formState.safetyRating} onChange={updateField} />
      </FormSection>

      <FormSection title="Notes">
        <TextAreaField label="Notes" name="notes" value={formState.notes} onChange={updateField} />
      </FormSection>

      <div className="form-actions">
        <Link href="/enclosures" className="button button-secondary">
          Cancel
        </Link>
        <button type="submit" className="button button-primary" disabled={isSubmitting}>
          {isSubmitting ? "Creating..." : "Create Enclosure"}
        </button>
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

type BaseFieldProps = {
  label: string;
  name: keyof EnclosureFormState;
  value: string;
  required?: boolean;
  error?: string;
  placeholder?: string;
  onChange: (fieldName: keyof EnclosureFormState, value: string) => void;
};

function TextField({ label, name, value, required = false, error, placeholder, onChange }: BaseFieldProps) {
  return (
    <label className="form-field">
      <span>
        {label}
        {required ? <span className="required-marker"> Required</span> : null}
      </span>
      <input
        className={error ? "form-control form-control-invalid" : "form-control"}
        name={name}
        value={value}
        placeholder={placeholder}
        aria-invalid={Boolean(error)}
        onChange={(event) => onChange(name, event.target.value)}
      />
      {error ? <span className="field-error">{error}</span> : null}
    </label>
  );
}

function NumberField({
  label,
  name,
  value,
  error,
  step = "0.01",
  onChange,
}: BaseFieldProps & { step?: string }) {
  return (
    <label className="form-field">
      <span>{label}</span>
      <input
        className={error ? "form-control form-control-invalid" : "form-control"}
        name={name}
        value={value}
        type="number"
        min="0"
        step={step}
        aria-invalid={Boolean(error)}
        onChange={(event) => onChange(name, event.target.value)}
      />
      {error ? <span className="field-error">{error}</span> : null}
    </label>
  );
}

function SelectField({ label, name, value, options, required = false, error, onChange }: BaseFieldProps & { options: string[] }) {
  return (
    <label className="form-field">
      <span>
        {label}
        {required ? <span className="required-marker"> Required</span> : null}
      </span>
      <select
        className={error ? "form-control form-control-invalid" : "form-control"}
        name={name}
        value={value}
        aria-invalid={Boolean(error)}
        onChange={(event) => onChange(name, event.target.value)}
      >
        <option value="">Select {label.toLowerCase()}</option>
        {options.map((option) => (
          <option key={option} value={option}>
            {formatEnumLabel(option)}
          </option>
        ))}
      </select>
      {error ? <span className="field-error">{error}</span> : null}
    </label>
  );
}

function TextAreaField({ label, name, value, onChange }: BaseFieldProps) {
  return (
    <label className="form-field form-field-full">
      <span>{label}</span>
      <textarea
        className="form-control form-textarea"
        name={name}
        value={value}
        rows={5}
        onChange={(event) => onChange(name, event.target.value)}
      />
    </label>
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
