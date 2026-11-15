# Local Catalog Setup

Howell imports GBIF taxonomy into the existing Professor PostgreSQL database. Skipper and
Gilligan read Professor after Howell exits; normal Animal creation never contacts GBIF.
No individual Animals or catalog variants are created by an import.

## Prerequisites and first-time setup

Use .NET 10, Docker with PostgreSQL 16, Node/npm, and an EF Core 10 tool matching the
repository (10.0.12). GBIF requires internet access and a real project/contact URL in the
User-Agent. Work from the repository root. These are Bash commands. Choose a **local
Development** database ending in `_dev` or `_test`; remote hosts are rejected.

For a fresh local environment using the repository's existing Compose configuration:

```bash
export POSTGRES_DB=critterops_dev
export POSTGRES_USER=critterops
export POSTGRES_PORT=5432
read -r -s -p 'Local PostgreSQL password: ' POSTGRES_PASSWORD; echo
export POSTGRES_PASSWORD
export DOTNET_ENVIRONMENT=Development
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__ProfessorDb="Host=127.0.0.1;Port=${POSTGRES_PORT};Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
export Providers__gbif__UserAgent='CritterOps/1.0 (+https://github.com/LifelessPumpkin/critter_ops)'
docker compose up -d db
# Install once; use update instead of install if already installed.
dotnet tool install --global dotnet-ef --version 10.0.12
(cd apps/skipper-api && CRITTEROPS_EF_DESIGN_TIME=true dotnet ef database update --project ../../packages/professor-persistence --startup-project .)
npm --prefix apps/gilligan-web ci
```

Use your own real project URL/contact when operating a fork. Do not commit credentials.
The existing Compose volume retains its original credentials/database: changing environment
variables does not reinitialize it. Use its actual credentials or a separate development
instance; do not delete the volume to make this script work. A password containing connection
string delimiters must be correctly quoted in the Npgsql connection string.

## HOW TO IMPORT 100+ SPECIES LOCALLY

After the one-time configuration above, one command populates the catalog:

```bash
./scripts/import-dev-catalog.sh          # defaults to demo
./scripts/import-dev-catalog.sh demo     # same profile explicitly
./scripts/verify-dev-catalog.sh demo
```

For larger real taxonomy coverage:

```bash
./scripts/import-dev-catalog.sh expanded
./scripts/verify-dev-catalog.sh expanded
```

Preview without writing catalog, issues, or execution history:

```bash
./scripts/import-dev-catalog.sh demo --dry-run
./scripts/import-dev-catalog.sh expanded --dry-run
```

Dry-run summaries label proposed changes and separately show actual persisted counts. They
never claim the proposed Species were persisted. File logs are still written.

| Preset | Minimum | Target | Maximum eligible Species | Examined-record ceiling | HTTP-attempt ceiling |
|---|---:|---:|---:|---:|---:|
| demo (default) | 100 | 150 | 200 | 1,500 | 1,800 |
| expanded (explicit) | 1,000 | 1,500 | 5,000 | 15,000 | 10,000 |

Both presets use the same GBIF client, reconciler, transactions, advisory lock and history.
They use 250 ms minimum request spacing, concurrency ceiling 1, page ceiling 100, and a
30-second per-request timeout. Existing bounded retries/backoff and Retry-After behavior
remain in force; every retry consumes the same total request budget. Processing is sequential.
Demo normally takes minutes; expanded takes substantially longer and is intentionally opt-in.
Network latency, skipped taxa, retries and existing coverage affect elapsed time.

## Configuration and scope accounting

Definitions are in `apps/howell-sync/appsettings.json` under
`DevelopmentCatalog:Profiles:demo` and `DevelopmentCatalog:Profiles:expanded`.
Each definition has `Name`, `Genera`, `MinimumSpecies`, `TargetSpecies`, `MaximumSpecies`,
`MaximumExaminedRecords`, and a `Provider` preset containing the existing provider's five
limit settings. Shared base URL, dataset, User-Agent and retry settings stay in `Providers:gbif`.
Profile limit presets override those five base settings; use profile-specific environment
variables to override a preset, for example:

