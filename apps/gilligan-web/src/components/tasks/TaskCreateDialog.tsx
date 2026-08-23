"use client";

import { type FormEvent, useEffect, useMemo, useRef, useState } from "react";
import { Button, Input, Select, Textarea } from "@/components/ui";
import { type Animal, fetchAnimals } from "@/lib/api/animals";
import { type Enclosure, fetchEnclosures } from "@/lib/api/enclosures";
import { TaskApiError, type CreateTaskRequest, type HusbandryTask, createTask } from "@/lib/api/tasks";

type TaskCreateDialogProps = {
  isOpen: boolean;
  onClose: () => void;
  onCreated: (task: HusbandryTask) => void;
};

type TaskFormState = {
  title: string;
  description: string;
  taskType: string;
  dueDate: string;
  dueTime: string;
  scheduleType: "one-time" | "recurring";
  recurrenceType: string;
  recurrenceInterval: string;
  animalId: string;
  enclosureId: string;
};

type FieldErrors = Partial<Record<keyof TaskFormState, string>>;

const taskTypeOptions = ["Feeding", "Cleaning", "WaterChange", "Medication", "Inspection", "Maintenance", "General", "Other"].map(
  (value) => ({
    label: formatEnumLabel(value),
    value,
  }),
);

const recurrenceOptions = ["Daily", "Weekly", "Monthly", "Custom"].map((value) => ({
  label: formatEnumLabel(value),
  value,
}));

const initialFormState: TaskFormState = {
  title: "",
  description: "",
  taskType: "",
  dueDate: formatDateInputValue(new Date()),
  dueTime: "09:00",
  scheduleType: "one-time",
  recurrenceType: "Daily",
  recurrenceInterval: "1",
  animalId: "",
  enclosureId: "",
};

