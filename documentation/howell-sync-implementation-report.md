# Howell Sync — Ticket 3 implementation report

## Files changed

Created:

- `apps/howell-sync/howell-sync.csproj`
- `apps/howell-sync/packages.lock.json`
- `apps/howell-sync/appsettings.json`
- `apps/howell-sync/Program.cs`
- `apps/howell-sync/Commands/CatalogCommand.cs`
- `apps/howell-sync/Providers/ICatalogProvider.cs`
- `apps/howell-sync/Persistence/CatalogSession.cs`
- `apps/howell-sync/Persistence/CatalogRunRepository.cs`
- `apps/howell-sync/Execution/CatalogExecutionContext.cs`
- `apps/howell-sync/Execution/CatalogOperationRunner.cs`
- `apps/howell-sync/README.md`
- `packages/professor-persistence/professor-persistence.csproj`
- `packages/professor-persistence/packages.lock.json`
- `tests/skipper/Unit/HowellCommandTests.cs`
- `tests/skipper/Integration/HowellExecutionTests.cs`
- `tests/skipper/Integration/HowellProcessHarness.cs`
- `documentation/howell-sync-implementation-report.md`

Modified:

- `README.md`
- `.github/workflows/mary-ann-testing.yml`
- `apps/skipper-api/skipper-api.csproj`
- `apps/skipper-api/packages.lock.json`
- `apps/skipper-api/Dockerfile`
- `apps/skipper-api/README.md`
- `tests/skipper/Unit/MaryAnn.Skipper.UnitTests.csproj`
- `tests/skipper/Integration/MaryAnn.Skipper.IntegrationTests.csproj`
- `packages/mary-ann-tests/README.md`
- `documentation/project-structure.md`

Relocated the following files from `apps/skipper-api/` into
`packages/professor-persistence/`, retaining their relative paths and namespaces.
Only ProfessorDbContext's descriptive comment changed; models, mappings, migration
IDs, and the snapshot are unchanged. Skipper's design-time factory remains in Skipper.

