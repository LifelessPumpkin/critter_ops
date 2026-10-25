# Ticket 2 implementation report

## Outcome

Implemented deterministic legacy reference classification, exact original/custom preservation, explicit create/update contracts, reference-safe Gilligan edits, diagnostics and PostgreSQL regression tests. No production migration was executed. Ticket 2 remains partially incomplete for destructive cleanup: custom/original text and ambiguous operational groups cannot safely be removed from the current physical model. No legacy Animal columns were dropped.

The complete inventory, matching rules, migration sequence, final API/storage model, rollback limitations and Tickets 3–8 implications are in [the architecture document](architecture/legacy-animal-reference-migration.md). The read-only [validation SQL](operations/validate-animal-reference-migration.sql) provides the deployment-time checks.

## Files created

- `apps/skipper-api/Data/Migrations/20261017210000_MigrateLegacyAnimalReferences.cs`
- `tests/skipper/Integration/LegacyAnimalReferenceMigrationTests.cs`
- `documentation/architecture/legacy-animal-reference-migration.md`
- `documentation/operations/validate-animal-reference-migration.sql`
- `documentation/animal-reference-migration-report.md`

## Files modified

- `apps/skipper-api/Domain/Animals/Animal.cs`
- `apps/skipper-api/Dtos/Animals/CreateAnimalRequestDto.cs`
- `apps/skipper-api/Dtos/Animals/UpdateAnimalRequestDto.cs`
- `apps/skipper-api/Dtos/Animals/AnimalResponseDto.cs`
- `apps/skipper-api/Dtos/Animals/AnimalListItemDto.cs`
- `apps/skipper-api/Services/Animals/AnimalService.cs`
- `tests/skipper/Unit/AnimalReferenceValidationTests.cs`
- `tests/skipper/Integration/AnimalCatalogIntegrationTests.cs`
- `tests/skipper/Integration/CatalogMetadataIntegrationTests.cs` — pins Ticket 1 rollback coverage to Ticket 1's migration rather than applying future data migrations.
- `apps/gilligan-web/src/lib/api/animals.ts`
- `apps/gilligan-web/src/components/animals/AnimalForm.tsx`
- `apps/gilligan-web/src/components/animals/AnimalReferenceFields.tsx`
- `apps/gilligan-web/src/components/animals/AnimalCard.tsx`
- `apps/gilligan-web/src/components/activity/EntityActivityPage.tsx`
- `apps/gilligan-web/src/components/animals/AnimalForm.test.tsx`
- `apps/gilligan-web/src/components/animals/AnimalReferenceFields.test.tsx`
- `apps/gilligan-web/src/components/animals/AnimalCard.test.tsx`

## Inventory and final model

| Existing field | Final role | Migration | Cleanup status |
| --- | --- | --- | --- |
| Species | Permanent custom species when SpeciesId absent; original description otherwise | Unique normalized scientific/common/alias match populates SpeciesId; exact original retained | Retained; not disposable compatibility-only storage |
| SubspeciesOrMorph | Permanent custom variant when AnimalVariantId absent; original description otherwise | Unique normalized match within selected species populates AnimalVariantId | Retained; not disposable compatibility-only storage |
| AnimalType | Original operational category | Unique exact catalog group mapping populates AnimalGroupId | Retained for Herptile/Aquatic/other unmappable information and filters |
| AnimalGroupId / SpeciesId / AnimalVariantId | Structured relationships | Existing IDs preserved; discrepancies diagnosed | Kept; existing single-variant model unchanged |

No additional Animal custom columns duplicate existing storage. Structured IDs are authoritative. Custom values remain permanently supported. Read models expose both resolved display values and original descriptions.

## Migration results — tests only

The main populated PostgreSQL fixture contains ten animals:

| Diagnostic | Result |
| --- | ---: |
| Animals inspected | 10 |
| Newly structured species | 4 |
| Custom species preserved, including ambiguity | 3 |
| Ambiguous species | 1 |
| Newly structured variants | 1 |
| Custom variants preserved, including ambiguity | 4 |
| Ambiguous variants | 1 |
| Group mappings | 8 |
| Records requiring investigation | 5 |

Two species references and two variant references were already present; blank species/variants are not counted as custom preservations. The five investigation records cover ambiguous operational groups, conflicting original text, a cross-species variant and an incompatible scientific-name/group candidate. Their original data remains intact. These are deliberately unresolved test conflicts, not production findings.

