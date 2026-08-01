# Enclosure Timeline Domain Design

## Purpose

The Enclosure Timeline records historical events associated with an enclosure.

It provides a shared operational history for activities such as water tests, cleanings, feedings, and tasks without adding many specialized columns to the Enclosure entity.

The design goal is to create a flexible timeline foundation that supports current enclosure operations while allowing new event types to be added later.

## Design Philosophy

Timeline events are modeled as first-class records with strongly typed common fields and flexible metadata for event-specific values.

Common timeline information is stored relationally.

Event-specific details are stored in a PostgreSQL `jsonb` metadata field.

This avoids two problems:

* A very wide timeline table with many nullable columns
* A timeline stored entirely as unstructured JSON

The timeline should describe what happened, when it happened, who performed it, and any contextual details needed for display or reporting.

## Core Timeline Event Entity

Every timeline event belongs to exactly one enclosure.

Field Purpose
Id Bigint identity primary key
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

The current Enclosure primary key is an integer, so `EnclosureId` uses the existing integer key to preserve a real foreign key relationship.

## Enclosure Relationship

Every timeline event belongs to one enclosure.

Relationship:

Enclosure (1)
        │
        │
        └───────────< EnclosureTimelineEvent (Many)

An enclosure may have many timeline events.

A timeline event may not exist without an enclosure.

Deleting an enclosure is restricted while timeline events exist. This protects operational history from being removed accidentally.

## Event Types

EventType is represented as an enum and stored as a readable string in PostgreSQL.

Initial values include:

* WaterTest
* Cleaning
* Feeding
* Task
* Other

The Other value exists for events that do not yet have a dedicated type.

Future event types can be added without changing the common timeline shape.

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

Cleaning

```json
{
  "cleaningType": "Partial",
  "waterChangePercent": 25,
  "substrateChanged": false,
  "equipmentCleaned": ["Filter", "Glass"]
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
