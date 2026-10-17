# Adaptive Add Animal — implementation report

Implemented locally on October 17, 2026. Changes have not been pushed. No pull request description was prepared. Test servers and the disposable browser-verification database container were stopped/removed; the existing development database was not migrated.

## Files Changed

### Created

- `apps/skipper-api/Domain/Animals/AnimalGroup.cs`
- `apps/skipper-api/Domain/Animals/Species.cs`
- `apps/skipper-api/Domain/Animals/AnimalVariant.cs`
- `apps/skipper-api/Data/Configurations/AnimalCatalogConfiguration.cs`
- `apps/skipper-api/Data/AnimalCatalogSeed.cs`
- `apps/skipper-api/Data/Migrations/20261017170530_AddAnimalReferenceCatalog.cs`
- `apps/skipper-api/Data/Migrations/20261017170530_AddAnimalReferenceCatalog.Designer.cs`
- `apps/skipper-api/Data/Migrations/20261017170959_AddAnimalCatalogSearchIndexes.cs`
- `apps/skipper-api/Data/Migrations/20261017170959_AddAnimalCatalogSearchIndexes.Designer.cs`
- `apps/skipper-api/Dtos/Animals/CatalogDtos.cs`
- `apps/skipper-api/Controllers/AnimalCatalogController.cs`
- `apps/gilligan-web/src/lib/api/animalCatalog.ts`
- `apps/gilligan-web/src/components/animals/AnimalReferenceFields.tsx`
- `apps/gilligan-web/src/components/animals/animalDraft.ts`
- `apps/gilligan-web/src/app/animals/[id]/edit/page.tsx`
- `tests/skipper/Integration/AnimalCatalogIntegrationTests.cs`
- `tests/skipper/Unit/AnimalReferenceValidationTests.cs`
- `apps/gilligan-web/src/components/animals/AnimalForm.test.tsx`
- `apps/gilligan-web/src/components/animals/AnimalReferenceFields.test.tsx`
- `documentation/architecture/animal-reference-catalog.md`
- `documentation/animal-reference-implementation-report.md`

### Modified

- `apps/skipper-api/Domain/Animals/Animal.cs`
- `apps/skipper-api/Data/ProfessorDbContext.cs`
- `apps/skipper-api/Data/Configurations/AnimalConfiguration.cs`
- `apps/skipper-api/Data/Migrations/ProfessorDbContextModelSnapshot.cs`
- `apps/skipper-api/Dtos/Animals/CreateAnimalRequestDto.cs`
- `apps/skipper-api/Dtos/Animals/UpdateAnimalRequestDto.cs`
- `apps/skipper-api/Dtos/Animals/AnimalResponseDto.cs`
- `apps/skipper-api/Dtos/Animals/AnimalListItemDto.cs`
- `apps/skipper-api/Dtos/Animals/AnimalSearchRequestDto.cs`
- `apps/skipper-api/Services/Animals/AnimalService.cs`
- `apps/skipper-api/Services/Animals/CreateAnimalResult.cs`
- `apps/skipper-api/Services/Animals/UpdateAnimalResult.cs`
- `apps/skipper-api/Controllers/AnimalsController.cs`
- `apps/gilligan-web/src/lib/api/animals.ts`
- `apps/gilligan-web/src/components/animals/AnimalForm.tsx`
- `apps/gilligan-web/src/components/enclosures/EnclosureForm.tsx`
- `apps/gilligan-web/src/app/animals/new/page.tsx`
- `apps/gilligan-web/src/app/enclosures/new/page.tsx`
- `apps/gilligan-web/src/components/ui/Selection.tsx`
- `apps/gilligan-web/src/components/animals/AnimalOverview.tsx`
- `apps/gilligan-web/src/components/animals/AnimalCard.tsx`
- `apps/gilligan-web/src/components/animals/AnimalTable.tsx`
- `apps/gilligan-web/src/components/activity/EntityActivityPage.tsx`
- `packages/professor-db/README.md`
- `documentation/testing-and-ci.md`

## Database Changes