- `Domain/Activity/ActivityEvent.cs`
- `Domain/Activity/ActivityEventAnimal.cs`
- `Domain/Activity/ActivityEventAnimalRelationshipType.cs`
- `Domain/Activity/ActivityEventEnclosure.cs`
- `Domain/Activity/ActivityEventEnclosureRelationshipType.cs`
- `Domain/Activity/ActivityEventType.cs`
- `Domain/Activity/AnimalDispositionActivity.cs`
- `Domain/Activity/AnimalDispositionType.cs`
- `Domain/Activity/AnimalFeedingActivity.cs`
- `Domain/Activity/AnimalFeedingQuantityUnit.cs`
- `Domain/Activity/AnimalFeedingResult.cs`
- `Domain/Activity/AnimalMedicationActivity.cs`
- `Domain/Activity/AnimalMedicationResult.cs`
- `Domain/Activity/AnimalMedicationRoute.cs`
- `Domain/Activity/AnimalMovementActivity.cs`
- `Domain/Activity/AnimalTreatmentActivity.cs`
- `Domain/Activity/AnimalTreatmentResult.cs`
- `Domain/Activity/EnclosureCleaningActivity.cs`
- `Domain/Activity/EnclosureCleaningType.cs`
- `Domain/Activity/EnclosureWaterChangeActivity.cs`
- `Domain/Activity/InspectionActivity.cs`
- `Domain/Activity/InspectionResult.cs`
- `Domain/Activity/MaintenanceActivity.cs`
- `Domain/AnimalTimeline/AnimalTimelineEventType.cs`
- `Domain/Animals/Animal.cs`
- `Domain/Animals/AnimalGroup.cs`
- `Domain/Animals/AnimalSex.cs`
- `Domain/Animals/AnimalStatus.cs`
- `Domain/Animals/AnimalType.cs`
- `Domain/Animals/AnimalVariant.cs`
- `Domain/Animals/CatalogMetadata.cs`
- `Domain/Animals/Species.cs`
- `Domain/EnclosureTimeline/EnclosureTimelineEventType.cs`
- `Domain/Enclosures/Enclosure.cs`
- `Domain/Enclosures/EnclosureMobility.cs`
- `Domain/Enclosures/EnclosureStatus.cs`
- `Domain/Enclosures/EnclosureType.cs`
- `Domain/Tasks/RecurrenceType.cs`
- `Domain/Tasks/Task.cs`
- `Domain/Tasks/TaskType.cs`
- `Data/AnimalCatalogSeed.cs`
- `Data/Configurations/ActivityEventAnimalConfiguration.cs`
- `Data/Configurations/ActivityEventConfiguration.cs`
- `Data/Configurations/ActivityEventEnclosureConfiguration.cs`
- `Data/Configurations/AnimalCatalogConfiguration.cs`
- `Data/Configurations/AnimalConfiguration.cs`
- `Data/Configurations/AnimalDispositionActivityConfiguration.cs`
- `Data/Configurations/AnimalFeedingActivityConfiguration.cs`
- `Data/Configurations/AnimalMedicationActivityConfiguration.cs`
- `Data/Configurations/AnimalMovementActivityConfiguration.cs`
- `Data/Configurations/AnimalTreatmentActivityConfiguration.cs`
- `Data/Configurations/CatalogMetadataConfiguration.cs`
- `Data/Configurations/EnclosureCleaningActivityConfiguration.cs`
- `Data/Configurations/EnclosureConfiguration.cs`
- `Data/Configurations/EnclosureWaterChangeActivityConfiguration.cs`
- `Data/Configurations/InspectionActivityConfiguration.cs`
- `Data/Configurations/MaintenanceActivityConfiguration.cs`
- `Data/Configurations/TaskConfiguration.cs`
- `Data/Migrations/20260704232051_InitialCreate.Designer.cs`
- `Data/Migrations/20260704232051_InitialCreate.cs`
- `Data/Migrations/20260705032400_AddEnclosure.Designer.cs`
- `Data/Migrations/20260705032400_AddEnclosure.cs`
- `Data/Migrations/20260705120000_AddAnimal.Designer.cs`
- `Data/Migrations/20260705120000_AddAnimal.cs`
- `Data/Migrations/20260801193455_AddEnclosureTimeline.Designer.cs`
- `Data/Migrations/20260801193455_AddEnclosureTimeline.cs`
- `Data/Migrations/20260803030713_AddAnimalTimeline.Designer.cs`
- `Data/Migrations/20260803030713_AddAnimalTimeline.cs`
- `Data/Migrations/20260816000000_AddActivityLedger.cs`
- `Data/Migrations/20260816203251_AddAnimalMovementActivity.Designer.cs`
- `Data/Migrations/20260816203251_AddAnimalMovementActivity.cs`
- `Data/Migrations/20260816210711_AddAnimalFeedingActivity.Designer.cs`
- `Data/Migrations/20260816210711_AddAnimalFeedingActivity.cs`
- `Data/Migrations/20260816220707_AddAnimalDispositionActivity.Designer.cs`
- `Data/Migrations/20260816220707_AddAnimalDispositionActivity.cs`
- `Data/Migrations/20260816223422_AddAnimalMedicalActivities.Designer.cs`
- `Data/Migrations/20260816223422_AddAnimalMedicalActivities.cs`
- `Data/Migrations/20260816224908_AddEnclosureCleaningActivity.Designer.cs`
- `Data/Migrations/20260816224908_AddEnclosureCleaningActivity.cs`
- `Data/Migrations/20260817010356_AddTasks.Designer.cs`
- `Data/Migrations/20260817010356_AddTasks.cs`
- `Data/Migrations/20261006032003_AddStructuredOperationalActivities.Designer.cs`
- `Data/Migrations/20261006032003_AddStructuredOperationalActivities.cs`
- `Data/Migrations/20261017170530_AddAnimalReferenceCatalog.Designer.cs`
- `Data/Migrations/20261017170530_AddAnimalReferenceCatalog.cs`
- `Data/Migrations/20261017170959_AddAnimalCatalogSearchIndexes.Designer.cs`
- `Data/Migrations/20261017170959_AddAnimalCatalogSearchIndexes.cs`
- `Data/Migrations/20261017184321_ExpandAnimalReferenceCatalog.Designer.cs`
- `Data/Migrations/20261017184321_ExpandAnimalReferenceCatalog.cs`
- `Data/Migrations/20261017210000_MigrateLegacyAnimalReferences.cs`
- `Data/Migrations/ProfessorDbContextModelSnapshot.cs`
- `Data/ProfessorDbContext.cs`

