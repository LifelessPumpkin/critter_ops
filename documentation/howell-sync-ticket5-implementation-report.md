# Ticket 5 implementation report

Implemented existing-reference synchronization, bounded expansion reuse, conservative
field ownership, durable sync checkpoints, linked retries, issue deduplication and metrics.
No scheduler, deployment, API, separate database, push, or pull request description was created.

## Files changed

Created:

- `apps/howell-sync/Synchronization/CatalogReferenceRefresher.cs`
- `apps/howell-sync/Synchronization/CatalogIssueRecorder.cs`
- `packages/professor-persistence/Data/Migrations/20261027042605_AddCatalogSynchronizationProgress.cs`
- `packages/professor-persistence/Data/Migrations/20261027042605_AddCatalogSynchronizationProgress.Designer.cs`
- `tests/skipper/Integration/GbifSynchronizationTests.cs`
- `documentation/howell-sync-ticket5-implementation-report.md`

Modified:

- `apps/howell-sync/Commands/CatalogCommand.cs`
- `apps/howell-sync/Execution/CatalogExecutionContext.cs`
- `apps/howell-sync/Execution/CatalogOperationRunner.cs`
- `apps/howell-sync/Persistence/CatalogRunRepository.cs`
- `apps/howell-sync/Providers/ICatalogProvider.cs`
- `apps/howell-sync/Providers/Gbif/GbifApiClient.cs`
- `apps/howell-sync/Providers/Gbif/GbifCatalogProvider.cs`
- `apps/howell-sync/Providers/Gbif/GbifSpeciesReconciler.cs`
- `apps/howell-sync/README.md`
- `packages/professor-persistence/Domain/Animals/CatalogMetadata.cs`
- `packages/professor-persistence/Data/Configurations/CatalogMetadataConfiguration.cs`
- `packages/professor-persistence/Data/Migrations/ProfessorDbContextModelSnapshot.cs`
- `tests/skipper/Integration/GbifImportTests.cs`
- `tests/skipper/Integration/CatalogMetadataIntegrationTests.cs`
- `tests/skipper/Unit/HowellCommandTests.cs`

The existing metadata rollback test still targets its historical schema. Its insertion
and final count now avoid mapping new columns onto that old schema. A separate new test
verifies the synchronization migration's upgrade/downgrade and preservation of legacy
reference IDs and unknown ownership.

## Architecture and lifecycle

The existing provider registry, runner, Professor context, session advisory lock, batch
transactions, HTTP client, request protections, and importer are reused. GBIF retrieval
normalizes selected fields into `CatalogTaxon`; `CatalogReferenceRefresher` handles
existing-reference comparisons and writes without GBIF DTO dependencies. Existing-reference
imports delegate to that same refresher. New taxa still use the existing import matching
and creation logic.

The runner validates configuration, acquires the catalog-wide PostgreSQL lock, validates
retry scope if present, recovers abandoned Running history, starts a new run, and invokes
the provider. External retrieval precedes each short transaction. Catalog changes, issues,
counts and reference checkpoints commit together. Finalization records status, duration
endpoints, HTTP counters and completion reason. Cancellation has independent bounded
history cleanup. No transaction is held during HTTP calls.

## CLI and configuration

```sh
sync --provider gbif [--taxon <existing-key>] [--limit 1..1000] [--dry-run]
import --provider gbif (--query <text> | --taxon <key>) [--limit 1..1000] [--dry-run]
sync --provider gbif --retry-run <run-id> [--dry-run]
import --provider gbif --retry-run <run-id> [--dry-run]
validate [--dry-run]
```

Sync selects existing references in the configured dataset, plus missing-dataset references
that need review; it cannot create Species/references. Import is explicit bounded expansion.
Queries are text search, not descendant enumeration. Prior `sync --query` callers must use
`import --query`. Retry rejects scope/limit overrides and restores the stored original scope.

Existing provider settings continue to control dataset, default record limit, request budget,
page size, attempt timeout, concurrency, pacing, Retry-After and exponential retries. Every
request and HTTP retry counts against one invocation budget. No new configuration system
or client was introduced. Each catalog transaction processes one record.

Exit codes: **0** bounded success (possibly issues); **1** failure; **2** invalid arguments,
configuration, provider or retry; **3** lock contention; **4** partial request-budget exhaustion;
**130** cancellation.

## Change detection and manual protection

Scientific and canonical names compare after whitespace normalization and case folding.
Meaningful scientific authorship/name differences are retained. Canonical names are now
stored separately. Unchanged records advance check timestamps without unnecessary content
updates or changed-content timestamps.

