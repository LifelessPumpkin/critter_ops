# Enclosure Timeline Domain Design

## Purpose

The Enclosure Timeline returns historical events associated with an enclosure.

It provides a shared operational history for activities such as water tests, cleanings, feedings, and tasks without adding many specialized columns to the Enclosure entity.

The design goal is to create a flexible timeline foundation that supports current enclosure operations while allowing new event types to be added later. Enclosure timeline API routes remain enclosure-scoped, but the underlying source of truth is the shared Activity Ledger described in `activity-ledger-domain-design.md`.

## Design Philosophy

Timeline events are modeled as shared activity records with strongly typed common fields and flexible metadata for event-specific values.

Common timeline information is stored on `ActivityEvent`.

Event-specific details are stored in a PostgreSQL `jsonb` metadata field.

This avoids two problems:

* A very wide timeline table with many nullable columns
* A timeline stored entirely as unstructured JSON

The timeline should describe what happened, when it happened, who performed it, and any contextual details needed for display or reporting.

## Core Timeline Event View

Every enclosure timeline response is projected from one `ActivityEvent` related through `ActivityEventEnclosures`.

Field Purpose
Id Shared ActivityEvents bigint identity primary key
EnclosureId Requested enclosure related through ActivityEventEnclosures
EventType Type of timeline event
OccurredAt When the activity actually occurred
Title Short human-readable description
Description Optional notes or context projected from ActivityEvent.Notes
PerformedBy Optional human-readable actor name
SourceReferenceId Optional reference to an originating source record
SourceType Optional source entity type name
Metadata Optional event-specific JSON values
CreatedAt Record creation timestamp
UpdatedAt Last modification timestamp

Timeline event identifiers use the shared `ActivityEvents.Id` incremental `bigint` value. This keeps event URLs compact while leaving room for a large number of future events.

The current Enclosure primary key is an integer, so `EnclosureId` uses the existing integer key to preserve a real foreign key relationship.

## Enclosure Relationship

Every enclosure timeline event must be associated with the requested enclosure.

Relationship:

Enclosure (1)
        │
        │
        └───────────< ActivityEventEnclosure >──────── ActivityEvent

An enclosure may have many activity events.

Deleting an enclosure is restricted while activity event associations exist. This protects operational history from being removed accidentally.

## Event Types

EventType is represented as an enum and stored as a readable string in PostgreSQL.

Initial values include:

* WaterTest
* Cleaning
* Feeding
* AnimalMovement
* AnimalDisposition
* Medication
* Treatment
* Task
* Other

The Other value exists for events that do not yet have a dedicated type.

Future event types can be added without changing the common timeline shape.

Animal movement events are created through `POST /api/animals/{animalId}/movements`, not the generic timeline endpoint. They appear in both source and destination enclosure timelines with wording based on the requested enclosure.

Animal feeding events are created through `POST /api/animals/{animalId}/feedings`, not the generic timeline endpoint. They appear in the associated enclosure timeline with structured food, quantity, result, and animal context projected from the shared ledger.

Animal disposition events are created through `POST /api/animals/{animalId}/dispositions`, not the generic timeline endpoint. They appear in the associated enclosure timeline as the animal leaving active care or reaching end of life.

Medication and treatment events are created through `POST /api/animals/{animalId}/medications` and `POST /api/animals/{animalId}/treatments`, not the generic timeline endpoint. They appear in enclosure timelines as high-level medical activity for the animal while preserving detailed medical fields in the dedicated animal medical APIs.

Enclosure cleaning events are created through `POST /api/enclosures/{enclosureId}/cleanings`, not the generic timeline endpoint. They appear in enclosure timelines with structured cleaning titles such as `Full cleaning completed`, `25% water change completed`, or `Deep cleaning completed - substrate replaced`.

## Metadata

Metadata stores event-specific values as PostgreSQL `jsonb`.

Metadata should only contain values that vary by event type.

Examples include:

Water Test

```json
{
  "temperature": 78.4,
  "ph": 7.2,
  "ammonia": 0,
  "nitrite": 0,
  "nitrate": 10,
  "salinity": null,
  "unitSystem": "Imperial"
}
```

Feeding

```json
{
  "food": "Frozen mouse",
  "quantity": 1,
  "unit": "Item",
  "feedingResult": "Accepted"
}
```

Task

```json
{
  "taskName": "Replace UVB bulb",
  "taskStatus": "Completed",
  "scheduledFor": "2026-07-18T10:00:00Z"
}
```

Metadata should not duplicate common fields such as title, occurrence time, event type, or enclosure identity.

## API Design

Timeline API routes are nested under enclosures because timeline events are scoped to a single enclosure.

Initial routes:

* `GET /api/enclosures/{id}/timeline`
* `GET /api/enclosures/{id}/timeline/{eventId}`
* `POST /api/enclosures/{id}/timeline`
* `PUT /api/enclosures/{id}/timeline/{eventId}`
* `DELETE /api/enclosures/{id}/timeline/{eventId}`

List responses are ordered newest first by `OccurredAt`.

Single-event, update, and delete operations query by both route identifiers:

* Event Id
* Enclosure Id

This prevents events from being accessed or changed through the wrong enclosure route.

## Create Behavior

POST creates a new timeline event.

The request may set:

* Event type
* Occurred date
* Title
* Description
* Performed by
* Source reference
* Source type
* Metadata

Structured event types such as animal movement, animal feeding, animal disposition, medication, treatment, and enclosure cleaning are blocked from the generic timeline create/update/delete operations. Those categories use dedicated endpoints so required detail rows cannot be skipped.

The server sets:

* Id
* EnclosureId
* CreatedAt
* UpdatedAt

Creating an event for a missing enclosure returns `400 Bad Request`.

## Update Behavior

PUT updates an existing timeline event in place.

The request may update:

* Event type
* Occurred date
* Title
* Description
* Performed by
* Metadata

The API does not allow changing:

* Id
* EnclosureId
* CreatedAt

UpdatedAt is always set by the server.

If the event does not exist, or exists under a different enclosure, the API returns `404 Not Found`.

## Delete Behavior

Timeline events are hard deleted in the current implementation.

This is acceptable while the system is early and users may need to remove accidental or malformed entries.

Future audit requirements may replace hard deletion with:

* Soft deletion
* Correction records
* Event reversal records
* Immutable append-only audit trails

## Indexing

The timeline table is indexed for common enclosure history queries.

Indexes include:

* EnclosureId
* OccurredAt
* EventType
* EnclosureId and OccurredAt

The combined enclosure and occurrence index supports chronological queries for a single enclosure.

## Future Extensions

The timeline is intended to become the shared historical surface for enclosure operations.

Potential future additions include:

* Animal-specific feeding references
* User account relationships for PerformedBy
* Source entity integrations for tasks, feedings, and water tests
* Soft deletion or correction records
* Event-type-specific validation
* Timeline filtering by date range or event type
