export const activityEventTypes = [
  "Feeding",
  "AnimalMovement",
  "Medication",
  "Treatment",
  "AnimalDisposition",
  "Cleaning",
  "WaterTest",
  "Task",
  "Note",
  "Other",
] as const;

export type ActivityEventType = (typeof activityEventTypes)[number];
export const activitySortOptions = [
  { value: "Newest", label: "Occurred · Newest first" },
  { value: "Oldest", label: "Occurred · Oldest first" },
  { value: "TypeAscending", label: "Activity type · A–Z" },
  { value: "TypeDescending", label: "Activity type · Z–A" },
  { value: "AnimalAscending", label: "Animal · A–Z" },
  { value: "AnimalDescending", label: "Animal · Z–A" },
  { value: "EnclosureAscending", label: "Enclosure · A–Z" },
  { value: "EnclosureDescending", label: "Enclosure · Z–A" },
  { value: "PerformerAscending", label: "Performer · A–Z" },
  { value: "PerformerDescending", label: "Performer · Z–A" },
] as const;

export type ActivitySortDirection = (typeof activitySortOptions)[number]["value"];

export function getActivitySortLabel(sort: ActivitySortDirection) {
  return activitySortOptions.find((option) => option.value === sort)?.label ?? "Occurred · Newest first";
}

export type ActivityAssociation = {
  id: number;
  name: string;
};

export type ActivityDetails = {
  movement?: {
    fromEnclosureId: number;
    fromEnclosureName: string;
    toEnclosureId: number;
    toEnclosureName: string;
    reason?: string | null;
  } | null;
  feeding?: {
    food: string;
    quantity: number;
    unit: string;
    result: string;
  } | null;
  disposition?: {
    dispositionType: string;
    reason?: string | null;
    recipientOrDestination?: string | null;
  } | null;
  medication?: {
    medicationName: string;
    dose: number;
    doseUnit: string;
    route: string;
    result?: string | null;
  } | null;
  treatment?: {
    treatmentType?: string | null;
    treatmentName: string;
    result?: string | null;
  } | null;
  cleaning?: {
    cleaningType: string;
    waterChangePercent?: number | null;
    substrateChanged: boolean;
    equipmentCleaned?: string | null;
  } | null;
};

type ActivitySearchResultDto = {
  id: number;
  eventType: ActivityEventType;
  occurredAt: string;
  title: string;
  performedBy?: string | null;
  notes?: string | null;
  sourceType?: string | null;
  metadata?: unknown;
  createdAt: string;
  animals: Array<{ animalId: number; animalName: string }>;
  enclosures: Array<{ enclosureId: number; enclosureName: string }>;
  details: ActivityDetails;
};

export type ActivityRecord = {
  id: number;
  type: ActivityEventType;
  occurredAt: string;
  createdAt: string;
  title: string;
  description: string;
  notes?: string | null;
  performer?: string | null;
  sourceType?: string | null;
  metadata?: unknown;
  details: ActivityDetails;
  animals: ActivityAssociation[];
  enclosures: ActivityAssociation[];
};

export type ActivitySearchResponse = {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  items: ActivityRecord[];
};

export type ActivitySearchQuery = {
  search?: string;
  eventTypes?: ActivityEventType[];
  animalId?: number;
  enclosureId?: number;
  performedBy?: string;
  from?: string;
  to?: string;
  sort?: ActivitySortDirection;
  page?: number;
  pageSize?: number;
};

const apiBaseUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5198";

export async function fetchActivities(
  query: ActivitySearchQuery,
  signal?: AbortSignal,
): Promise<ActivitySearchResponse> {
  const parameters = new URLSearchParams();

  append(parameters, "Search", query.search);
  query.eventTypes?.forEach((eventType) => parameters.append("EventTypes", eventType));
  append(parameters, "AnimalId", query.animalId);
  append(parameters, "EnclosureId", query.enclosureId);
  append(parameters, "PerformedBy", query.performedBy);
  append(parameters, "From", query.from);
  append(parameters, "To", query.to);
  append(parameters, "Sort", query.sort ?? "Newest");
  append(parameters, "Page", query.page ?? 1);
  append(parameters, "PageSize", query.pageSize ?? 100);

  const response = await fetch(`${apiBaseUrl}/api/activity?${parameters.toString()}`, { signal });

  if (!response.ok) {
    throw new Error(`Failed to fetch activity: ${response.status}`);
  }

  const result = (await response.json()) as Omit<ActivitySearchResponse, "items"> & {
    items: ActivitySearchResultDto[];
  };

  return {
    ...result,
    items: result.items.map(normalizeActivity),
  };
}

function normalizeActivity(activity: ActivitySearchResultDto): ActivityRecord {
  return {
    id: activity.id,
    type: activity.eventType,
    occurredAt: activity.occurredAt,
    createdAt: activity.createdAt,
    title: activity.title,
    description: formatActivityDescription(activity),
    notes: activity.notes,
    performer: activity.performedBy,
    sourceType: activity.sourceType,
    metadata: activity.metadata,
    details: activity.details,
    animals: activity.animals.map((animal) => ({ id: animal.animalId, name: animal.animalName })),
    enclosures: activity.enclosures.map((enclosure) => ({
      id: enclosure.enclosureId,
      name: enclosure.enclosureName,
    })),
  };
}

function formatActivityDescription(activity: ActivitySearchResultDto) {
  const { details } = activity;
  let structuredDetail: string | undefined;

  if (details.movement) {
    structuredDetail = `${details.movement.fromEnclosureName} → ${details.movement.toEnclosureName}`;
  } else if (details.feeding) {
    structuredDetail = `${formatNumber(details.feeding.quantity)} ${formatEnumLabel(details.feeding.unit)} ${details.feeding.food}`;
  } else if (details.medication) {
    structuredDetail = `${details.medication.medicationName}, ${formatNumber(details.medication.dose)} ${details.medication.doseUnit}`;
  } else if (details.treatment) {
    structuredDetail = details.treatment.treatmentName;
  } else if (details.disposition) {
    structuredDetail = [
      formatEnumLabel(details.disposition.dispositionType),
      details.disposition.recipientOrDestination,
    ]
      .filter(Boolean)
      .join(" · ");
  } else if (details.cleaning) {
    structuredDetail = [
      formatEnumLabel(details.cleaning.cleaningType),
      details.cleaning.waterChangePercent != null
        ? `${formatNumber(details.cleaning.waterChangePercent)}% water change`
        : null,
      details.cleaning.equipmentCleaned,
    ]
      .filter(Boolean)
      .join(" · ");
  }

  return [structuredDetail || activity.title, activity.notes].filter(Boolean).join(" — ");
}

function append(parameters: URLSearchParams, key: string, value: string | number | undefined) {
  if (value !== undefined && value !== "") {
    parameters.append(key, String(value));
  }
}

export function formatEnumLabel(value: string) {
  return value.replace(/([a-z])([A-Z])/g, "$1 $2");
}

function formatNumber(value: number) {
  return new Intl.NumberFormat("en", { maximumFractionDigits: 2 }).format(value);
}