```bash
export DevelopmentCatalog__Profiles__expanded__TargetSpecies=2500
export DevelopmentCatalog__Profiles__expanded__MaximumSpecies=3000
export DevelopmentCatalog__Profiles__expanded__Provider__MaximumRequestsPerRun=10000
export DevelopmentCatalog__Profiles__expanded__Provider__MinimumRequestInterval=00:00:00.500
./scripts/import-dev-catalog.sh expanded
```

This larger override is an available bounded configuration, not a claim that it was manually
executed. Configure array elements with `...__Genera__0`, `...__Genera__1`, etc. Configurations
must satisfy minimum <= target <= maximum, maximum <= 200 for demo / 5000 for expanded,
examined ceiling <= 20000, and the provider's existing validation. Unknown profiles are rejected.
The limit is a ceiling on profile expansion, not a deletion rule for a database already larger
than the preset. No existing rows are deleted or reset.

Scopes are explicitly named **genera**, not a full-taxonomy crawl. GBIF search is bounded and
filtered to species-rank accepted results. Search relevance can include unrelated names, so
Howell checks the authoritative usage and canonical genus before importing. Only distinct
active local Species linked to ACCEPTED GBIF references in the configured dataset and one of
the profile's canonical genera qualify. Local Synonym/Deprecated Species, unrelated genera,
other datasets, unlinked seeds and duplicate references cannot satisfy the minimum. A seeded
Species that is safely reconciled to an accepted in-scope GBIF usage does qualify, but is
reported as updated rather than newly created. Scientific names remain provider-backed;
common names are searchable alternate records and do not overwrite curated primary names.

Pages rotate across scopes for category coverage. All scopes, settings and bounds are saved
in the existing run's Scope JSON. The real catalog can include extinct species and provider
hybrid taxa classified at SPECIES rank; it is taxonomy, not a curated pet-store list.

## Reruns and recovery

```bash
./scripts/import-dev-catalog.sh demo
./scripts/import-dev-catalog.sh expanded
# Replace 42 with an incomplete run ID from CatalogSyncRuns:
./scripts/import-dev-catalog.sh expanded --retry-run 42
```

Repeated runs preserve internal IDs, provider references, manual values, and existing Animal
references. Expanded adds to demo; demo after expanded never removes data. A profile already
at target completes with zero new Species and zero provider requests, while still recording a
meaningful execution and existing matching count. `unchanged examined` counts records examined
in this execution, not every existing row.

Resume safely replays the stored bounded scope from the start, skipping already persisted
eligible references before fetching their details/names. It continues expansion without
recreating committed Species. This deliberately avoids trusting an unstable GBIF relevance
offset; it is not an exactly-once snapshot or a promise that provider search order never changes.
Retry uses saved profile settings, links a new run to the original, and rejects mismatched
profiles/datasets or completed source runs. A normal rerun uses current configuration.
Use the existing `sync --provider gbif --limit 100` command to refresh already-linked data.

History summaries distinguish existing matching Species, committed/proposed creations,
updates, examined unchanged records, eligible total, entire Professor total, HTTP attempts,
retries, issues, duration and final status. Request budget exhaustion is Partial (exit 4),
insufficient eligible Species is Partial/ProfileBelowMinimum (exit 5), and both retain committed
batches. Other exit codes: 1 execution failure, 2 configuration/arguments, 3 lock held, 130 cancellation.
An interrupted run is recovered by the existing catalog lock/history mechanism.

## Verification, history and issues

```bash
./scripts/verify-dev-catalog.sh demo
./scripts/verify-dev-catalog.sh expanded
# For the repository Compose database:
docker compose exec -T db psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" < documentation/operations/verify-dev-catalog.sql
```

Verification is read-only, contacts no GBIF service, counts persisted distinct Species, and
returns exit 5 when the selected profile is below minimum. The SQL report includes totals,
seed IDs, categories, primary/alternate names, aliases, external references, variant preferences,
recent history, issue groups, duplicate checks and Animal-to-Species references. Run-level
`RecordsCreated` is the authoritative new-row count; total references are not a Species count.
Inspect `logs/howell-sync/` for the matching timestamped log and `run=<id>`.