Added AnimalGroup, Species and AnimalVariant reference entities in the existing Skipper/Professor EF model. Animal has nullable links to all three. Existing Species, SubspeciesOrMorph and AnimalType columns remain. No historical animal is automatically matched or reclassified, including ambiguous Herptile/Aquatic records.

The additive catalog migration creates tables, restrict-delete foreign keys and scoped indexes, plus migration-managed seeds: seven groups, sixteen species and four representative dog breeds/python morphs. A second migration installs/uses pg_trgm and adds GIN substring-search indexes. The migration history makes initialization repeatable without production startup writes. PostgreSQL migration credentials must be allowed to install pg_trgm, or an administrator must preinstall it. Rolling back the search indexes retains the shared extension.

Actual PostgreSQL tests rolled back to the previous schema, inserted representative legacy records, migrated forward twice, and checked text/type preservation and seed counts. Model snapshot parity is also tested.

## API Changes

- GET `/api/animal-groups`: active groups, ordered by SortOrder/Id, with legacy-type compatibility metadata.
- GET `/api/species`: case-insensitive common/scientific substring search, optional animalGroupId, page/pageSize, initial empty search.
- GET `/api/animal-variants`: optional speciesId scope, case-insensitive name search and pagination.

Search results default to 20, cap at 100, execute in PostgreSQL, and include hasMore. Ordering is deterministic and search wildcard characters are escaped. No catalog is downloaded wholesale. Animal search additionally accepts animalGroupIds; catalog filters include matching unclassified legacy types only where the configured mapping is explicit. Original animalTypes filters remain available.

Create/PUT accept nullable catalog IDs or animal-local text. Structured IDs take precedence; selected species supplies its group when omitted. Without a selected group, custom animals use the legacy Other value. Validation rejects unknown/unavailable IDs and incompatible group/species/variant combinations with HTTP 400 and readable messages. FK races during writes return a readable validation result. Variants remain optional.

Animal list/detail responses retain old fields and add resolved names, IDs, scientific names and variant metadata. Projections join reference tables, avoiding N+1 lookups. Explicit null clears a reference. Older clients omitting IDs preserve references when round-tripping unchanged resolved values; an integration test also covers catalog variant renames between reads and edits.

## Frontend Changes

Animal Group loads from Skipper. Species and variant selectors reuse Ginger's styled combobox, approximately 300 ms debounce, keyboard selection, Escape and outside dismissal. Searches cancel/ignore superseded requests. Options show scientific names and retain selected display metadata. Catalog metadata controls Breed/Morph/etc. labels. Explicit custom-value actions remain usable during API errors or pending requests and never create global catalog entries. Incompatible group/species changes clear dependent references while preserving other fields.

The enclosure selector includes `+ Create new enclosure`. It preserves the form under a workflow UUID in sessionStorage and navigates to the existing Add Enclosure page. Success returns the actual created enclosure ID; cancellation returns only the workflow. Restoration refreshes options and separately fetches the created ID, merging asynchronous results safely. Direct Add Enclosure navigation retains its original cancellation destination. Storage failure keeps the user in the animal form with their values intact.

Drafts are isolated by workflow/tab, ignored for fresh unrelated sessions, rejected after six hours, and removed on animal success or explicit cancel. Completed/cancelled in-memory Add Animal fields are also reset. Missing returned enclosures show an error without losing animal information.

No Edit Animal page previously existed. A route now reuses the same form, retaining structured/custom/legacy values and undisplayed metadata on unrelated edits. Existing list/activity layouts remain in place; resolved values and scientific names are displayed, with legacy filters still accessible.

## External Data Evaluation

Research recommendations only: use a reviewed, versioned Catalogue of Life Base subset as a potential starting point, with separately licensed GBIF/iNaturalist vernacular enrichment. NCBI is useful for sequence-linked identifiers but is not a sole husbandry nomenclature authority. Source licenses, attribution, release identity, common-name quality, and merged/deleted identifiers must be reviewed per import. Breed/morph/strain data requires separate husbandry curation.

