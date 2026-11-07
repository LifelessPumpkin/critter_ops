# Howell Sync

Howell Sync is the standalone .NET 10 catalog worker. GBIF → Howell → Professor
PostgreSQL → Skipper → Gilligan. Animal management and API startup do not call GBIF.
The worker executes once and exits. No timer, scheduler, deployment, public API, or
separate database is included.

## Commands

Apply Professor migrations before running; Howell never applies them automatically.
Configure `ConnectionStrings__ProfessorDb` and
`Providers__gbif__UserAgent='CritterOps/1.0 (+https://<your-real-contact-url>)'`.
Use a development database for manual testing.

```sh
dotnet run --project apps/howell-sync -- validate
dotnet run --project apps/howell-sync -- sync --provider gbif --limit 100 --dry-run
dotnet run --project apps/howell-sync -- sync --provider gbif --limit 100
dotnet run --project apps/howell-sync -- sync --provider gbif --taxon 42
dotnet run --project apps/howell-sync -- sync --provider gbif --retry-run 123
# Controlled expansion via the existing importer:
dotnet run --project apps/howell-sync -- import --provider gbif --query 'Python regius' --limit 5 --dry-run
dotnet run --project apps/howell-sync -- import --provider gbif --taxon 42 --limit 1
dotnet run --project apps/howell-sync -- import --provider gbif --retry-run 124
```

`sync` refreshes existing GBIF references in the configured dataset, plus references
with missing dataset metadata (reported for review without guessing a dataset). It
never creates a Species or external reference. Optional `--taxon` restricts to an
existing numeric external identifier; it is not a Species ID. Processing uses reference
ID ascending, with a persisted selection of at most `--limit` references. Defaults are
bounded. A fresh invocation starts at the lowest matching reference ID, so a limit
smaller than the catalog does **not** rotate through the catalog weekly. Size the limit
and budget appropriately or use explicit existing-taxon scopes.

`import` performs controlled expansion and refreshes matching existing references through
the same change detector. Exactly one `--query` or `--taxon` is required. Search is a
bounded relevance-ordered text query, **not** a descendants traversal. An exact taxon
imports only that taxon. Neither command crawls Animalia. `sync --query` from the prior
import-style implementation is replaced by `import --query`.

`--limit` accepts 1–1000 and counts examined records, including skips. Search stops at
end-of-results or that limit. This is successful bounded completion, not a claim that
a taxonomy branch is complete. `validate` reads Professor mappings and writes nothing.
Unsupported providers and unsafe scopes are rejected. `help` needs no configuration.

| Exit | Meaning |
|---|---|
| 0 | Bounded scope complete, possibly with record-level review issues |
| 1 | Execution/database/provider failure; committed batches may remain |
| 2 | Invalid arguments/configuration/provider/retry scope |
| 3 | Another catalog operation owns the PostgreSQL execution lock |
| 4 | Partial: request budget exhausted before the requested scope completed |
| 130 | Cancelled; committed batches may remain |

## Configuration and provider protections

Environment overrides use `Providers__gbif__<Setting>`. Configuration is loaded from
the executable's appsettings.json followed by environment configuration. Only the selected
provider is validated. No connection string or raw provider error body is logged.

| Setting | Default | Bounds/purpose |
|---|---|---|
| BaseUrl | `https://api.gbif.org/v1/` | HTTPS base ending in `/`, no credentials/query/fragment |
| UserAgent | empty | Required CritterOps identity with real operator-supplied contact |
| DatasetIdentifier | `d7dddbf4-2cf0-4f39-9b2a-bb099caae36c` | Explicit v1 Backbone compatibility strategy |
| DefaultImportLimit | 20 | 1–1000; default record bound for both import and sync |
| PageSize | 20 | 1–100 external search/name results per page |
| MaximumRequestsPerRun | 100 | 1–10000 actual HTTP attempts, including retries |
| RequestTimeout | 00:00:30 | Positive, at most five minutes per attempt |
| MaximumConcurrentRequests | 1 | 1–8; current processing remains sequential |
| MinimumRequestInterval | 00:00:01 | 0–60 seconds between request starts |
| MaximumRetries | 3 | 0–10 per request |
| InitialRetryDelay | 00:00:02 | Positive exponential-backoff base |
| MaximumRetryDelay | 00:01:00 | At least initial delay; at most five minutes |

