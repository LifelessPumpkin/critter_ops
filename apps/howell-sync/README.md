# Howell Sync

Howell Sync is the standalone .NET 10 catalog worker. GBIF data flows through Howell
into the existing Professor PostgreSQL catalog; Skipper and Gilligan continue serving
local data without a dependency on live GBIF. Nothing imports on startup. There is no
scheduler, new database, breed/morph importer, or public worker API.

## Run a small development import

Apply Professor migrations using [Skipper's instructions](../skipper-api/README.md#ef-core-migrations).
Howell does not create databases or apply migrations. Configure your **development**
connection and your real project contact; neither is supplied in the repository:

```sh
export ConnectionStrings__ProfessorDb='Host=<host>;Database=<dev-database>;Username=<user>;Password=<password>'
export Providers__gbif__UserAgent='CritterOps/1.0 (+https://<your-real-project-contact-url>)'
dotnet run --project apps/howell-sync -- validate
dotnet run --project apps/howell-sync -- import --provider gbif --query 'Python regius' --limit 5 --dry-run
dotnet run --project apps/howell-sync -- import --provider gbif --query 'Python regius' --limit 5
# A numeric v1 GBIF name-usage key, not a Species.Id or COL XR identifier:
dotnet run --project apps/howell-sync -- import --provider gbif --taxon <usage-key> --limit 1
# Explicit, bounded refresh; this does not schedule future executions:
dotnet run --project apps/howell-sync -- sync --provider gbif --query 'Python regius' --limit 5
```

Replace placeholders before running. Do not point a development import at production.
`help` and `validate` require no GBIF configuration. Validate is always read-only and
accepts no provider/scope. The selected provider validates configuration and scope
before acquiring a database connection. Missing contact configuration exits 2.

| Exit | Meaning |
|---|---|
| 0 | Requested bounded scope evaluated, possibly with recorded record-level issues |
| 1 | Failed/incomplete operation, request budget exhausted, or history failure; committed batches may remain |
| 2 | Invalid arguments/configuration or unsupported provider/operation |
| 3 | Another catalog operation holds the lock |
| 130 | Cancelled; committed batches may remain |

## API and dataset strategy

Official documentation reviewed on **2026-10-25**, starting at
[GBIF technical documentation](https://techdocs.gbif.org/):

- [Species API and schema](https://techdocs.gbif.org/en/openapi/v1/species), including the
  [published OpenAPI JSON](https://techdocs.gbif.org/openapi/checklistbank.json).
- [API usage, pagination and rate guidance](https://techdocs.gbif.org/en/openapi/)
  (the old [developer summary](https://www.gbif.org/developer/summary) redirects here).
- [Taxonomy interpretation](https://techdocs.gbif.org/en/data-processing/taxonomy-interpretation).
- [GBIF's COL XR migration guide](https://data-blog.gbif.org/post/catalogue-of-life-taxonomic-backbone/).
- [Occurrence downloads](https://techdocs.gbif.org/en/data-use/api-downloads), reviewed
  to distinguish them from taxonomy lookups; this worker does not use them.

GBIF's primary current taxonomy is **Catalogue of Life eXtended Release (COL XR)**,
with checklist key `7ddf754f-d193-4cc9-b351-99906754a03b`. The original Backbone is
frozen and supported for backwards compatibility. It is not the preferred current
taxonomy. The migration guide explains the distinction between numeric Backbone keys
and alphanumeric COL XR identifiers, and the limitations of legacy lookup/usage APIs.

This first adapter deliberately implements the documented **v1 checklist name-usage
API**, with dataset `d7dddbf4-2cf0-4f39-9b2a-bb099caae36c` (Backbone) as an explicit
compatibility default. These endpoints provide the bounded search, full usage,
accepted relationships and vernacular pagination needed here. The implementation does
not reinterpret v2 matching results as v1 name usages or perform automatic fuzzy merges.
A future COL XR adapter strategy needs its own verified retrieval and identity mapping;
changing a UUID alone is insufficient. Configuration explicitly rejects the COL XR
checklist key. Other v1-compatible checklist UUIDs can be configured, but operators
must verify their availability and licensing. No fallback dataset is silently selected.

Endpoints relative to `https://api.gbif.org/v1/`:

| Endpoint | Meaning |
|---|---|
| `species/search?q=...&datasetKey=...&offset=...&limit=...` | Relevance-ordered full-text search, including names/classification; **not descendants** |
| `species/{usageKey}` | Exact numeric usage lookup; also hydrates species search results because search projections need not contain canonical names/deletion metadata |
| `species/{usageKey}/vernacularNames?offset=...&limit=...` | Paginated common names |

No web scraping, occurrence calls, undocumented endpoints, or automatic whole-taxonomy
downloads are used. Exact lookups must return the requested key and configured dataset.
Missing dataset metadata is rejected with an issue rather than fabricated.

## Configuration and HTTP behavior

Settings use the existing `Providers:gbif` section, not a second provider registry or
configuration system. Environment overrides use `Providers__gbif__<Setting>`.
JSON loads from the executable directory, followed by Generic Host environment
configuration. Connection strings and contact values are not included in worker errors.

| Setting | Default | Validation/purpose |
|---|---|---|
| BaseUrl | `https://api.gbif.org/v1/` | HTTPS, absolute, trailing slash; no credentials, query or fragment |
| UserAgent | empty | Required for GBIF; identifies CritterOps and includes operator-supplied HTTPS project URL or contact address; no invented contact |
| RequestTimeout | `00:00:30` | Positive, at most five minutes, per HTTP attempt including body download |
| MaximumConcurrentRequests | 1 | 1–8; shared by all endpoints and retries in an invocation |
| MinimumRequestInterval | `00:00:01` | 0–60 seconds between request starts; asynchronous and cancellable |
| MaximumRequestsPerRun | 100 | 1–10,000 actual attempts, **including retries** |
| MaximumRetries | 3 | 0–10 retries per request, in addition to the first attempt |
| InitialRetryDelay | `00:00:02` | Positive exponential-backoff base |
| MaximumRetryDelay | `00:01:00` | At least initial delay, at most five minutes |
| PageSize | 20 | 1–100 taxonomy or vernacular results per page |
| DefaultImportLimit | 20 | 1–1,000 taxonomy records from the requested scope |
| DatasetIdentifier | Backbone UUID above | Required valid v1 checklist UUID; COL XR explicitly unsupported |

These are application safeguards, **not GBIF-approved request rates**. GBIF gives no
fixed guaranteed query rate and recommends an identifiable User-Agent. Operators are
responsible for providing a real, reachable contact. Syntax checks cannot prove ownership.

HttpClientFactory supplies a named client, with redirects disabled and a 4 MiB response
buffer limit. Requests use only locally constructed relative paths and validated numeric
keys; provider-supplied URLs are never followed. Every attempt passes the same concurrency,
pacing and budget gates. Processing is sequential; raising concurrency does not introduce
parallel record processing.

HTTP 429, 500, 502, 503 and 504, transport failures and attempt timeouts can retry. Delays
are `InitialRetryDelay × 2^retry × (1 + jitter)`, capped by MaximumRetryDelay, with random
jitter in [0,1). Retry-After seconds and HTTP dates are honored as a lower bound, including
on 503; a value above the maximum waiting policy stops safely. A shared cooldown prevents
subsequent requests bypassing the wait. Permanent HTTP errors (including 400/401/403/404),
invalid JSON and invalid identifiers are not retried. Cancellation never retries.
Throttling counts are logged and a recovered run records a ProviderThrottled issue.

## Scope, limits and pagination

Exactly one of `--query` or `--taxon` is required. `--query` takes nonempty text up to
300 characters. `--taxon` takes one positive integer usage key and processes exactly
one scope record; it does **not** fetch descendants. `--limit` is 1–1,000, defaulting
to DefaultImportLimit, and means **maximum taxonomy records processed from the scope**,
including skipped records. It does not mean Species created, HTTP requests, or total
common names/aliases. Names have no separate CLI limit, but their pagination consumes
the same finite HTTP budget. No accepted-taxon chain is fetched.

Search requests shrink to the remaining scope limit. Pagination validates offset,
limit, results and endOfRecords; offsets advance by returned record count. Empty
non-final pages, repeated taxonomy keys, and repeated vernacular pages stop with an
issue. Short final pages and empty final results are supported. The worker stops at
endOfRecords or the configured scope limit, whichever comes first. A successful limited
search claims only that bounded scope, never a complete genus or taxonomy import.

Budget exhaustion stops the run as **Failed**, records RequestBudgetExhausted, returns
1 and retains only committed counts. This uses the existing status model; there is no
misleading successful partial-completion state. Names are fully fetched before a taxon
batch begins, so exhaustion during name fetching does not leave a partially linked taxon.

## Mapping and curation

Only non-deleted `SPECIES` records with status `ACCEPTED` create canonical Species.
Subspecies and all other ranks are skipped. Supported synonym statuses are SYNONYM,
HOMOTYPIC_SYNONYM and HETEROTYPIC_SYNONYM. DOUBTFUL, MISAPPLIED, PROPARTE_SYNONYM and
unknown statuses are recorded for review, not automatically promoted.

Whitespace is trimmed/collapsed and matching is case-insensitive. The provider's full
scientificName, including authorship/qualifiers, remains in the external reference.
When available, GBIF's canonicalName supplies the local ScientificName and an additional
exact match candidate. Authors and qualifiers are never stripped heuristically. A missing
canonical name falls back to the full scientific name within the local 100-character bound.

All groups require `kingdom=Animalia`. Existing active AnimalGroups are resolved by name;
no IDs or groups are created/changed:

| Classification | AnimalGroup |
|---|---|
| Mammalia | Mammal |
| Aves | Bird |
| Reptilia | Reptile |
| Amphibia | Amphibian |
| Actinopterygii, Chondrichthyes, Dipnoi, Coelacanthi, Myxini, Cephalaspidomorphi | Fish |
| Phylum Arthropoda, Mollusca, Annelida, Cnidaria, Echinodermata, Porifera, Nematoda, Platyhelminthes | Invertebrate |

These exact, deliberately narrow business mappings are not assertions that Fish or
Invertebrate is a formal taxonomic rank. Unrecognized classification is skipped with
an issue; there is no Other fallback. Additional classifications require reviewed rules.

Reconciliation first uses `(provider, dataset, external key)`, then uniquely matching
normalized scientific/canonical names with compatible group. Ambiguity or group conflict
produces an issue. No fuzzy/common-name matching, automatic merges, deletions or external
reference reassignment occurs. Seeded Species IDs and manually curated names/groups stay
unchanged. New Species use database-generated internal IDs and IsManuallyManaged=false.

The preferred CommonName is never selected or overwritten. Common-name rows preserve
language and source citation when representable. Source is `gbif:<dataset>` plus the
provider citation where present; entries exceeding schema bounds are skipped with issues.
Names deduplicate by normalized name/language/source, aliases by name/type/source, and
external references by provider/dataset/key, backed by existing database constraints.

A synonym attaches only to a locally resolved ACCEPTED reference in the same dataset.
Its full scientific name becomes a provider-attributed synonym alias and its own external
reference retains acceptedKey/status. If encountered first, an issue retains the numeric
accepted key; import the accepted usage explicitly and restart. No recursive lookup or
invented relationship occurs. Oversized alias names generate an issue without truncation.

Provider renames refresh external metadata. Automatically managed Species can rename when
no other local match conflicts, retaining the previous name as a historical alias. Curated
names are preserved and conflicts recorded. Existing reference status/rank/deletion changes
refresh external metadata but retain the link and create TaxonomyChanged issues. Subsequent
runs remain blocked for that usage while an open/investigating TaxonomyChanged issue exists;
resolving/ignoring it is an explicit operator decision outside this ticket. Species are never
deleted or deactivated automatically. Existing manually authored aliases/names are not edited.

## Provenance and execution history

| GBIF field | Professor representation |
|---|---|
| key | SpeciesExternalReference.ExternalIdentifier (string), never Species.Id |
| datasetKey | ExternalReference.DatasetIdentifier; run stores the configured dataset |
| scientificName | ExternalScientificName, unmodified provider value |
| taxonomicStatus | ExternalTaxonomicStatus |
| acceptedKey | ExternalAcceptedIdentifier |
| vernacularName / language / source | SpeciesCommonName.Name / LanguageCode / Source |
| local observation time | LastCheckedAt, CreatedAt, UpdatedAt; not fabricated provider modification time |

Provider slug is `gbif`. ParentKey is parsed but no hierarchy is persisted. Constituent
checklists, provider modification dates, full citations/license documents and taxonID are
not added to the current schema. Missing source citations remain provider/dataset-only
attribution. Run counts describe committed processed/created/updated/skipped work. Updated
means an existing associated Species was evaluated and its reference metadata refreshed,
not necessarily a change to its local name. Invalid/conflicting records are skipped with
issues; rolled-back batches do not increment processed/failed counters. Fatal failures and
partial progress are represented by Failed plus existing counts/issues/error message.

## Transactions, dry runs and recovery

The existing ICatalogProvider/ProviderRegistry, CatalogOperationRunner, ProfessorDbContext,
CatalogSession and CatalogRunRepository are reused. No new execution framework or migration
is required. One scoped provider uses one per-operation HTTP client wrapper and reconciler.
The generic execution context carries the parsed command; provider validation owns its scope.

The catalog-wide PostgreSQL advisory lock (`0x4352495454434154`) is held by a dedicated,
nonpooled, nonmultiplexed physical connection. Use direct PostgreSQL or session pooling,
not transaction pooling. Every catalog writer must cooperate with the lock. Contexts cannot
reconnect after lock loss. Ordinary Skipper reads do not acquire this lock.

HTTP and vernacular pagination complete outside transactions. Each taxon is one bounded
batch: Species, references, names, aliases, issues and cumulative run counts commit together.
A failed/cancelled batch rolls back; prior batches remain. Database uniqueness conflicts
fail the batch rather than reassigning references; an idempotent restart rereads persisted
state. No automatic database retry changes ownership or hides a conflict.

Dry runs fetch the same bounded data and evaluate matching/conflicts, with a per-run proposed
state to avoid counting the same proposed Species repeatedly. They log proposed counts and
issues, and persist **nothing**, including no history. Batch transactions are PostgreSQL
READ ONLY and rolled back. SaveChanges is not called in dry runs.

Cancellation propagates through HTTP, gates, waits, records and EF. Finalization uses a separate
10-second cleanup token and records Cancelled where possible. Closing the lock connection
releases it. Interrupted Running rows are marked Failed by the next non-dry worker under the
same lock; database/network failures may prevent immediate history finalization.

There is **no durable offset/checkpoint resume**. Small bounded scopes restart from the beginning,
using persisted identities and uniqueness constraints for idempotency. Search ordering/data can
change between runs, so a restarted text search is not a snapshot continuation. Operators retain
the original scope/limit in their invocation/job configuration; the run schema does not store it.
A synonym skipped before its accepted record may need another invocation. There is no claim of
complete synonym coverage or whole-import atomicity.

## Attribution and licensing

Consult [GBIF terms](https://www.gbif.org/terms),
[checklist publishing guidance](https://ipt.gbif.org/manual/en/ipt/latest/best-practices-checklists)
and the selected dataset's registry metadata/license before redistribution. Dataset licences
and citation requirements vary; provider/dataset IDs and available name citations are retained,
but do not replace a complete dataset citation or licence review. This worker does not assume one
licence covers every GBIF checklist. GBIF supplies taxonomy, not comprehensive breed/morph data.

## Verification and follow-ups

```sh
dotnet build apps/howell-sync/howell-sync.csproj
dotnet build apps/skipper-api/skipper-api.csproj
dotnet test tests/skipper/Unit/MaryAnn.Skipper.UnitTests.csproj
dotnet test tests/skipper/Integration/MaryAnn.Skipper.IntegrationTests.csproj
```

Run suites sequentially because they share build outputs. HTTP tests use controlled handlers
and virtual time; persistence tests use disposable real PostgreSQL containers. CI never needs
live GBIF or contact credentials. Existing Howell tests cover transaction cancellation, process
crashes, advisory-lock contention, abandoned-run recovery and database-enforced dry-run safety.

Ticket 5 can schedule bounded CLI invocations and interpret nonzero partial-failure exits;
it must supply a real contact, dataset policy and original scope. Ticket 6 should preserve
Species identity when adding variant assignments. Ticket 7 can consume local names/aliases
and references, with no GBIF calls in request paths. Ticket 8 should provide conflict review,
licensing/citation operations, recovery procedures and a future COL XR migration plan. Those
tickets are not implemented here.