A last-applied scientific-name marker records ownership only for new importer-created
Species. Automatic rename requires a matching current local value, no manual flag, no
competing ownership/collision, and safe taxonomy state. Legacy unknown ownership, curated
values and subsequent local edits are preserved and conflicts recorded. A newly attached
reference does not gain permission to rename an existing Species.

Previous canonical/local names become historical aliases when representable. Synonym and
deprecated names can become attributed aliases. Local aliases remain untouched. Taxonomic
status and accepted-taxon changes update external metadata while preserving internal
status, Species identity and Animal links. Ambiguous taxonomy is recorded rather than
merged, deleted, or reassigned. Accepted relationships use bounded local lookup only.

Common-name additions retain language and source. Missing/changed previously recorded
provider names generate review issues and remain stored. Preferred common name and AnimalGroup
are never replaced. Duplicate names/aliases are suppressed and database constraints remain
in force. Exact HTTP 404, temporary provider failure, malformed records and identity/dataset
mismatches receive different handling. A missing external taxon never deletes Species.

## Persistence, history and issues

Apply `AddCatalogSynchronizationProgress` before running the updated worker. Howell never
applies migrations itself. Tests applied it only to disposable PostgreSQL databases.

New reference fields: `ExternalCanonicalName`, `LastAppliedScientificName`.
New run fields: `Scope`, `RetryOfRunId`, `ReferenceIds`, `LastCommittedReferenceId`,
`CheckpointComplete`, `CompletionReason`, `RecordsUnchanged`, `IssuesGenerated`,
`HttpRequests`, `HttpRetries`. `Partial` extends the status enum. Duration remains derivable
from existing start/completion timestamps. Failed finalization increments the failure count;
rolled-back catalog changes never inflate committed processed/updated counts.

`Fingerprint` plus a filtered unique index prevents duplicate open/investigating issues.
Fingerprints use selected identity/condition fields, not entire response serialization.
Issues retain original history association and resolution state. Resolved/ignored issues
are preserved; recurrence can create a new open issue. Missing local Species is allowed.
Metrics count newly generated issues rather than repeated encounters.

Dry-run policy is unchanged: no persisted history or catalog changes. READ ONLY PostgreSQL
transactions enforce this. Proposed changes/issues are counted and mode-labelled in logs;
production checkpoints and issue-resolution state are untouched.

## Recovery

Sync persists a bounded ordered list of reference IDs and resumes after the last committed
ID. Retry verifies provider, operation, dataset, stored limit and checkpoint shape under the
same lock. It creates linked history without rewriting previous failures. A lost/earlier
checkpoint can be safely replayed through idempotent writes. New retry counters describe
only work performed by that retry.

Import retries replay the original bounded scope from the beginning; there is no external
offset checkpoint claim. Failed transactions roll back together; previous batches remain.
Budget exhaustion is Partial, unexpected failure is Failed, cancellation is Cancelled.
Interrupted Running executions are recovered only after obtaining the same database lock.
Closing/killing the worker releases its physical session lock. A database outage can prevent
finalization, leaving history recoverable by a later worker. No in-memory-only concurrency
mechanism was added.

## Weekly execution compatibility

An external scheduler can eventually run:

```sh
dotnet /app/howell-sync.dll sync --provider gbif --limit 100
```

Environment configuration, readable retained logs, deterministic exit codes, finite request bounds, and
generic-host SIGTERM handling support noninteractive execution. No timer or schedule is
installed. Request/attempt/wait limits bound runtime; there is no separate whole-run timeout.

## Executed verification

| Check | Result | Evidence |
|---|---|---|
| Howell Sync standalone build | PASS | `dotnet build apps/howell-sync/howell-sync.csproj --no-restore -m:1 -p:UseSharedCompilation=false`; zero warnings/errors |
| Skipper standalone build | PASS | Same build flags on `apps/skipper-api/skipper-api.csproj`; zero warnings/errors |
| Full backend PostgreSQL integration suite | PASS | 74 passed, 0 failed, 0 skipped |
| Synchronization / change detection | PASS | Real PostgreSQL with controlled HTTP responses |
| Manual override / edited / unknown ownership | PASS | Local fields, aliases, competing-provider ownership preserved |
| Idempotency | PASS | Stable IDs, unchanged counts/timestamps, names/aliases/issues deduplicated |
| Checkpoint / rollback / replay | PASS | Atomic rollback, retained committed batch, lost-checkpoint replay |
| Retry / partial / stale recovery | PASS | Exact sync scope resume, import replay, rejected incompatible retries, abandoned-run recovery |
| Concurrency / process crash | PASS | Existing independent-worker/process tests plus retry lock contention |
| Dry run | PASS | Catalog/history/checkpoints/resolution state preserved, including dry-run retry |
| Migration upgrade/downgrade | PASS | Existing and new migration coverage; no pending model changes |
| Existing Animal relationships / Skipper access | PASS | Animal service returns original SpeciesId after taxonomy changes |
| Backend unit suite | NOT RUN | Execution permission request was declined; added CLI unit cases remain unexecuted |
| CLI help smoke test | PASS | Built worker displays sync/import/retry syntax and exit codes |
| Manual live GBIF synchronization | NOT RUN | No live provider/catalog mutation performed |
| Production execution / deployment | NOT RUN | Outside scope |