One API client per invocation shares pacing, request budget, concurrency gate, and
Retry-After cooldown across taxon, search, and common-name requests. HTTP 429, selected
5xx responses, transport errors, and attempt timeouts have bounded exponential backoff
and jitter. Retry-After above the permitted wait stops the operation. Redirects are
disabled; responses are capped at 4 MiB. Cancellation propagates through waits, requests,
and EF operations. Runtime is bounded by the record/request budgets and maximum waits;
there is no separate whole-run timeout setting.

This adapter uses the existing documented v1 numeric usage endpoints:

- `species/{key}`: exact lookup.
- `species/search?q=...&datasetKey=...&offset=...&limit=...`: bounded search.
- `species/{key}/vernacularNames?offset=...&limit=...`: fully paginated common names.

See the [GBIF species API](https://techdocs.gbif.org/en/openapi/v1/species),
[API guidance](https://techdocs.gbif.org/en/openapi/), and
[COL XR migration guide](https://data-blog.gbif.org/post/catalogue-of-life-taxonomic-backbone/).
The inherited Backbone default is a frozen compatibility dataset, not a promise of
weekly new taxa. COL XR identifiers are unsupported; its dataset UUID is explicitly
rejected. A future adapter must verify its identity and retrieval semantics. Merely
changing the dataset UUID cannot migrate existing references. No accepted-taxon chains
or synonym-list endpoint are fetched. Synonyms encountered in explicit imports and
changed reference statuses can add aliases; exhaustive synonym discovery is not claimed.

## Synchronization and ownership

`ICatalogProvider` retains provider retrieval and command configuration. GBIF maps its
DTOs into selected `CatalogTaxon` fields; `CatalogReferenceRefresher` performs generic
comparison and persistence within the existing batch lifecycle. The importer uses this
same refresher for already-linked records. No HTTP request occurs inside a catalog
transaction. New records still use `GbifSpeciesReconciler` and its exact-name/group
matching, ambiguity checks, and existing database uniqueness constraints.

`Species.Id` and `Animal.SpeciesId` remain stable. References are never reassigned,
Species never automatically merge/delete/deactivate, and external identifiers never
become internal IDs. Dataset/identifier mismatches retain the original reference and
create an issue. Unknown/malformed metadata is not fabricated.

Comparison collapses insignificant whitespace and compares names without case sensitivity.
Authorship and meaningful name differences remain significant. External scientific name,
canonical name, status and accepted identifier are stored as provider metadata. Raw name
values are preserved on meaningful updates; whitespace-only changes do not rewrite content.
`LastCheckedAt` and `LastSyncedAt` may advance without a content update. Content timestamps
and updated metrics remain unchanged for identical data.

Only new importer-created Species receive `LastAppliedScientificName` on their creating
reference. Automatic scientific renames require all of:

- `IsManuallyManaged=false`;
- a known last-applied value matching the current local scientific name;
- no competing provider ownership marker or local name collision;
- an accepted, species-rank, nondeleted taxon without an unresolved taxonomy-change issue.

Legacy imports and manually matched records have unknown field ownership and are preserved.
An imported value subsequently edited locally is preserved even when the manual flag was
not changed. An override is never cleared automatically. The preferred common name, group,
local status and manual aliases are not overwritten. Classification/name conflicts are
recorded. Older canonical names and safely replaced local names become historical aliases.
Deleted provider names can become deprecated aliases without deleting their Species.

Taxonomic status changes refresh external metadata and create `TaxonomyChanged` review
issues. Accepted-taxon changes create `AcceptedTaxonConflict` issues; the accepted relationship
is checked only against existing local references, with no recursive retrieval. Neither
change merges Species or reassigns Animals. An exact HTTP 404 becomes `MissingExternalRecord`;
a 5xx/transport error fails the operation, while malformed content or identity mismatches
are separately diagnosed. One 404 never invalidates local identity.

Common names retain language and provider/dataset plus citation attribution. New names are
added, duplicates suppressed, and old names preserved. Complete accepted-species responses
that omit previously recorded provider names produce `CommonNamesAbsent` issues; no deletion
is inferred. No automatic preferred-name selection occurs. Alias/name uniqueness is enforced
in PostgreSQL as well as application comparisons. Unknown alias ownership is retained.

## History, issues, dry runs and metrics

`CatalogSyncRun` stores provider, dataset, operation, serialized bounded command scope,
start/end/status, retry linkage, selected reference IDs, last committed reference ID, and
checkpoint completion. Metrics include processed, created, updated, unchanged, skipped,
failed, newly generated issues, actual HTTP attempts and HTTP retries. Duration is derived
from start/end timestamps. Failed execution finalization adds one failure count; rolled-back
record changes do not count as committed processed/updated records. Retry counts describe
HTTP retries; operation retries are represented by `RetryOfRunId` links.

Each record is classified as created, updated, unchanged or skipped. Missing/malformed
references are skipped with issues. Metadata changes count as updates even when local
curated values remain protected. Issues generated means **new issue rows**, not encounters
with an already-open issue. Logs include scope, run ID, mode, counts, duration and completion
reason. A record-limit boundary is successful; budget exhaustion is `Partial` with exit 4.
Unexpected failures are `Failed`, cancellation is `Cancelled`.

`CatalogSyncIssue` retains the originating execution and optional Species/reference IDs.
A SHA-256 fingerprint of selected identity/condition fields deduplicates open/investigating
issues across runs, backed by a filtered unique index. Existing issue resolution state,
creation time and run association are untouched. Resolved/ignored issues remain historical;
a later recurrence may create a new open issue. Legacy issues without a fingerprint remain
unchanged and can acquire one additional equivalent issue on first new detection. No issue
resolution UI or automatic resolution is included.

Dry runs perform retrieval and comparisons and report **proposed** counts/issues. The
inherited policy persists nothing: no history, catalog data, issues, resolution changes,
or production checkpoints. PostgreSQL READ ONLY transactions enforce this; proposed
changes are discarded and no SaveChanges is called. Dry-run retry also leaves its source
run untouched.

## Transactions, checkpoints and recovery

All operations, expansion and retry share the existing catalog-wide advisory lock
`0x4352495454434154` on a dedicated nonpooled physical PostgreSQL connection. Use direct
PostgreSQL or session pooling, not transaction pooling. Lock loss aborts work; contexts
cannot reconnect and silently lose exclusion. Skipper reads remain independent.

External data is fetched first. Each reference/taxon is one bounded transaction committing
catalog changes, issues, cumulative metrics, and the reference checkpoint together. A failed
batch rolls back; previous batches remain durable. Sync persists its bounded ID selection
before processing and checkpoints IDs in ascending order. A crash after commit safely resumes
after that ID. Replaying an earlier checkpoint is idempotent. This is not an exactly-once claim.

`--retry-run` accepts Failed, Partial, Cancelled, or abandoned Running executions with a
stored compatible scope. It validates provider, operation and configured dataset under the
lock. Scope/limit overrides are rejected. A new linked execution preserves the original
history. Sync resumes the stored reference list after its last committed ID; counters in the
new run describe only work done by that retry. Empty stored scopes remain empty. Import
replays the original bounded query/taxon scope from the start; there is no durable external
offset resume because search order/data may change. Individual-record retries and arbitrary
conflict re-evaluation are not separate modes. A normal explicit sync re-evaluates conflicts.

The next writing execution holding the same lock marks previous Running rows Failed before
starting. Lock ownership, not age, establishes abandonment. Checkpoints/history remain.
Cancellation/failure release the physical connection; finalization uses an independent
10-second token. If the database is unavailable, finalization may fail and leave Running
history for the next worker to recover. Dry runs and validate do not recover stale history.

## Future weekly execution

An external scheduler can invoke a prebuilt worker weekly with environment configuration:

```sh
dotnet /app/howell-sync.dll sync --provider gbif --limit 100
```

Supply a request budget sufficient for that scope and inspect nonzero exit codes and
persisted history. The generic host handles SIGTERM/Ctrl+C; JSON logs support noninteractive
container execution. Configure the external schedule and its timeout in a future infrastructure
ticket. This repository change neither configures nor deploys that scheduler.

## Verification and follow-ups

```sh
dotnet build apps/howell-sync/howell-sync.csproj
dotnet build apps/skipper-api/skipper-api.csproj
dotnet test tests/skipper/Unit/MaryAnn.Skipper.UnitTests.csproj
dotnet test tests/skipper/Integration/MaryAnn.Skipper.IntegrationTests.csproj
```

Tests use deterministic HTTP handlers and disposable PostgreSQL databases, not live GBIF.
Existing CI builds Howell/Skipper and executes these suites. Consult [GBIF terms](https://www.gbif.org/terms)
and dataset licensing/citation metadata before redistribution; stored provenance does not
replace licence review.

Ticket 6 must preserve Species identity while adding multiple variant assignments. Ticket 7
can search the local common names/aliases without provider calls. Ticket 8 should document
scheduler configuration, migrations, issue review, budget sizing, dataset licensing and future
COL XR migration. None of those tickets is implemented here.
