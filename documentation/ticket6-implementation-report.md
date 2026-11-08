# Ticket 6 implementation report

Implemented multiple structured/custom animal variant assignments across Professor, Skipper and Gilligan. No push, PR description, production database migration, genetics functionality, or Ticket 7/8 implementation was performed.

## Files changed

Created:

- `packages/professor-persistence/Domain/Animals/AnimalVariantAssignment.cs`
- `packages/professor-persistence/Data/Configurations/AnimalVariantAssignmentConfiguration.cs`
- `packages/professor-persistence/Data/Migrations/20261107183835_AddAnimalVariantAssignments.cs`
- `packages/professor-persistence/Data/Migrations/20261107183835_AddAnimalVariantAssignments.Designer.cs`
- `apps/skipper-api/Dtos/Animals/VariantAssignmentDto.cs`
- `tests/skipper/Integration/AnimalVariantAssignmentTests.cs`
- `apps/gilligan-web/src/components/animals/AnimalVariants.test.tsx`
- `documentation/animal-variant-assignments.md`
- `documentation/ticket6-implementation-reporta.md`

Modified:

- `README.md`
- `packages/professor-persistence/Domain/Animals/Animal.cs`
- `packages/professor-persistence/Data/ProfessorDbContext.cs`
- `packages/professor-persistence/Data/Migrations/ProfessorDbContextModelSnapshot.cs`
- `apps/skipper-api/Dtos/Animals/CreateAnimalRequestDto.cs`
- `apps/skipper-api/Dtos/Animals/UpdateAnimalRequestDto.cs`
- `apps/skipper-api/Dtos/Animals/AnimalResponseDto.cs`
- `apps/skipper-api/Dtos/Animals/AnimalListItemDto.cs`
- `apps/skipper-api/Services/Animals/AnimalService.cs`
- `apps/gilligan-web/src/lib/api/animals.ts`
- `apps/gilligan-web/src/components/animals/AnimalReferenceFields.tsx`
- `apps/gilligan-web/src/components/animals/AnimalForm.tsx`
- `apps/gilligan-web/src/components/animals/animalDraft.ts`
- `apps/gilligan-web/src/components/animals/AnimalCard.tsx`
- `apps/gilligan-web/src/components/animals/AnimalTable.tsx`
- `apps/gilligan-web/src/components/animals/AnimalReferenceFields.test.tsx`
- `apps/gilligan-web/src/components/animals/AnimalForm.test.tsx`
- `apps/gilligan-web/src/components/animals/AnimalCard.test.tsx`

## Data model

Animals own assignment rows with independent generated IDs, order and timestamps. Exactly one catalog reference or custom value is required. Structured types derive from the catalog; custom types default to species preference or Other. PostgreSQL enforces exclusive values, allowed custom types, and unique structured IDs/custom normalized-name-and-type combinations. Custom comparison trims/collapses whitespace and ignores case; display text is preserved. Animal deletion cascades assignments; catalog deletion is restricted.

## Migration

The migration copies existing structured references and meaningful custom text, preserving distinct mixed values as separate assignments. Matching catalog/text representations are represented once while exact original text remains preserved. A full-row audit snapshot and executable SQL checks verify identity, animal counts/data and value coverage. Invalid references, incompatible species, and distinct mixed text are reported in the audit.

All legacy variant columns remain. Populated rollback is blocked to prevent data loss. Tests exercised empty/fresh and populated databases, reruns, mixed/duplicate representations, preservation, rollback safeguards and legacy invalid-reference fixtures. Deployment-specific conflicts remain unknown because no production database was inspected or migrated.

## Skipper API

Create/update accept assignment collections; all read surfaces return display metadata, identity, type and order. Omitted/null update collections preserve assignments; an explicit empty array clears them. Unchanged natural keys retain IDs, and optional assignment IDs support custom edits. Validation runs before mutation and one SaveChanges transaction persists the aggregate. Species compatibility, duplicates, invalid/foreign IDs, empty/both/neither values and inactive new selections are rejected.

Deprecated single-variant API adapters remain for existing clients; assignments are authoritative and new writes leave legacy columns empty. Gilligan uses collection payloads. Existing filtering/pagination remains unchanged; no variant filter existed. Catalog endpoints and Howell Sync behavior are unchanged.

## Gilligan UI

Shared Add/Edit form appends structured/custom selections, displays removable assignments, edits custom text inline and retains unchanged assignment IDs. Species/group changes preserve selections and explain incompatibility; users must explicitly resolve them. Adaptive labels and mixed-type headings remain. Detail shows all values; lists show two names plus an overflow count. Drafts retain mixed collections across enclosure creation and upgrade old single-selection drafts. Existing Ginger controls are reused.

## Actual verification

| Check | Result | Evidence |
| --- | --- | --- |
| Professor migration | PASS | PostgreSQL integration suite: fresh/populated migration, repeatable backfill, whole-animal preservation and rollback guards |
| PostgreSQL constraint tests | PASS | Real PostgreSQL 16 via Testcontainers: exclusive values, duplicates, normalized custom values, deletion policies |
| Backend build | PASS | `dotnet build apps/skipper-api/skipper-api.csproj --no-restore`; zero warnings/errors. Final integration run also rebuilt Skipper, Professor and Howell Sync |
| Backend persistence / Skipper integration | PASS | 78 tests, 0 failures, 0 skipped; includes new assignment tests and existing regressions |
| Backend unit tests | PASS | 73 tests, 0 failures, 0 skipped |
| Gilligan build | FAIL | Next build could not download configured Geist/Geist Mono fonts from Google Fonts; network-enabled retry was declined |
| Gilligan tests | PASS | 32 tests across 5 files |
| Gilligan TypeScript | PASS | `tsc --noEmit` |
| Changed UI lint | PASS | ESLint on changed production UI/API files |
| Manual browser testing | NOT RUN | No claim of browser/manual validation |
| Production migration | NOT RUN | Test databases only |

Full build verification remains outstanding. Initial regression runs exposed test fixture issues and outdated single-selection expectations; those were corrected before the final passing runs.

## Known limitations and follow-up dependencies

Legacy columns and deprecated API adapters are intentionally retained. Migration issue rows require deployment-specific review; invalid legacy catalog IDs cannot become valid foreign keys. Existing incompatible historical assignments remain readable but must be resolved before editing. Concurrent conflicting replacements can require refresh/retry; no optimistic-concurrency/version contract was added. No genetics or automatic catalog promotion exists.

Ticket 7 should measure collection projections and preserve pagination/Any semantics if variant filters are introduced. Ticket 8 should document audit review, backup/rollback, coordinated API alias retirement and separately validated column cleanup. See [model and operations documentation](animal-variant-assignments.md) for contracts and executable audit queries.