Full integration command:
`dotnet test tests/skipper/Integration/MaryAnn.Skipper.IntegrationTests.csproj --no-restore --logger 'console;verbosity=minimal'`.
Final integration output is in `/private/tmp/ticket5-complete-integration.log` on this workspace host.
Earlier migration-test failures were corrected before the final passing run. No live GBIF
access was needed for CI-style tests. The initial sandbox test attempt could not create
.NET IPC sockets; approved integration runs executed outside that restriction.

## Known limitations

- The inherited v1 Backbone dataset is frozen compatibility data. COL XR and new providers
  remain unsupported; a weekly invocation does not guarantee new upstream taxa.
- Fresh sync invocations start at the lowest reference ID; they do not rotate an oversized
  catalog automatically. Use suitable bounds/budgets or explicit existing-taxon scopes.
- Only bounded existing-reference sync and explicit import scopes are supported. No general
  hierarchy traversal, all-provider sync, per-Species CLI filter, or record-level retry mode.
- Import search replay is not a stable external snapshot. Offset resume and exhaustive synonym
  discovery are not claimed.
- Legacy provenance remains unknown. Operators must review conflicts; no automatic issue
  resolution or administration UI is included.
- Legacy issues without fingerprints are preserved and may gain one new equivalent issue.
- No name deletion or automatic preferred-name/group/status selection. Accepted-taxon
  conflicts need human review; there are no recursive accepted-taxon lookups.
- Metrics after a hard process/database failure can only reflect the last durable batch.
- Unit-suite execution and live-provider manual testing remain unverified as noted above.

## Follow-up dependencies

Ticket 6 must preserve stable Species/Animal identity when adding multiple variant
assignments. Ticket 7 can index local common names and aliases, without request-path GBIF
dependencies. Ticket 8 should operationalize migrations, issue review, budget/scope sizing,
scheduler configuration, licensing/citations, and future COL XR adapter migration.

Process note: an initial `git status` was inadvertently executed before the pasted ticket's
Git-inspection restriction was read. Subsequent inspection used files directly; no further
prohibited Git inspection, push, or PR-description work was performed.


## Logging follow-up — November 7, 2026

Replaced JSON console dumps with timestamped, single-line console/file output. Explicit
START, PROGRESS, ISSUE, COUNTS, FINISH, RETRY and LOGGING events expose run IDs, bounded
scope, proposed versus committed counts, timing, and exit status. Routine HTTP messages
are debug-only. New issue explanations are logged after their batch commits (or after a
dry-run batch is evaluated), without dumping raw provider responses or exception payloads.

Each non-help invocation opens a unique, auto-flushed file under `logs/howell-sync/`
(relative to the working directory) and prints its absolute path. Segments rotate at
10 MiB; matching files older than 30 days are cleaned up at startup. Directory, retention
and size are configurable through `HowellLog`. Unix files are created with owner-only
read/write permissions. Containers need a mounted directory for persistence across removal.
Initialization failure stops startup; a later file-write failure explicitly warns and
continues console-only. PostgreSQL history is unchanged.

Additional created files:

- `apps/howell-sync/Logging/OperationLogProvider.cs`
- `tests/skipper/Unit/HowellLoggingTests.cs`

Additional modified files: `apps/howell-sync/Program.cs`, `apps/howell-sync/appsettings.json`.
Logging changes also update the existing runner, context, GBIF client/provider/reconciler,
README and this report. No new dependencies or database changes were needed.

Follow-up verification:

- PASS — Howell build (zero warnings/errors).
- PASS — all 60 Howell-filtered unit tests, including CLI and logging tests.
- PASS — all 55 Howell-filtered PostgreSQL integration tests.
- PASS — actual CLI configuration-failure smoke test, expected exit 2, with matching
  console/file output and owner-only file permissions; explicitly empty connection string,
  no database contacted.
- NOT RUN — live GBIF import for this logging follow-up; the displayed README success
  example is illustrative.

The earlier report's unit-suite NOT RUN entry records the original implementation session.
The Howell subset has now been executed successfully; the entire backend unit suite was
not rerun as part of this logging change.