export function TaskCreateDialog({ isOpen, onClose, onCreated }: TaskCreateDialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const titleInputRef = useRef<HTMLInputElement>(null);
  const [formState, setFormState] = useState<TaskFormState>(initialFormState);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [apiErrors, setApiErrors] = useState<string[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [animals, setAnimals] = useState<Animal[]>([]);
  const [enclosures, setEnclosures] = useState<Enclosure[]>([]);
  const [isLoadingAssociations, setIsLoadingAssociations] = useState(false);
  const [associationError, setAssociationError] = useState<string | null>(null);

  useEffect(() => {
    const dialog = dialogRef.current;

    if (!dialog) {
      return;
    }

    if (isOpen && !dialog.open) {
      dialog.showModal();
      window.setTimeout(() => titleInputRef.current?.focus(), 0);
    }

    if (!isOpen && dialog.open) {
      dialog.close();
    }
  }, [isOpen]);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    let isMounted = true;

    async function loadAssociations() {
      if (isMounted) {
        setIsLoadingAssociations(true);
        setAssociationError(null);
      }

      try {
        const [animalData, enclosureData] = await Promise.all([fetchAnimals(), fetchEnclosures()]);

        if (isMounted) {
          setAnimals(animalData);
          setEnclosures(enclosureData);
        }
      } catch (error) {
        console.error(error);

        if (isMounted) {
          setAssociationError("Unable to load animal or enclosure options.");
        }
      } finally {
        if (isMounted) {
          setIsLoadingAssociations(false);
        }
      }
    }

    loadAssociations();

    return () => {
      isMounted = false;
    };
  }, [isOpen]);

  const animalOptions = useMemo(
    () =>
      animals.map((animal) => ({
        label: `${animal.name} - ${animal.animalType}`,
        value: String(animal.id),
      })),
    [animals],
  );

  const enclosureOptions = useMemo(
    () =>
      enclosures.map((enclosure) => ({
        label: `${enclosure.name} - ${enclosure.location}`,
        value: String(enclosure.id),
      })),
    [enclosures],
  );

  function handleCancel() {
    resetTransientState();
    onClose();
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const validationErrors = validateForm(formState);
    setFieldErrors(validationErrors);
    setFormError(null);
    setApiErrors([]);

    if (Object.keys(validationErrors).length > 0) {
      setFormError("Please fix the highlighted fields before creating the task.");
      return;
    }

    setIsSubmitting(true);

    try {
      const createdTask = await createTask(toCreateTaskRequest(formState));
      onCreated(createdTask);
      setFormState(initialFormState);
      resetTransientState();
      onClose();
    } catch (error) {
      if (error instanceof TaskApiError) {
        setFormError(error.message);
        setApiErrors(error.details);
      } else {
        setFormError("Unable to create task. Please try again.");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  function updateField(fieldName: keyof TaskFormState, value: string) {
    setFormState((currentState) => {
      const nextState = {
        ...currentState,
        [fieldName]: value,
      };

      if (fieldName === "animalId") {
        const selectedAnimal = animals.find((animal) => String(animal.id) === value);

        if (selectedAnimal?.enclosureId) {
          nextState.enclosureId = String(selectedAnimal.enclosureId);
        }
      }

      if (fieldName === "scheduleType" && value === "one-time") {
        nextState.recurrenceType = "Daily";
        nextState.recurrenceInterval = "1";
      }

      return nextState;
    });

    if (fieldErrors[fieldName]) {
      setFieldErrors((currentErrors) => {
        const nextErrors = { ...currentErrors };
        delete nextErrors[fieldName];
        return nextErrors;
      });
    }
  }

  function resetTransientState() {
    setFieldErrors({});
    setFormError(null);
    setApiErrors([]);
    setIsSubmitting(false);
  }

  return (
    <dialog
      ref={dialogRef}
      className="dialog task-create-dialog"
      aria-labelledby="task-create-title"
      onCancel={(event) => {
        event.preventDefault();
        handleCancel();
      }}
    >
      <div className="dialog-header">
        <div>
          <h2 id="task-create-title">Add Task</h2>
          <p>Create one-time or recurring husbandry work.</p>
        </div>
        <button type="button" className="dialog-close-button" aria-label="Close Add Task" onClick={handleCancel}>
          &times;
        </button>
      </div>

      <form className="entity-form task-create-form" onSubmit={handleSubmit} noValidate>
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

        {associationError ? (
          <div className="form-error-panel" role="alert">
            <p>{associationError}</p>
          </div>
        ) : null}

        <section className="form-section">
          <h3>Task Details</h3>
          <div className="form-grid">
            <Input
              ref={titleInputRef}
              label="Task Name"
              name="title"
              value={formState.title}
              required
              error={fieldErrors.title}
              onChange={(event) => updateField("title", event.target.value)}
            />
            <Select
              label="Task Type"
              name="taskType"
              value={formState.taskType}
              options={taskTypeOptions}
              placeholder="Select task type"
              required
              error={fieldErrors.taskType}
              onChange={(event) => updateField("taskType", event.target.value)}
            />
            <Textarea
              label="Description"
              name="description"
              value={formState.description}
              rows={4}
              onChange={(event) => updateField("description", event.target.value)}
            />
          </div>
        </section>

        <section className="form-section">
          <h3>Schedule</h3>
          <div className="form-grid">
            <Select
              label="Schedule Type"
              name="scheduleType"
              value={formState.scheduleType}
              options={[
                { label: "One-time task", value: "one-time" },
                { label: "Recurring task", value: "recurring" },
              ]}
              required
              onChange={(event) => updateField("scheduleType", event.target.value)}
            />
            <Input
              label={formState.scheduleType === "recurring" ? "Start Date" : "Due Date"}
              name="dueDate"
              type="date"
              value={formState.dueDate}
              required
              error={fieldErrors.dueDate}
              onChange={(event) => updateField("dueDate", event.target.value)}
            />
            <Input
              label="Due Time"
              name="dueTime"
              type="time"
              value={formState.dueTime}
              required
              error={fieldErrors.dueTime}
              onChange={(event) => updateField("dueTime", event.target.value)}
            />

            {formState.scheduleType === "recurring" ? (
              <>
                <Select
                  label="Repeats"
                  name="recurrenceType"
                  value={formState.recurrenceType}
                  options={recurrenceOptions}
                  required
                  error={fieldErrors.recurrenceType}
                  onChange={(event) => updateField("recurrenceType", event.target.value)}
                />
                <Input
                  label={getRecurrenceIntervalLabel(formState.recurrenceType)}
                  name="recurrenceInterval"
                  type="number"
                  min="1"
                  step="1"
                  value={formState.recurrenceInterval}
                  required
                  error={fieldErrors.recurrenceInterval}
                  onChange={(event) => updateField("recurrenceInterval", event.target.value)}
                />
              </>
            ) : null}
          </div>
        </section>

        <section className="form-section">
          <h3>Association</h3>
          <div className="form-grid">
            <Select
              label="Animal"
              name="animalId"
              value={formState.animalId}
              options={animalOptions}
              placeholder={getAssociationPlaceholder(isLoadingAssociations, animals.length, "animal")}
              disabled={isLoadingAssociations || animals.length === 0}
              error={fieldErrors.animalId}
              onChange={(event) => updateField("animalId", event.target.value)}
            />
            <Select
              label="Enclosure"
              name="enclosureId"
              value={formState.enclosureId}
              options={enclosureOptions}
              placeholder={getAssociationPlaceholder(isLoadingAssociations, enclosures.length, "enclosure")}
              disabled={isLoadingAssociations || enclosures.length === 0}
              error={fieldErrors.enclosureId}
              onChange={(event) => updateField("enclosureId", event.target.value)}
            />
          </div>
        </section>

        <div className="form-actions">
          <Button type="button" variant="secondary" onClick={handleCancel}>
            Cancel
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Creating..." : "Create Task"}
          </Button>
        </div>
      </form>
    </dialog>
  );
}

function validateForm(formState: TaskFormState) {
  const errors: FieldErrors = {};

  if (!formState.title.trim()) {
    errors.title = "Task name is required.";
  }

  if (!formState.taskType) {
    errors.taskType = "Task type is required.";
  }

  if (!formState.dueDate) {
    errors.dueDate = "Due date is required.";
  }

  if (!formState.dueTime) {
    errors.dueTime = "Due time is required.";
  }

  if (formState.scheduleType === "recurring") {
    if (!formState.recurrenceType) {
      errors.recurrenceType = "Recurrence type is required.";
    }

    const interval = Number(formState.recurrenceInterval);

    if (!formState.recurrenceInterval || Number.isNaN(interval) || interval < 1 || !Number.isInteger(interval)) {
      errors.recurrenceInterval = "Enter a whole number greater than zero.";
    }
  }

  return errors;
}

function toCreateTaskRequest(formState: TaskFormState): CreateTaskRequest {
  return {
    title: formState.title.trim(),
    description: toOptionalString(formState.description),
    taskType: formState.taskType,
    dueAt: toLocalIsoString(formState.dueDate, formState.dueTime),
    recurrenceType: formState.scheduleType === "recurring" ? formState.recurrenceType : "None",
    recurrenceInterval: formState.scheduleType === "recurring" ? Number(formState.recurrenceInterval) : null,
    animalId: toOptionalNumber(formState.animalId),
    enclosureId: toOptionalNumber(formState.enclosureId),
  };
}

function toLocalIsoString(date: string, time: string) {
  return new Date(`${date}T${time}`).toISOString();
}

function toOptionalString(value: string) {
  const trimmedValue = value.trim();
  return trimmedValue ? trimmedValue : null;
}

function toOptionalNumber(value: string) {
  return value ? Number(value) : null;
}

function getAssociationPlaceholder(isLoading: boolean, count: number, label: string) {
  if (isLoading) {
    return `Loading ${label}s...`;
  }

  if (count === 0) {
    return `No ${label}s available`;
  }

  return `Select ${label}`;
}

function getRecurrenceIntervalLabel(recurrenceType: string) {
  if (recurrenceType === "Weekly") {
    return "Every N weeks";
  }

  if (recurrenceType === "Monthly") {
    return "Every N months";
  }

  return "Every N days";
}

function formatDateInputValue(date: Date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function formatEnumLabel(value: string) {
  return value.replace(/([a-z])([A-Z])/g, "$1 $2");
}
