# Activity Ledger Domain Design

## Purpose

The Activity Ledger is the authoritative historical log for CritterOps.

It stores one record for one real-world activity, then relates that activity to the animals and enclosures involved. Animal and enclosure timeline APIs remain separate API concepts, but both read from the same `ActivityEvents` records.

## Core Entity

`ActivityEvent` stores common historical fields:

Field Purpose
Id Bigint identity primary key
EventType Broad activity category
OccurredAt When the activity actually occurred
Title Short human-readable description
Notes Optional notes or context
PerformedBy Optional human-readable actor name
SourceReferenceId Optional reference to an originating source record
SourceType Optional source entity type name
Metadata Optional event-specific JSON values
CreatedAt Record creation timestamp
UpdatedAt Last modification timestamp

Event identifiers use incremental `bigint` values, consistent with existing timeline conventions.

## Associations

Animals are related through `ActivityEventAnimals`.

Field Purpose
ActivityEventId Foreign key to ActivityEvents.Id
AnimalId Foreign key to Animals.Id
RelationshipType Animal role in the event

Initial animal relationship values are `Primary`, `Affected`, `Parent`, and `Offspring`.

Enclosures are related through `ActivityEventEnclosures`.

Field Purpose
ActivityEventId Foreign key to ActivityEvents.Id
EnclosureId Foreign key to Enclosures.Id
RelationshipType Enclosure role in the event

Initial enclosure relationship values are `Primary`, `Source`, and `Destination`.

This allows one movement event to relate to one animal and multiple enclosures without creating duplicate timeline records.

## Timeline Views

`GET /api/animals/{animalId}/timeline` returns activity events where the requested animal appears in `ActivityEventAnimals`.

`GET /api/enclosures/{enclosureId}/timeline` returns activity events where the requested enclosure appears in `ActivityEventEnclosures`.

Both timeline views remain chronologically ordered by `OccurredAt` descending, then event ID descending for deterministic ordering.

## Specialized Details

The ledger intentionally does not model every future event detail directly on `ActivityEvent`.

Future structured categories can add dedicated tables such as `AnimalMovementEvent`, `FeedingEvent`, `MedicationEvent`, or `WaterTestEvent` that reference the parent `ActivityEvent`.

Until those tables are needed, existing timeline metadata continues to live in the `Metadata` jsonb column.

## Animal Movement

Animal movement is the first structured activity detail implemented on top of the ledger.

`AnimalMovementActivity` has a one-to-one relationship with `ActivityEvent` and uses `ActivityEventId` as its primary key.

Field Purpose
ActivityEventId Shared activity event ID
FromEnclosureId Source enclosure
ToEnclosureId Destination enclosure
Reason Optional bounded movement reason

Movement creation writes one `ActivityEvent` with `EventType = AnimalMovement`, one primary animal association, one source enclosure association, one destination enclosure association, and one `AnimalMovementActivity` detail row.

The same transaction updates `Animal.EnclosureId` to the destination enclosure. The source enclosure must match the animal's current enclosure, and the destination must differ from the current enclosure.

Movement endpoints:

* `POST /api/animals/{animalId}/movements`
* `GET /api/animals/{animalId}/movements`
* `GET /api/animals/{animalId}/movements/{activityId}`
* `PUT /api/animals/{animalId}/movements/{activityId}`
* `DELETE /api/animals/{animalId}/movements/{activityId}`

Movement `PUT` updates descriptive fields only: `OccurredAt`, `Reason`, `Notes`, and `PerformedBy`.

Movement `DELETE` is limited to the animal's latest movement and restores the animal's previous enclosure. Older movement corrections should use a future correction or voiding workflow instead of hard deletion.

## Animal Feeding

Animal feeding is a structured husbandry activity implemented on top of the ledger.

`AnimalFeedingActivity` has a one-to-one relationship with `ActivityEvent` and uses `ActivityEventId` as its primary key.

Field Purpose
ActivityEventId Shared activity event ID
Food Required bounded description of food offered
Quantity Decimal amount offered
Unit Strongly typed quantity unit
Result Strongly typed feeding result

Quantity units are stored as strings and currently include `Item`, `Gram`, `Kilogram`, `Ounce`, `Pound`, `Milliliter`, `Liter`, `Teaspoon`, `Tablespoon`, `Cup`, and `Other`.

Feeding results are stored as strings and currently include `Accepted`, `PartiallyAccepted`, `Refused`, `NotObserved`, and `Other`.

Feeding creation writes one `ActivityEvent` with `EventType = Feeding`, one primary animal association, one primary enclosure association using the animal's current enclosure, and one `AnimalFeedingActivity` detail row.

Feeding endpoints:

* `POST /api/animals/{animalId}/feedings`
* `GET /api/animals/{animalId}/feedings`
* `GET /api/animals/{animalId}/feedings/{activityId}`
* `PUT /api/animals/{animalId}/feedings/{activityId}`
* `DELETE /api/animals/{animalId}/feedings/{activityId}`

Feeding `PUT` updates descriptive and structured feeding fields: `OccurredAt`, `PerformedBy`, `Food`, `Quantity`, `Unit`, `Result`, and `Notes`. It does not change the activity ID, animal association, or enclosure association.

## Animal Disposition

Animal disposition is a structured lifecycle activity implemented on top of the ledger.

`AnimalDispositionActivity` has a one-to-one relationship with `ActivityEvent` and uses `ActivityEventId` as its primary key.

Field Purpose
ActivityEventId Shared activity event ID
DispositionType Strongly typed disposition category
Reason Optional reason for the disposition
RecipientOrDestination Optional recipient, destination, release location, or related party

Disposition types are stored as strings and currently include `Sold`, `Surrendered`, `Transferred`, `Released`, `Deceased`, and `Other`.

Disposition creation writes one `ActivityEvent` with `EventType = AnimalDisposition`, one primary animal association, one primary enclosure association using the animal's current enclosure, and one `AnimalDispositionActivity` detail row.

The same transaction updates the animal's current status, disposition date, and disposition reason. `Other` is intentionally mapped to `Inactive` until a more specific status model is introduced.

Disposition endpoints:

* `POST /api/animals/{animalId}/dispositions`
* `GET /api/animals/{animalId}/dispositions`
* `GET /api/animals/{animalId}/dispositions/{activityId}`
* `PUT /api/animals/{animalId}/dispositions/{activityId}`
* `DELETE /api/animals/{animalId}/dispositions/{activityId}`

Disposition `PUT` updates descriptive fields and keeps animal status/disposition fields synchronized. Disposition `DELETE` is limited to the latest disposition whose status still matches the animal's current state; it restores the animal to `Active`.
