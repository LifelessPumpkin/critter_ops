# Howell Sync

Howell Sync is a standalone .NET 10 Generic Host console application for maintaining
CritterOps' animal reference catalog in Professor PostgreSQL. It exposes no HTTP API.
Skipper serves animal management and reference APIs; normal animal management does
not depend on Howell Sync being available. No provider, external download, scheduler,
container deployment, or additional database is introduced in this ticket.

## Local execution

Install the .NET 10 SDK. Provision the existing Professor PostgreSQL database and
apply its migrations using [Skipper's instructions](../skipper-api/README.md#ef-core-migrations).
Howell never creates a database or applies migrations on startup. No Docker Compose
setup is required by the worker. Tests use their own disposable PostgreSQL containers.

From the repository root:

```sh
dotnet build apps/howell-sync
dotnet run --project apps/howell-sync -- help
# Set this with your own local values through your shell/secret manager:
export ConnectionStrings__ProfessorDb='Host=<host>;Database=<database>;Username=<user>;Password=<password>'
dotnet run --project apps/howell-sync -- validate
dotnet run --project apps/howell-sync -- validate --dry-run
dotnet run --project apps/howell-sync -- import --provider gbif
dotnet run --project apps/howell-sync -- sync --provider gbif --dry-run
```

The last two commands fail with exit 2: GBIF is not implemented. They perform no
import, create no run, and do not connect to Professor. No production no-op provider
is registered. Supported commands are `help`, `import`, `sync`, and `validate`;
`--help` is accepted for each operation. Import/sync require `--provider <name>`.
`--dry-run` is supported; validate is always read-only and rejects `--provider`.
Unknown commands, unknown/repeated options, and missing values exit 2.

| Exit | Meaning |
|---|---|
| 0 | Operation completed (possibly with recorded issues) |
| 1 | Database, execution, or execution-history failure |
| 2 | Invalid arguments/configuration or unsupported provider/operation |
| 3 | Another catalog operation holds the lock |
| 130 | Cancellation |

JSON settings are loaded from the executable directory (`appsettings.json` is copied
on build/publish), followed by standard Generic Host environment overrides. Use
`DOTNET_ENVIRONMENT` for environment-specific JSON and `Providers__<slug>__<setting>`
for provider overrides. No future provider settings are required at startup.
Command-line options select the operation/provider and dry-run mode; secrets belong
in configuration, not command arguments. The checked-in connection string is empty.

## Structure and provider registration

- `Program.cs`: CLI entry, Generic Host lifecycle, JSON logging, DI composition.
- `Commands/CatalogCommand.cs`: validated CLI model and operation dispatch values.
- `Providers/ICatalogProvider.cs`: provider name, supported operations, selected
  configuration validation, and asynchronous import/sync dispatch; DI registry.
- `Execution/CatalogOperationRunner.cs`: shared lifecycle and read-only validation.
- `Execution/CatalogExecutionContext.cs`: bounded transactions, counts, issue recording.
- `Persistence/`: session coordination and execution-history repository.
- `../../packages/professor-persistence`: the single ProfessorDbContext, all domain
  models, EF mappings, seed definitions, migrations, and model snapshot. Existing
  `skipper_api` namespaces remain for compatibility; Howell does not reference Skipper.

Register a future `ICatalogProvider` with `builder.Services.AddSingleton<ICatalogProvider,
YourProvider>()` (or a scoped lifetime; the registry and runner are scoped).
The registry resolves names without provider-specific branching in Program. Provider
names must be canonical lowercase slugs of at most 100 characters. Each provider
validates only its own `Providers:<slug>` section and receives the selected operation,
execution context, and cancellation token. Providers can add typed URL, user agent,
timeout, rate limit, request budget, and batch options later; no GBIF settings or HTTP
client are implemented now. Provider configuration errors must never include secrets.

The runner/repository are scoped per invocation; the session factory is stateless and
singleton. DbContexts are created and disposed per history operation or batch against
the exclusively owned session. Do not run batches concurrently, dispose the supplied
connection, replace connections, enable reconnect/retry policies, or commit the
framework-owned transaction from a provider. Providers are trusted application code,
not sandboxed plugins, and must use the supplied execution context for persistence.

## Lifecycle, locking, and recovery

Parse → resolve provider → validate selected configuration → acquire catalog lock →
recover abandoned runs → create Running history → execute bounded batches → finalize
history → close the lock session. Unsupported requests fail before database access.

`CatalogSession.LockKey` (`0x4352495454434154`) is one database-wide PostgreSQL
session advisory lock shared by all Howell import, sync, and validation operations.
`pg_try_advisory_lock` is atomic and nonblocking. Every future catalog writer must
cooperate with this same key; ordinary Skipper requests do not acquire it. Use a direct
PostgreSQL connection or session pooling proxy, never a transaction-pooling proxy.

A dedicated **nonpooled, nonmultiplexed** connection owns the lock and all operation
DbContexts. No replacement connection is allowed after failure. Closing the physical
session releases the lock on completion, error, or cancellation. PostgreSQL releases
locks and rolls back the active transaction after a killed process/disconnected
session is detected. Network partitions may delay detection; the worker does not
promise instantaneous failover and does not reconnect or retry indefinitely.

Once a non-dry import/sync owns the lock, all previous Running rows are marked Failed
with a completion timestamp and interruption diagnostic. An active cooperating worker
cannot be marked stale because it still holds the lock. Validation/dry runs never
recover history because they perform no persistent writes. A database outage may
prevent finalization; the error is logged and the next writing invocation recovers the
Running row. No provider-specific retry, checkpoint resumption, or whole-import
atomicity is claimed. Previously committed batches remain after failure or crash.

## Transactions, history, and dry runs

Fetch external data **before** calling `BatchAsync`. Its callback handles one bounded
unit of database work. Each call creates a fresh ProfessorDbContext and transaction;
SaveChanges, cumulative processing counts, and the catalog writes commit together.
Exceptions/cancellation roll back the entire active batch, including writes saved
inside the callback. Earlier batches remain committed. Return nonnegative counts;
counts for failed batches are not counted as committed work. No external HTTP work
should occur inside a batch transaction.

Normal supported operations create CatalogSyncRun using existing operation/status
enums. Completion, failure, cancellation, timestamps, provider identity, and committed
counts use the existing table. A successful run with persisted issues becomes
CompletedWithIssues. `RecordIssueAsync` persists a sanitized provider-owned diagnostic
linked to the run; providers may also add issues in the catalog batch when they need
atomic association. Never pass credentials, raw external records, or raw exceptions
into issue descriptions. No migration or additional table is needed.

Dry runs persist **nothing**, including no execution history, stale-run updates, or
issues. Logs identify `DryRun=true` with an ephemeral execution identifier. Providers
must branch on `context.DryRun` and query/evaluate planned changes without calling
SaveChanges. Every dry-run batch executes `SET TRANSACTION READ ONLY`, which rejects
accidental EF or raw SQL writes, then rolls back. Planned counts can be returned for
logging. A provider attempting writes fails; it is never reported as a successful
simulation. `RecordIssueAsync` suppresses persistence during dry runs.

`validate` checks mapped columns in Species, AnimalGroups, AnimalVariants,
SpeciesExternalReferences, CatalogSyncRuns, and CatalogSyncIssues using bounded
one-row queries in a read-only transaction. It does not scan all records, migrate,
repair, or create execution history.

## Logs and shutdown

JSON console logs carry operation, provider, execution ID, dry-run flag, UTC start
and completion times, duration, processing counts, outcome, and sanitized failure
class. Exception messages/configuration values are intentionally excluded to prevent
secret leakage. Ctrl+C, SIGTERM, and Generic Host shutdown cancel the operation token.
Pass the supplied token through provider work and database calls. History finalization
uses a separate 10-second cleanup token so cancellation can be recorded, then the
physical connection is disposed. Providers must cooperate with cancellation; forcing
termination relies on PostgreSQL rollback/session cleanup and next-run recovery.

## Verification and follow-ups

```sh
./packages/mary-ann-tests/scripts/test-skipper.sh unit
./packages/mary-ann-tests/scripts/test-skipper.sh integration
```

Howell tests live in the existing Mary Ann xUnit suites. Integration tests need Docker
and use real PostgreSQL, including a separate test-only executable entry point that
holds a transaction so the parent test can kill its process. This provider is never
registered with or shipped in the production worker. CI builds Howell independently
and runs both suites. Run suites sequentially when they share build outputs.

Ticket 4 adds GBIF and provider-specific idempotency/configuration using this framework.
Ticket 5 adds scheduling and production execution policy around the CLI/exit codes.
Tickets 6 and 7 consume the shared persistence for variant assignments/search without
adding worker dependencies to API requests. Ticket 8 expands catalog operations,
recovery, and provider runbooks after real provider behavior is defined.
