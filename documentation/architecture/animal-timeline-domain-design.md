# Animal Timeline Domain Design

## Purpose

The Animal Timeline returns historical events associated with a single animal.

It provides one chronological history surface for animal-specific activity such as feedings, treatments, notes, and tasks.

The design goal is to preserve animal history even when the animal moves between enclosures. Animal timeline API routes remain animal-scoped, but the underlying source of truth is the shared Activity Ledger described in `activity-ledger-domain-design.md`.

## Design Philosophy

The animal timeline follows the same API architecture as the enclosure timeline.

Common timeline information is stored on `ActivityEvent`.

Event-specific details are stored in a PostgreSQL `jsonb` metadata field.

This keeps the table understandable while avoiding nullable columns for every possible event detail.

The timeline should describe what happened, when it happened, where it happened, who performed it, and any event-specific details needed for display or reporting.

## Core Timeline Event View

Every animal timeline response is projected from one `ActivityEvent` related through `ActivityEventAnimals`.

Field Purpose
Id Shared ActivityEvents bigint identity primary key
AnimalId Requested animal related through ActivityEventAnimals
EnclosureId Enclosure context related through ActivityEventEnclosures
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

Animal records currently require an enclosure, so animal timeline create and update requests still require `EnclosureId`. The service stores that value as a primary enclosure association.

## Animal Relationship

Every animal timeline event must be associated with the requested animal.

Relationship:

Animal (1)
        │
        │
        └───────────< ActivityEventAnimal >─────────── ActivityEvent

An animal may have many activity events.

Deleting an animal is restricted while activity event associations exist so historical data is not silently removed.

## Enclosure Context

Animal timeline events expose `EnclosureId` from an activity enclosure association.

This is intentionally separate from the animal's current enclosure assignment.

If an animal moves from one enclosure to another, older timeline events continue to reference the original enclosure context.

Relationship:

Enclosure (1)
        │
        │
        └───────────< ActivityEventEnclosure >──────── ActivityEvent

Deleting an enclosure is restricted while activity event associations reference it.

## Event Types

EventType is represented as an enum and stored as a readable string in PostgreSQL.

Initial values include:

* Feeding
* AnimalMovement
* AnimalDisposition
* Medication
* Treatment
* Note
* Task
* Other

Animal movement events are created through `POST /api/animals/{animalId}/movements`, not the generic timeline endpoint. They appear in the animal timeline as movement activity projected from the shared ledger.

Animal feeding events are created through `POST /api/animals/{animalId}/feedings`, not the generic timeline endpoint. They appear in the animal timeline with structured food, quantity, and result details projected from the shared ledger.

Animal disposition events are created through `POST /api/animals/{animalId}/dispositions`, not the generic timeline endpoint. They appear in the animal timeline with disposition type, destination, reason, and notes projected from the shared ledger.

Medication and treatment events are created through `POST /api/animals/{animalId}/medications` and `POST /api/animals/{animalId}/treatments`, not the generic timeline endpoint. They appear in the animal timeline with structured medical detail projected from the shared ledger.

The Other value exists for events that do not yet have a dedicated type.

## Metadata

Metadata stores event-specific values as PostgreSQL `jsonb`.

Metadata should only contain values that vary by event type.

Examples include:

Feeding

```json
{
  "food": "Frozen mouse",
  "quantity": 1,
  "unit": "Item",
  "feedingResult": "Accepted"
}
```

Treatment

```json
{
  "treatmentName": "Topical antiseptic",
  "dose": "Small amount",
  "reason": "Minor scrape",
  "followUpNeeded": true
}
```

Note

```json
{
  "category": "Behavior",
  "visibility": "General"
}
```

Task

```json
{
  "taskName": "Weigh animal",
  "taskStatus": "Completed",
  "scheduledFor": "2026-08-02T13:00:00Z"
}
```

Metadata should not duplicate common fields such as title, occurrence time, event type, animal identity, or enclosure identity.

## API Design

Timeline API routes are nested under animals because timeline events are scoped to a single animal.

Routes:

* `GET /api/animals/{animalId}/timeline`
* `GET /api/animals/{animalId}/timeline/{eventId}`
* `POST /api/animals/{animalId}/timeline`
* `PUT /api/animals/{animalId}/timeline/{eventId}`
* `DELETE /api/animals/{animalId}/timeline/{eventId}`

List responses are ordered newest first by `OccurredAt`.

When occurrence times are equal, records are ordered by descending timeline event ID so the ordering is deterministic.

Single-event, update, and delete operations query by both route identifiers:

* Animal Id
* Event Id

This prevents events from being accessed or changed through the wrong animal route.

## Create Behavior

POST creates a new timeline event.

The request may set:

* Enclosure Id
* Event type
* Occurred date
* Title
* Description
* Performed by
* Source reference
* Source type
* Metadata

The server sets:

* Id
* AnimalId
* CreatedAt
* UpdatedAt

Creating an event for a missing animal returns `404 Not Found`.

Creating an event for a missing enclosure returns `404 Not Found`.

## Update Behavior

PUT updates an existing timeline event in place.

The request may update:

* Enclosure Id
* Event type
* Occurred date
* Title
* Description
* Performed by
* Metadata

The API does not allow changing:

* Id
* AnimalId
* CreatedAt

UpdatedAt is always set by the server.

If the event does not exist, or exists under a different animal, the API returns `404 Not Found`.

If the updated enclosure does not exist, the API returns `404 Not Found`.

## Delete Behavior

Timeline events are hard deleted in the current implementation.

This matches the current enclosure timeline behavior and is acceptable while the system is early.

Future audit requirements may replace hard deletion with:

* Soft deletion
* Correction records
* Event reversal records
* Immutable append-only audit trails

## Indexing

The timeline table is indexed for common animal history queries.

Indexes include:

* AnimalId
* EnclosureId
* OccurredAt
* EventType
* AnimalId, OccurredAt, and Id

The combined animal, occurrence, and event ID index supports chronological animal timeline queries with deterministic ordering.

## Future Extensions

Potential future additions include:

* Dedicated feeding records linked through SourceReferenceId
* Dedicated treatment or medical records linked through SourceReferenceId
* User account relationships for PerformedBy
* Soft deletion or correction records
* Event-type-specific validation
* Timeline filtering by date range, event type, or enclosure
