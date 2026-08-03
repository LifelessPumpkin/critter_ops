# Animal Timeline Domain Design

## Purpose

The Animal Timeline records historical events associated with a single animal.

It provides one chronological history surface for animal-specific activity such as feedings, treatments, notes, and tasks.

The design goal is to preserve animal history even when the animal moves between enclosures. Each timeline event therefore stores both the animal and the enclosure context associated with the event at the time it occurred.

## Design Philosophy

The animal timeline follows the same architecture as the enclosure timeline.

Common timeline information is stored relationally.

Event-specific details are stored in a PostgreSQL `jsonb` metadata field.

This keeps the table understandable while avoiding nullable columns for every possible event detail.

The timeline should describe what happened, when it happened, where it happened, who performed it, and any event-specific details needed for display or reporting.

## Core Timeline Event Entity

Every animal timeline event belongs to exactly one animal and stores one enclosure context.

Field Purpose
Id Bigint identity primary key
AnimalId Required foreign key to Animals.Id
EnclosureId Required foreign key to Enclosures.Id
EventType Type of timeline event
OccurredAt When the activity actually occurred
Title Short human-readable description
Description Optional notes or context
PerformedBy Optional human-readable actor name
SourceReferenceId Optional reference to an originating source record
SourceType Optional source entity type name
Metadata Optional event-specific JSON values
CreatedAt Record creation timestamp
UpdatedAt Last modification timestamp

Timeline event identifiers use an incremental `bigint` value. This keeps event URLs compact while leaving room for a large number of future events.

Animal records currently require an enclosure, so animal timeline events also require `EnclosureId`.

## Animal Relationship

Every timeline event belongs to one animal.

Relationship:

Animal (1)
        │
        │
        └───────────< AnimalTimelineEvent (Many)

An animal may have many timeline events.

A timeline event may not exist without an animal.

Deleting an animal is restricted while timeline events exist so historical data is not silently removed.

## Enclosure Context

Animal timeline events store `EnclosureId` directly.

This is intentionally separate from the animal's current enclosure assignment.

If an animal moves from one enclosure to another, older timeline events continue to reference the original enclosure context.

Relationship:

Enclosure (1)
        │
        │
        └───────────< AnimalTimelineEvent (Many)

Deleting an enclosure is restricted while animal timeline events reference it.

## Event Types

EventType is represented as an enum and stored as a readable string in PostgreSQL.

Initial values include:

* Feeding
* Treatment
* Note
* Task
* Other

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