## Howell Sync architecture

Standalone net10.0 console executable using Generic Host, configuration, DI, and
JSON Microsoft.Extensions.Logging. Program handles entry/lifetime only; command
parsing, provider registry, execution runner/context, session factory, and history
repository are separate. No HTTP host or Skipper project dependency is introduced.
The runner, registry, and history repository are scoped; a stateless session factory
creates the lock connection. Short-lived DbContexts use that connection per batch or
history operation. Both Skipper and Howell reference the shared Professor library.

ICatalogProvider declares a canonical name, supported operations, selected-provider
configuration validation, and cancellable operation dispatch. Production registers
no providers. Controlled test implementations live exclusively in test assemblies.

## CLI

`help`, `import --provider <name>`, `sync --provider <name>`, and `validate` are
implemented. Operation commands support `--help` and `--dry-run`; validation rejects
provider selection. Unknown/repeated options and missing provider values fail.
Exit codes: 0 completion, 1 execution/database/history failure, 2 invalid
arguments/configuration/unsupported provider, 3 lock contention, 130 cancellation.
GBIF fails clearly before database access and never records a successful import.

## Professor integration

One ProfessorDbContext, one set of entities/configurations, and the existing Professor
PostgreSQL connection convention (`ConnectionStrings__ProfessorDb`). Standard JSON
and environment configuration support future provider-specific sections; only the
selected provider validates its configuration. No credentials were added.

CatalogSyncRun records Running, Completed/CompletedWithIssues, Failed, or Cancelled,
provider/operation, timestamps, committed counts, and sanitized failure diagnostics.
CatalogSyncIssue can be recorded through the execution context with its run association.
No new migration, history table, lock table, or database was introduced. Existing
migration commands now target the shared library with Skipper as startup project.

## Concurrency and recovery

A catalog-wide PostgreSQL session advisory lock uses key `0x4352495454434154`.
Acquisition is atomic/nonblocking; contenders exit 3. A dedicated nonpooled,
nonmultiplexed connection owns both the lock and all operation contexts. Closing it
releases the lock. A connection interceptor forbids EF reopening a lost session,
including when provider code catches a database error and queries again.

After acquiring the lock, a writing operation marks abandoned Running rows Failed
with completion timestamps and a diagnostic before creating its new run. It cannot
recover an active cooperating worker. Validation/dry runs leave history untouched.
PostgreSQL rolls back an active transaction and releases locks when it detects a
crashed/disconnected client. A real child-process kill is covered by integration tests.

## Dry run

Dry runs persist no catalog changes, issues, execution history, or recovery updates.
Logs include DryRun and an ephemeral execution identifier; no schema extension is
needed. Providers evaluate plans with read-only queries and return planned counts.
PostgreSQL read-only transactions reject EF/raw SQL mutation attempts. A provider
that attempts writes fails rather than pretending a simulation succeeded.

## Transactions and cancellation

