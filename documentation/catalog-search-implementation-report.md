# Catalog search and development import implementation report

Implementation is on `codex/catalog-search-performance`, based on the existing local ticket-95
commit `e05ddce`. The supplied worktree initially pointed at the much older scaffold commit;
the other checkout was left untouched. No push or pull request was created, and the prohibited
git status/diff/show commands were not used.

## Search implementation

Both endpoints execute bounded PostgreSQL queries with lightweight DTO projections and
AsNoTracking. Species search covers scientific, primary common, alternate common and alias names,
with case-insensitive literal substring matching, normalized input whitespace, category filters,
exact/prefix/partial ranking and stable display/scientific/ID tie-breakers. Variant search requires
SpeciesId, filters by name, and orders by name/ID. Pagination defaults to 25, caps at 100 and
rejects offsets beyond 100000. HasMore uses a single lookahead row.

Matching-ID unions let PostgreSQL use the existing scientific/common-name indexes. The new EF
migration adds only two measured pg_trgm GIN indexes, on alternate common names and aliases.
The existing category/variant B-tree and name GIN indexes remain. Fresh-database migration and
model-snapshot consistency are covered by PostgreSQL integration tests. See
[contracts](architecture/catalog-search.md) and [measured plans](performance/catalog-search.md).

## Gilligan changes

The shared Add/Edit selectors request one server page at a time only while open. Nonempty text
is debounced 300 ms; input changes immediately invalidate/abort earlier work. Active-request
identity also guards against transports that ignore abort. Category changes, Species changes,
close/reopen, clearing and unmounting are covered. Next/Previous paging, Retry, loading and empty
states stay local to the selector. Selected Species and current variant assignments remain intact;
incompatible assignments require explicit removal. Existing Ginger components and custom-value
behavior are preserved. No full Species or Variant catalog is downloaded.

## GBIF import profiles

The configuration presets in Howell's appsettings.json use the same provider, reconciler,
transaction/advisory-lock lifecycle and history as ordinary imports. `demo` defaults to a 150
Species target (minimum 100, maximum 200); `expanded` targets 1500 (minimum 1000, maximum 5000).
Each has explicit genus scopes, examined-record/request ceilings, pacing, concurrency, page size
and timeout. Scope verification counts distinct active Species backed by accepted in-scope
canonical GBIF usages, never raw HTTP rows or reference counts. Unrelated records and unlinked
seeds do not satisfy the minimum. Reruns preserve IDs and curation; retry replays saved scopes,
skipping already-linked eligible taxa. The existing mapper now recognizes GBIF Squamata as
Reptile; missing classification remains an issue/skip, never a guessed group.

Scripts: `scripts/import-dev-catalog.sh [demo|expanded] [--dry-run] [--retry-run ID]` and
`scripts/verify-dev-catalog.sh [demo|expanded]`. Both enforce Development/loopback/database suffix
safety and migration preflight. Verification performs no provider requests. Detailed counts,
requests/retries/issues/duration/final status are logged and execution metrics persist in Professor.

## Real execution and end-to-end verification

All real runs used the isolated `critterops-ticket7-db` Professor container, database
`critterops_catalog_dev`, port 55439. The user's existing `critterops-db` was not reset or modified.
The database and imported catalog remain after Howell exits.

- Earlier pre-profile run (November 8): 126 new Species, 551 requests, retained in the database.
- Demo run 2 (November 14): 88 existing eligible Species → **150 eligible Species**; 61 new
  Species, one existing Species updated, 68 examined unchanged, 332 HTTP requests, zero retries,
  17 new issues, 90.8 seconds, CompletedWithIssues. Total Professor Species afterward: 203.
- Expanded run 3: 150 existing eligible Species → **1500 eligible Species**; **1350 new
  Species**, zero updated, 147 examined unchanged, 4504 HTTP requests, zero retries,
  361 new issues, 1184.1 seconds (19m44s), CompletedWithIssues. Total Professor Species: **1553**.
  Issues: 180 UnrecognizedAnimalGroup, 178 RepeatedUsageKey, 3 InvalidCommonName. These were
  recorded and skipped/handled without inventing classification or duplicating Species.
- Real default-demo and expanded reruns (runs 4/5): zero created/updated, zero HTTP requests,
  Completed. Species-ID/scientific-name and external-reference identity fingerprints stayed
  identical; common-name and alias counts stayed identical; duplicate queries returned no rows.