Additional PostgreSQL fixtures cover an empty/fresh database, seeded catalogs, repeat migration/SQL execution, metadata aliases/common names, null/empty fields, duplicate scientific names, invalid historical IDs, unknown operational-group strings, exact custom whitespace, variant removal, preservation during unrelated edits and refusal of unsafe populated rollback.

Production totals: **NOT RUN / unknown** for every diagnostic. No claims are made about actual collection animals.

## Validation and database changes

The migration updates only reference-ID columns, preserving Animal IDs, all unrelated columns, enclosure IDs and original text. The populated test compares all original Animal columns other than the three intentionally classified FK columns, checks the exact ID set, checks retained original reference IDs, and verifies readable API projections. No animals or dependent relationships are recreated. Database FK enforcement remains unchanged. Pre-existing broken catalog IDs are preserved and flagged rather than repaired by guessing.

Added database objects: `AnimalReferenceMigrationAudit` table and `AnimalReferenceMigrationSummary` view. No Animal columns, indexes or constraints are added, removed or changed. The transient `pg_temp` normalization function exists only for migration execution. EF model mappings and snapshot need no schema changes; pending-model tests pass.

The audit retains original reference text and IDs, outcomes and issues. Preservation failures raise and roll back. Classification validity requires no investigation issues. Classification validity is not column-removal readiness: custom storage remains in use. `Down` can remove empty diagnostics but refuses rollback once animals have been audited; recovery requires a reviewed forward correction or verified backup restore.

## API and frontend changes

Create/update use `customSpeciesName` and `customVariantName` alongside the existing structured IDs. Obsolete request `species`/`subspeciesOrMorph` fields are removed and rejected as unmapped JSON. Existing read fields remain derived display fields. Responses additionally expose custom and original text. Structured/custom conflicts, invalid IDs, wrong-species variants and incompatible groups are rejected.

Omitted update references preserve all existing reference data. Gilligan omits them for unrelated edits, preserving incomplete and conflicting migrated records. Explicit nulls allow variant removal. Add Animal sends custom text only when no corresponding catalog selection exists. The draft/enclosure workflow remains compatible. Group changes preserve names as custom values and invalidate incompatible IDs; species changes preserve variant text while invalidating its ID. Both changes show review notices. Card/detail displays expose scientific names and differing historical descriptions.

## Executed verification

| Check | Result |
| --- | --- |
| Backend build | PASS — `dotnet build apps/skipper-api --no-restore`; subsequent test runs compiled final backend changes |
| Backend unit tests | PASS — 13 tests |
| PostgreSQL integration tests | PASS — 19 tests using disposable PostgreSQL 16 containers |
| Migration tests | PASS — included in PostgreSQL suite, including three dedicated Ticket 2 tests |
| API regression tests | PASS — service/controller regressions with PostgreSQL, including new contracts, custom data, edits, filters and invalid references |
| Full HTTP transport end-to-end tests | NOT RUN |
| Frontend production build / TypeScript | PASS — `npm run build` |
| Frontend automated tests | PASS — 25 tests across 4 files |
| Manual browser testing | NOT RUN |
| Production migration / representative real-animal verification | NOT RUN |

Early verification found and corrected a PostgreSQL variable/alias collision, an EF raw-SQL test brace-escaping issue, stale payload assertions, and a compile error during request cleanup. Final results above supersede those intermediate failures. Vitest emits the existing configuration warning and a jsdom navigation notice; all tests pass.

## Remaining limitations and deployment work

- Retain Species/SubspeciesOrMorph: they implement permanent custom storage and preserve additional historical descriptions. A future physical rename/removal must move that information first.
- Retain AnimalType until ambiguous operational categories have a permanent reviewed representation. Never map Herptile/Aquatic to a guessed biological group.
- No production data inventory or cleanup readiness has been measured. Execute the included validation SQL with writes paused after migration, inspect diagnostics and verify representative animals before reopening writes.
- Malformed historical schemas can contain null required fields, undefined enum strings or invalid FKs. Migration tests show preservation and issue reporting; those records require manual remediation before normal application reads. The migration does not make corrupt historical schemas conform to the current EF model.
- No production-scale performance benchmark or browser/manual workflow verification was performed. Migration locks animal/catalog writes for the transaction; plan the migration window against a representative restored database.
- Rollback of populated migration is deliberately blocked. No automatic backup or destructive production action was performed.
- No multiple-variant assignments, catalog import/sync, worker or deployment changes were implemented. Tickets 3–8 should preserve this custom/original-data contract; detailed dependencies are in the architecture document.

No Git inspection commands, pushes, commits or pull-request description were produced for this task.