Each bounded batch receives a fresh context and transaction. Catalog changes and
cumulative progress counts commit together. Exceptions/cancellation roll back the
active batch, including writes saved inside its callback. Previously committed
batches remain; whole-import rollback and resumability are not claimed. External
fetches must occur before entering batch callbacks. Providers must use the supplied
context sequentially and honor cancellation. Ctrl+C/SIGTERM/host stop propagate
through the runner. History cleanup has an independent 10-second cancellation budget.
Database failures are not retried indefinitely; incomplete history is recovered on
the next writing run when persistence becomes available.

## Testing

| Check | Result | Evidence |
|---|---|---|
| Independent Howell build | PASS | `dotnet build apps/howell-sync --no-restore --nologo`; zero warnings/errors |
| Existing Skipper build | PASS | `dotnet build apps/skipper-api --no-restore --nologo`; zero warnings/errors |
| Existing backend and Howell unit tests | PASS | 30 tests, zero failures/skips |
| Existing backend and Howell PostgreSQL integration tests | PASS | 28 tests, zero failures/skips, real disposable PostgreSQL 16 |
| CLI tests | PASS | Help, parsing, invalid command/options, missing provider, unsupported provider, dry-run parsing, exit codes |
| Provider configuration tests | PASS | DI resolution, operation support, selected validation, unused configuration, environment override, import/sync dispatch |
| Execution-history tests | PASS | Running visibility, completed/with-issues/failed/cancelled, provider, operation, timestamps, committed counts; dry runs have no rows |
| PostgreSQL concurrency | PASS | Independent runner instances contend; active run not marked stale; subsequent runs acquire lock |
| Transaction tests | PASS | Committed representative record, failed/cancelled batch rollback, read-only dry-run write rejection |
| Cancellation tests | PASS | Token cancellation rolls back, finalizes Cancelled, and releases lock; standalone SIGTERM exits 130 |
| Restart/recovery tests | PASS | Abandoned history, lost backend connection, and forcibly killed child process; next operation recovers |
| Existing Skipper Docker image | PASS | Locked restore and publish with shared library; used temporary Buildx state because existing cache permissions were invalid |
| Migration tooling | PASS | Documented shared-project EF command applied all existing migrations to a disposable database |
| Manual CLI smoke tests | PASS | Standalone help, validate, validate dry run, unknown command/option, missing provider, unsupported GBIF import/sync, JSON logs, SIGTERM exit/lock release |
| Manual real provider import/sync | NOT RUN | Intentionally no external provider exists |

Initial parallel builds collided on shared output files; sequential reruns passed.
The initial child-process test selected the SDK-generated entry point; specifying
the test harness StartupObject fixed it. Sandbox socket restrictions required
approved reruns. Local EF CLI 8.0.11 emitted its existing version warning against
EF 10.0.12; migration application succeeded. The worker never auto-applies migrations.
The disposable manual-test database container was stopped after verification.

## Known limitations

No external integration, taxonomy retrieval/reconciliation, production scheduling,
container deployment, new public API, or separate database. No automatic provider
retry/resumption: completed batches can remain after failure. Network partitions can
delay PostgreSQL detecting a lost session. Advisory locking is cooperative and does
not block ordinary Skipper requests; future catalog writers must use the same key.
Session-level locking requires a direct connection or session-pooling proxy.
Providers are trusted code and must not replace connections, escape framework-owned
transactions, persist outside the supplied context, or perform concurrent batches.

## Follow-up dependencies

- Ticket 4: register GBIF, validate its options, implement bounded external fetches,
  safe dry-run planning, issue diagnostics, and provider-specific idempotency.
- Ticket 5: schedule the executable and consume its exit codes/history; add production
  execution policy without moving long-running sync into Skipper.
- Ticket 6: extend shared models/mappings for variant assignments independently.
- Ticket 7: improve catalog search through shared persistence and Skipper APIs.
- Ticket 8: expand provider operations/recovery runbooks once integrations exist.

No changes were pushed and no pull request description was written.
