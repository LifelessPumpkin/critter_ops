# Animal Domain Design

## Purpose

The Animal entity represents an individual living animal managed within CritterOps.

Every animal belongs to an Enclosure and serves as the central entity that connects the rest of the system, including feeding schedules, medical records, breeding history, weight tracking, maintenance activities, and inventory.

The objective is to create a stable core identity model that remains relatively unchanged while allowing operational features to evolve independently.

⸻

## Design Philosophy

The Animal entity is intentionally limited to information that defines the identity and current state of an animal.

Rather than storing every possible piece of information directly on the table, CritterOps follows a composition-based design where specialized information is stored in related entities.

Examples include:

* Medical history
* Feeding schedules
* Weight history
* Breeding records
* Environmental observations
* Behavioral logs

This keeps the primary animal record small, understandable, and maintainable.

⸻

## Core Identity

Every animal has a stable identity consisting of:

Field Purpose
Id Primary key
Name Display name
Species Biological species
SubspeciesOrMorph Morph, locality, or subspecies information
AnimalType Broad biological grouping
Sex Biological sex
Status Current operational status
Notes General descriptive information

These fields describe what the animal is, not what has happened to it.

⸻

## Enclosure Relationship

Every animal belongs to exactly one enclosure.

Relationship:

Enclosure (1)
        │
        │
        └───────────< Animal (Many)

An enclosure may contain many animals.

An animal always has one assigned enclosure.

Future transfers between enclosures should be recorded using movement history rather than overwriting historical information.

⸻

## Species

Species is intentionally stored as descriptive text.

Examples:

* Ball Python
* Red Kangaroo
* Leopard Gecko
* Axolotl
* Domestic Rabbit

Using free text avoids requiring software updates whenever a new species is introduced.

Future taxonomy support may normalize species into dedicated reference tables.

⸻

## Morph / Subspecies

Many animals require additional identification beyond species.

Examples include:

* Pastel
* Albino
* Super Mojave
* Western
* High Red

This field supports:

* Reptile morphs
* Bird mutations
* Fish strains
* Mammal subspecies

⸻

## Animal Type

AnimalType provides broad categorization used throughout the application.

Initial categories include:

* Mammal
* Avian
* Reptile
* Amphibian
* Fish
* Invertebrate
* Herptile
* Aquatic
* Other

This field is intended primarily for filtering, reporting, and future feature specialization.

⸻

## Animal Status

Status represents the current operational state of an animal.

Examples include:

* Active
* Quarantined
* Medical
* Breeding
* On Hold
* Sold
* Transferred
* Released
* Deceased
* Inactive

Status is operational and may change throughout the animal’s lifetime.

⸻

## Biological Sex

Sex is represented separately from breeding capability.

Initial values include:

* Male
* Female
* Unknown
* Hermaphrodite
* Mixed
* Not Applicable

Keeping this standardized simplifies breeding management and reporting.

⸻

## Life Cycle Information

The animal stores only significant lifecycle dates.

Birth Date

Birth date may be unknown.

When estimated, the system stores:

* BirthDate
* BirthDateIsEstimated

This prevents inaccurate assumptions while still supporting approximate age calculations.

⸻

## Acquisition

Every animal has an acquisition date representing when it entered the collection.

The source itself is stored separately.

⸻

## Disposition

Disposition tracks when an animal permanently leaves the collection.

Examples include:

* Sale
* Donation
* Transfer
* Release
* Death

Historical operational data should remain available even after disposition.

Animals should generally not be physically deleted from the database.

⸻

## Identification

Many species have permanent identifiers.

Examples include:

* RFID microchips
* Leg bands
* Ear tags
* Livestock identification
* Reptile tags

The base model supports:

* MicrochipNumber
* TagIdentifier

These are optional because many species will never receive permanent identification.

⸻

## Source Information

Source describes where the animal originated.

Examples include:

* Breeder
* Pet Store
* Rescue
* Zoo
* Donation
* Internal Breeding
* Wild Collection (where legally appropriate)

Additional vendor information should eventually be normalized into dedicated entities.

⸻

## Notes

Notes are intentionally unstructured.

Examples include:

* Personality observations
* General care notes
* Identification reminders
* Miscellaneous operational information

Structured operational data should not be stored in Notes if a dedicated feature exists.

⸻

## Audit Fields

The base entity records:

* CreatedAt
* UpdatedAt

These timestamps provide basic auditing while avoiding unnecessary complexity.

⸻

Future Architecture

The following capabilities should be implemented as separate entities rather than additional nullable columns.

⸻

## Medical

Future entities:

* AnimalMedicalRecord
* AnimalTreatment
* AnimalMedication
* AnimalVetVisit
* AnimalMedicalAlert

Medical history is operational data, not identity data.

⸻

## Feeding

Future entities:

* DietPlan
* FeedingSchedule
* FeedingEvent
* FeedingHistory

Feeding changes frequently and should not be stored directly on the animal record.

⸻

## Physical Measurements

Future entities:

* AnimalWeightLog
* AnimalLengthLog
* AnimalGrowthRecord
* BodyConditionRecord

Measurements are time-series data and should preserve historical values.

⸻

## Breeding

Future entities:

* AnimalPairing
* Clutch
* Litter
* IncubationRecord
* HatchRecord
* PregnancyRecord

Parental relationships such as Sire and Dam may eventually be introduced through breeding-specific entities rather than tightly coupling them to the base animal model.

⸻

## Genetics

Future entities:

* GeneticLine
* MorphGene
* TraitInheritance
* GeneticTest

These systems are expected to become complex enough to warrant dedicated models.

⸻

## Movement History

Rather than overwriting enclosure assignments, future movement should be recorded.

Example:

AnimalMovement

--------------
AnimalId
FromEnclosureId
ToEnclosureId
MovedDate
Reason
MovedBy

This preserves a complete history of enclosure transfers.

⸻

## Revision History

Future entity:

* AnimalRevisionHistory

Revision history enables:

* Audit trails
* Historical comparisons
* Restore previous values
* Compliance reporting

The base entity should only store the latest state.

⸻

## Entity Relationships (Future)

Animal
├── Enclosure (Many:1)
├── FeedingSchedules (1:N)
├── FeedingEvents (1:N)
├── MedicalRecords (1:N)
├── Medications (1:N)
├── VetVisits (1:N)
├── WeightLogs (1:N)
├── Measurements (1:N)
├── AnimalMovements (1:N)
├── RevisionHistory (1:N)
├── Photos (1:N)
├── Documents (1:N)
├── Pairings (1:N)
├── Offspring (1:N)
└── Genetics (1:1)

⸻

## Guiding Principles

1. Every animal has a single, stable identity.
2. Identity information should remain separate from operational records.
3. Historical information should be preserved rather than overwritten.
4. Time-series data belongs in dedicated log tables.
5. Avoid large tables with many nullable columns by using related entities.
6. Design for extensibility so new animal groups and features can be introduced without disruptive schema changes.
7. Treat the Animal entity as the central domain object that connects the rest of the CritterOps ecosystem while remaining focused on identity rather than operations.