- After expanded, demo's overlapping scopes contain **447** eligible Species. Rerunning demo
  correctly retains them rather than deleting down to its smaller target. Expanded remains 1500.
- Both real dry-run commands completed with no new catalog/history rows. Since targets were
  already satisfied, these no-op dry runs needed zero HTTP requests; retrieval/proposed changes
  in an empty catalog were exercised by the mocked-provider PostgreSQL tests.
- Both verification scripts passed. Professor has **1538** distinct accepted GBIF-backed Species
  overall, including 38 outside expanded's scopes from the earlier pre-profile run; total Species
  is 1553, including retained unlinked seeds. See [stored verification output](operations/catalog-verification-results.txt).
- Real imported coverage includes mammals, birds, reptiles, amphibians and invertebrates.
  The two Fish rows remain seed records: sampled GBIF fish records lacked classification.

Real Skipper HTTP checks passed for `Python bivittatus`, its alternate common name `Burmese
python`, `Rana` and `Drosophila`; category filtering, disjoint 25-row pages, HasMore and invalid
page/pageSize rejection also passed. Gilligan was built and started against this API. In Add
Animal, Reptile filtering, common/scientific search and selection were exercised in the actual
browser. Selection derived Reptile and displayed the Variant fallback. Creating an enclosure
and returning to the Animal draft preserved the selected Species. Saving created **Animal 1,
Catalog verification python**, referencing **Species 149 / Python bivittatus / GBIF 4820533**.
Direct PostgreSQL and HTTP reads confirmed the reference, including after expanded and both reruns. The final production Gilligan build also displayed distinct 25-row pages 1 and 2 against the expanded catalog. No mocked frontend/API data was used.

## Performance results

Actual EXPLAIN ANALYZE results for 1000, 10000 and 100000 synthetic Species are in the
[performance report](performance/catalog-search.md), with complete SQL/plans. At 100000 Species:
exact scientific 108.822 ms, partial 18.622 ms, primary common 10.432 ms, alternate 118.800 ms,
alias 113.643 ms, category 7.931 ms, combined 13.224 ms, empty 26.036 ms, no-match 6.638 ms,
page-1000 offset 60.140 ms and Species-specific variant 5.556 ms. These are measured single-query
observations, not latency guarantees. Synthetic data exists only in disposable test databases. The final real 1553-Species database
also ran the captured production query shape with EXPLAIN ANALYZE: Python bivittatus 1.279 ms,
Burmese python 1.082 ms, Drosophila 2.102 ms, no-match 1.179 ms. Full real plans are in
[the real-catalog plan output](performance/catalog-real-plans.txt).

## Testing

| Check | Result |
|---|---|
| Professor migrations | PASS — isolated real database and fresh integration databases |
| Backend build | PASS — Skipper, Howell and Professor compiled by the test builds |
| Backend unit tests | PASS — 82 |
| PostgreSQL integration tests | PASS — 85 (the large performance fixture is opt-in) |
| Gilligan production build | PASS |
| Gilligan tests | PASS — 45 |
| Changed selector/API-client lint | PASS |
| Import script tests | PASS — 4; integrated into Mary Ann's make test |
| Real demo import | PASS — 150 distinct eligible Species |
| Real expanded import | PASS — 1500 distinct eligible Species |
| Real reruns and verification scripts | PASS — identities unchanged, zero creations/requests |
| Real dry runs | PASS — no-op because targets already met; no database/history writes |
| End-to-end manual Animal creation | PASS — Animal 1 → Species 149 |
| Performance tests | PASS — opt-in real PostgreSQL EXPLAIN run at three sizes |

Initial test failures were corrected and rerun: asynchronous selector expectations, direct MVC
result subclass assertions, and preserving ordinary imports' repeated-provider-page failure
behavior. The final results above reflect the corrected code. Google font downloading required
network access for the production web build.

## Local Catalog Setup / HOW TO IMPORT 100+ SPECIES LOCALLY

The [complete setup guide](operations/local-catalog-setup.md) includes prerequisites, environment
variables, database startup, migrations, exact demo/expanded/dry-run/retry commands, verification
SQL, startup URLs, search examples and troubleshooting. After that one-time setup:

```bash
./scripts/import-dev-catalog.sh demo
./scripts/verify-dev-catalog.sh demo
./scripts/import-dev-catalog.sh expanded
./scripts/verify-dev-catalog.sh expanded
```

