# Enclosure Domain Design

## Purpose

The Enclosure entity represents a physical habitat that houses one or more animals. It serves as the foundational object for all enclosure types within CritterOps, regardless of species or environmental requirements.

The design goal is to establish a common base model that can be extended over time without requiring frequent schema changes or introducing nullable columns for species-specific functionality.

## Design Philosophy

The enclosure model follows a layered approach.

* The base Enclosure entity contains only fields that are applicable to nearly every enclosure.
* Species-specific requirements are modeled in separate related entities.
* Operational history (cleaning, maintenance, inspections, etc.) is stored in dedicated log tables rather than on the base entity.
* Environmental monitoring and smart device integration are independent systems related back to an enclosure.

This approach keeps the core entity small, understandable, and extensible.

## Base Enclosure Entity

The base entity stores general information shared by all enclosure types.

Field Purpose
Id Primary key
Name Human-readable enclosure name
Type Broad enclosure category
Location Physical location of the enclosure
SizeLabel Human-readable size (e.g. “40 Gallon Breeder”)
Length Physical length
Width Physical width
Height Physical height
DimensionUnit Unit for dimensions
Volume Capacity where applicable
VolumeUnit Gallons, liters, etc.
Material Primary enclosure construction material
MaxAnimalCapacity Intended maximum occupancy
Mobility Whether the enclosure is fixed or movable
Status Current operational state
SafetyRating General containment/security rating
Notes Free-form notes
CreatedDate Record creation timestamp
UpdatedDate Last modification timestamp

## Enclosure Types

The enclosure type is represented as an enum.

Initial supported values include:

* Aquarium
* Pond
* Terrarium
* Vivarium
* Paludarium
* Aviary
* Kennel
* Cage
* Crate
* Hutch
* Beehive
* Stable
* Tub
* Rack
* OutdoorRun
* Other

The Other value exists to support future enclosure types before dedicated enum values are introduced.

## Location Design

Location is intentionally stored as descriptive text rather than an enum.

Examples:

* Living Room
* Fish Room
* Reptile Room
* Garage
* Shed
* Backyard
* Greenhouse
* Veterinary Room

This allows facilities of different sizes to organize enclosures without requiring application updates whenever a new room is created.

A future enhancement may normalize locations into a dedicated Location entity if organizational requirements become more complex.

## Dimensions and Size

Two different concepts are stored.

Human-readable Size

Examples:

* 10 Gallon
* 40 Gallon Breeder
* XXL Dog Crate
* 1000 Gallon Pond

This exists primarily for display.

Physical Dimensions

Dimensions are stored separately.

* Length
* Width
* Height
* DimensionUnit

This supports calculations, validation, and future automation.

## Volume

Volume is stored independently from dimensions because many aquatic enclosures are identified primarily by capacity.

Examples:

* 5 Gallons
* 75 Gallons
* 400 Liters

## Material

Material represents the primary construction material.

Examples include:

* Glass
* Acrylic
* Wire
* Plastic
* PVC
* Wood
* Mesh
* Concrete
* Fabric

Species-specific structural information belongs in specialized entities.

## Status

Status represents the operational state of an enclosure.

Examples include:

* Active
* Empty
* Inactive
* Under Maintenance
* Cleaning Required
* Quarantine
* Reserved
* Retired

This allows planning, scheduling, and reporting without modifying the enclosure itself.

## Mobility

Mobility indicates whether an enclosure can be relocated.

Examples include:

* Fixed
* Movable
* Portable
* Temporary
* Outdoor Permanent

## Safety Rating

Safety Rating provides a generalized assessment of enclosure security.

Potential values include:

* Low
* Medium
* High
* Critical

Species-specific safety concerns belong in specialized enclosure detail entities.

## Species-Specific Design

The base enclosure intentionally avoids storing fields that only apply to one category of animal.

Instead, future one-to-one extension entities will be introduced.

Mammal Enclosure Details

Examples include:

* Lock type
* Bar spacing
* Floor type
* Anti-dig boundary
* Chew-proof rating
* Roof or cover type

## Avian Enclosure Details

Examples include:

* Orientation
* Wire gauge
* Perch capacity
* Feeder access
* Dropping tray system

## Herptile Enclosure Details

Examples include:

* Enclosure style
* Substrate depth
* Ventilation style
* Water integration
* Heating zones
* UVB penetration rating

## Aquatic Enclosure Details

Examples include:

* Water type
* Rim type
* Dry weight
* Filled weight
* Plumbing cutouts
* Water capacity

## Environmental Monitoring

Environmental targets are intentionally modeled separately.

Future entities may include:

* Target temperature range
* Target humidity range
* Lighting schedule
* Sensor integrations
* Smart devices
* Automated equipment

Keeping these outside the enclosure entity prevents the base model from becoming coupled to automation features.

## Maintenance History

Operational events should not overwrite the enclosure record.

Instead, they should be stored in historical tables.

Examples include:

* EnclosureCleaningLog
* EnclosureMaintenanceLog
* EnclosureInspectionLog
* EnclosureRevisionHistory

This preserves a complete audit trail while keeping the current enclosure record focused on its present state.

## Audit Strategy

The base entity records only:

* CreatedDate
* UpdatedDate

Long-term historical changes should be captured through a dedicated revision history system rather than by storing previous values directly on the enclosure.

This enables future features such as:

* Viewing historical changes
* Restoring previous configurations
* Compliance reporting
* Activity timelines

## Entity Relationships (Future)

Enclosure
├── Animals (1:N)
├── MammalEnclosureDetails (1:1)
├── AvianEnclosureDetails (1:1)
├── HerptileEnclosureDetails (1:1)
├── AquaticEnclosureDetails (1:1)
├── EnvironmentalTargets (1:1)
├── Sensors (1:N)
├── MaintenanceLogs (1:N)
├── CleaningLogs (1:N)
├── InspectionLogs (1:N)
└── RevisionHistory (1:N)

## Guiding Principles

1. Keep the base Enclosure entity small and broadly applicable.
2. Prefer composition through related entities over a single wide table with many nullable columns.
3. Preserve historical data in dedicated log tables instead of overwriting records.
4. Design for extensibility so new enclosure types and animal groups can be added without disruptive schema changes.
5. Favor clear, descriptive domain models that closely reflect real-world animal care operations.