No external import, synchronization, runtime taxonomy call or administration UI was implemented. The [architecture document](architecture/animal-reference-catalog.md#future-external-data-evaluation-research-only-reviewed-2026-10-17) contains the source-by-source assessment and primary-source links.

## Testing Results

| Check | Result | Actual execution/evidence |
| --- | --- | --- |
| Backend build | PASS | `dotnet build apps/skipper-api --no-restore`; zero warnings/errors. |
| Backend unit tests | PASS | `dotnet test tests/skipper/Unit --no-restore`; 11 passed. |
| Backend integration tests | PASS | `dotnet test tests/skipper/Integration --no-restore`; 10 passed against disposable PostgreSQL Testcontainers. |
| Frontend build | PASS | `npm run build` in Gilligan; production compilation, TypeScript and route generation completed. |
| Frontend tests | PASS | `npm test` in Gilligan; 20 passed across four files, including existing tests. |
| Targeted lint | PASS | ESLint on new catalog/form/draft modules, tests, request utilities, changed route pages, animal display components and EnclosureForm. |
| Full repository frontend lint | FAIL | Six existing React hook/ref lint findings in AnimalOverview and Selection. Their underlying pre-existing patterns were observed during initial inspection; this feature does not refactor the shared selector lifecycle or list-loading mechanism. |
| Migration validation | PASS | Actual legacy-schema rollback/insert/forward test, repeated migration, snapshot parity, plus fresh-database CLI migration for browser testing. |
| Search volume | PASS | Integration fixture with 2,000 additional species verifies bounded page results and deterministic pagination. |
| Add Animal workflow (browser) | PASS | Real Gilligan → Skipper → disposable PostgreSQL: enter name/notes, choose Reptile, scientific-name search for Python regius, select Ball python and Albino, create enclosure, restore every entered value, auto-select new enclosure, create animal. |
| Enclosure cancellation (browser) | PASS | Cancel returned to the originating animal workflow with draft fields intact. Automated coverage also preserves a previous enclosure selection. |
| Animal editing (browser) | PASS | Opened the created animal's Edit page, retained references, changed notes, saved successfully. Automated coverage checks undisplayed historical source/chip/tag/disposition metadata. |
| Completed-draft cleanup (browser) | PASS | Reopening the completed workflow URL showed a blank form and draft-unavailable notice rather than restored animal data. |
| Existing animal compatibility | PASS | PostgreSQL historical text/type preservation, legacy filtering, older-client update behavior, explicit reference clearing; frontend legacy draft restoration. |
| Every edge case manually in browser | NOT RUN | Out-of-order requests, API errors/pending requests, empty result sets, storage failure, and draft expiration are covered by automated tests rather than a separate browser pass for each. |
| Production migration/deployment | NOT RUN | Intentionally outside scope. |

## Known Limitations

- Initial reference coverage is deliberately small: sixteen species and four variants. Custom values remain necessary.
- The combobox displays the first 20 matching results; refine search for additional matches. APIs support subsequent pages. Deep offset pagination and larger catalog imports should be measured before expansion.
- Ambiguous legacy groups remain legacy until a user makes an informed classification choice. They remain visible through All and legacy filters.
- Common names are optional, scientific names are required for catalog entries, and all animal-local species/variant text retains the existing 100-character limit.
- Draft persistence depends on browser session storage. Six-hour expiration is checked on access; a closed browser/tab may clear drafts sooner.
- Full frontend lint has the existing six hook/ref findings noted above. Test execution also emits an existing Vite configuration warning and NuGet SSH.NET vulnerability advisories from the integration-test dependency graph. No dependency upgrade is included.
- Local EF CLI is version 8.0.11 with EF runtime 10.0.12; it emitted a version warning. Generated migrations and snapshot parity were verified on PostgreSQL.

## Follow-Up Work

Potential future tickets, not implemented: reviewed offline species imports with provenance/license handling; reference-data administration; broader species/variant catalogs; source-ID reconciliation and more advanced taxonomy support. The existing lint findings and test-dependency advisories can be handled separately.