The real verification environment used `dotnet run --project apps/skipper-api --no-build
--no-launch-profile --urls http://127.0.0.1:5199` and Gilligan's production `npm run start --
--hostname 127.0.0.1 --port 3001` with `SKIPPER_API_URL=http://127.0.0.1:5199`. Open
`http://localhost:3001/animals/new`. Commands to change a new contributor's existing Compose
volume were not executed against the user's separate database; follow the guide using the
correct existing volume credentials. The optional 2500-Species configuration was NOT RUN.

## Known limitations

- GBIF v1 Backbone compatibility remains explicit; this does not migrate to COL XR.
- Provider search is relevance ordered, not an immutable taxonomy snapshot. Resume safely
  replays bounded scopes; already committed IDs remain stable.
- Some GBIF usages have no recognizable class; they remain skipped and recorded. In particular,
  sampled Betta records have no class, so fish coverage can be incomplete.
- New imported preferred variant types remain null. Gilligan intentionally displays Variant;
  broader preferred-type classification belongs to the separate Howell classification work.
- The real catalog includes valid extinct/provider species-rank hybrid taxa. Profiles are genus
  presets, not a manually curated list of pet species.
- Offset pagination is deterministic for a fixed catalog, but concurrent imports can shift pages.
- Maximums constrain profile expansion; rerunning demo after expanded never deletes extra rows.
- Larger custom scopes/targets and remote PostgreSQL deployment were NOT RUN.

## Files changed

- `Makefile`
- `README.md`
- `apps/gilligan-web/src/components/animals/AnimalReferenceFields.test.tsx`
- `apps/gilligan-web/src/components/animals/AnimalReferenceFields.tsx`
- `apps/gilligan-web/src/components/animals/CatalogNavigation.tsx`
- `apps/gilligan-web/src/components/animals/VariantPicker.tsx`
- `apps/gilligan-web/src/components/animals/useCatalogSearch.test.tsx`
- `apps/gilligan-web/src/components/animals/useCatalogSearch.ts`
- `apps/gilligan-web/src/components/ui/Selection.tsx`
- `apps/gilligan-web/src/lib/api/animalCatalog.ts`
- `apps/howell-sync/Commands/CatalogCommand.cs`
- `apps/howell-sync/Execution/CatalogExecutionContext.cs`
- `apps/howell-sync/Execution/CatalogOperationRunner.cs`
- `apps/howell-sync/Execution/DevelopmentCatalogImport.cs`
- `apps/howell-sync/Execution/DevelopmentCatalogProfile.cs`
- `apps/howell-sync/Program.cs`
- `apps/howell-sync/Providers/Gbif/GbifApiClient.cs`
- `apps/howell-sync/Providers/Gbif/GbifCatalogProvider.cs`
- `apps/howell-sync/Providers/Gbif/GbifTaxonMapper.cs`
- `apps/howell-sync/README.md`
- `apps/howell-sync/appsettings.json`
- `apps/skipper-api/Controllers/AnimalCatalogController.cs`
- `apps/skipper-api/Dtos/Animals/CatalogDtos.cs`
- `documentation/architecture/catalog-search.md`
- `documentation/catalog-search-implementation-report.md`
- `documentation/operations/catalog-verification-results.txt`
- `documentation/operations/local-catalog-setup.md`
- `documentation/operations/verify-dev-catalog.sql`
- `documentation/performance/catalog-real-plans.txt`
- `documentation/performance/catalog-search-plans.txt`
- `documentation/performance/catalog-search.md`
- `packages/mary-ann-tests/README.md`
- `packages/mary-ann-tests/scripts/test-all.sh`
- `packages/mary-ann-tests/scripts/test-howell-script.sh`
- `packages/professor-persistence/Data/Configurations/CatalogMetadataConfiguration.cs`
- `packages/professor-persistence/Data/Migrations/20261114174712_AddCatalogAlternateNameSearchIndexes.Designer.cs`
- `packages/professor-persistence/Data/Migrations/20261114174712_AddCatalogAlternateNameSearchIndexes.cs`
- `packages/professor-persistence/Data/Migrations/ProfessorDbContextModelSnapshot.cs`
- `scripts/import-dev-catalog.sh`
- `scripts/verify-dev-catalog.sh`
- `tests/howell/test_import_script.py`
- `tests/skipper/Integration/CatalogPerformanceTests.cs`
- `tests/skipper/Integration/CatalogSearchTests.cs`
- `tests/skipper/Integration/DevelopmentCatalogImportTests.cs`
- `tests/skipper/Integration/PostgresFixture.cs`
- `tests/skipper/Unit/DevelopmentCatalogTests.cs`