## Start Skipper and Gilligan

With the same connection environment, in separate terminals:

```bash
dotnet run --project apps/skipper-api --no-launch-profile --urls http://127.0.0.1:5199
SKIPPER_API_URL=http://127.0.0.1:5199 npm --prefix apps/gilligan-web run dev -- --hostname 127.0.0.1 --port 3001
```

Open `http://localhost:3001/animals/new` (Animals → Add Animal). Open Species to fetch a bounded
page; type `Burmese python`, `Python bivittatus`, `Rana`, or `Danio`. Select Animal Category
before selecting a Species to constrain results; selecting a structured Species determines
and locks its category. Use Next/Previous page without losing the selection. A species with
no preferred type displays Variant. Newly imported preferences are currently null; Howell's
broader class-to-variant classification remains a separate dependency. Existing stored Breed/
Morph/etc. preferences are projected and used. No species-specific labels are added in Gilligan.

Enter a name, choose/create an enclosure, and save. Verify `Animals.SpeciesId` with the SQL
report. Edit Animal uses the same selectors. Changing Species retains variant chips and marks
incompatible assignments for explicit removal; custom values remain supported.

API spot checks:

```bash
curl --fail 'http://127.0.0.1:5199/api/species?search=Burmese%20python&page=1&pageSize=25'
curl --fail 'http://127.0.0.1:5199/api/species?search=Python&animalGroupId=3&page=1&pageSize=25'
curl --fail 'http://127.0.0.1:5199/api/species?page=2&pageSize=25'
```

## Troubleshooting

- Target rejected: set `DOTNET_ENVIRONMENT=Development`, use loopback Host, and a database
  ending `_dev` or `_test`; arbitrary remote/production targets cannot be enabled by a flag.
- Database connection failure: verify container/port, original volume credentials and connection
  syntax. Neither importer nor API automatically migrates or resets the database.
- Pending migrations: run the EF command above with the same connection. PostgreSQL must allow
  `pg_trgm` creation (the existing migration enables it); fresh PostgreSQL 16 images support it.
- User-Agent rejected: configure a real project/contact URL, not the example.org placeholder.
- Partial/below minimum: inspect scope, budget, skipped taxa and issues. Broaden configured genera
  or increase limits within their hard ceilings, then rerun. Do not assume requested rows equal
  persisted canonical Species. A new target above 1000 may require more than 3000 requests.
- Classification issues: unknown classes remain skipped with `UnrecognizedAnimalGroup`, never
  guessed. The existing mapper recognizes GBIF's Squamata class as Reptile.
- Lock contention: another worker owns the catalog lock. Wait for it to finish or stop that
  process normally; do not remove locks/checkpoints manually.
- GBIF unavailable/throttled: retries are bounded and honor Retry-After; retain the run ID and
  resume after recovery. Nonzero exits are not successful imports.

See [search contracts](../architecture/catalog-search.md),
[performance measurements](../performance/catalog-search.md), and
[implementation/verification report](../catalog-search-implementation-report.md) for actual executed results.

## Executed results (November 14, 2026)

Both real commands, both verification scripts, default-demo/expanded reruns, and both dry-run
flags were executed successfully in the isolated Professor instance. Demo reached 150 eligible
Species (61 new, 88 already eligible, one newly linked existing Species) in 90.8 seconds with 332
requests. Expanded reached 1500 eligible Species (1350 new) in 19m44s with 4504 requests. Both
runs completed with recorded review issues and zero HTTP retries. Professor contains 1553 Species.
Reruns made no new Species or provider requests and preserved IDs/references. Expanded increased
demo-scope coverage to 447; this is expected overlap, not a reason to delete data.

The actual browser workflow created Animal 1 using Species 149 (`Python bivittatus`), verified
common/scientific searches, derived category, Variant fallback, and bounded page navigation.
The optional 2500-Species override and changes to the user's separate existing Compose database
were NOT RUN. Fresh database migrations and empty-catalog imports were covered with disposable
PostgreSQL and mocked GBIF in automated tests.
