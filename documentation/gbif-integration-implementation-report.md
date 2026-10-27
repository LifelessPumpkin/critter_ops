# Ticket 4 — GBIF integration implementation report

Date: 2026-10-25. Implementation is confined to Howell Sync and its existing backend
test suites. No changes were pushed, and no pull request description was prepared.

## Files changed

Created:

- `apps/howell-sync/Providers/Gbif/GbifOptions.cs`
- `apps/howell-sync/Providers/Gbif/GbifModels.cs`
- `apps/howell-sync/Providers/Gbif/GbifApiClient.cs`
- `apps/howell-sync/Providers/Gbif/GbifTaxonMapper.cs`
- `apps/howell-sync/Providers/Gbif/GbifSpeciesReconciler.cs`
- `apps/howell-sync/Providers/Gbif/GbifCatalogProvider.cs`
- `tests/skipper/Unit/GbifClientTests.cs`
- `tests/skipper/Integration/GbifImportTests.cs`
- `documentation/gbif-integration-implementation-report.md`

Modified:

- `apps/howell-sync/Commands/CatalogCommand.cs`
- `apps/howell-sync/Execution/CatalogExecutionContext.cs`
- `apps/howell-sync/Execution/CatalogOperationRunner.cs`
- `apps/howell-sync/Providers/ICatalogProvider.cs`
- `apps/howell-sync/Program.cs`
- `apps/howell-sync/howell-sync.csproj`
- `apps/howell-sync/packages.lock.json`
- `apps/howell-sync/appsettings.json`
- `apps/howell-sync/README.md`

No Professor schema, migration, seed IDs, Skipper HTTP behavior or Gilligan code was
changed. Source inspection was used; the prohibited Git inspection commands were not run.

## GBIF integration

GbifCatalogProvider implements the existing ICatalogProvider and registers as a scoped
`gbif` provider. Both import and explicitly bounded sync use the same reconciliation path.
The existing registry, runner, session lock, transaction callback, history repository and
ProfessorDbContext remain responsible for coordination and persistence.

GbifApiClient uses a named HttpClientFactory client. Configuration is validated only for
the selected provider. The existing `Providers:gbif` namespace supports HTTPS BaseUrl,
identifiable UserAgent, per-attempt timeout, concurrency, pacing, attempt budget, retries,
backoff bounds, page size, default import limit and dataset UUID. The shipped contact is
empty so an operator must supply a real project URL/contact. Redirects are disabled; body
buffering is capped at 4 MiB. No response-provided URL is followed.

The official documentation and published OpenAPI schema were reviewed. Current GBIF
recommendations favor COL XR, while the original Backbone remains a frozen compatibility
source. This implementation explicitly chooses the documented v1 checklist usage API
with Backbone UUID `d7dddbf4-2cf0-4f39-9b2a-bb099caae36c` as its compatibility default.
It does not claim this is the recommended current taxonomy. The COL XR UUID is rejected
because its alphanumeric identity/retrieval model is not interchangeable with this adapter.
Other v1-compatible datasets are configurable, subject to operator verification.

Endpoints: `species/search`, `species/{usageKey}` and
`species/{usageKey}/vernacularNames`. Species search results are hydrated through exact
lookups to obtain canonical names and deletion metadata. The runbook links the official
[Species API](https://techdocs.gbif.org/en/openapi/v1/species),
[taxonomy interpretation](https://techdocs.gbif.org/en/data-processing/taxonomy-interpretation),
[COL XR migration guide](https://data-blog.gbif.org/post/catalogue-of-life-taxonomic-backbone/),
API usage guidance, download documentation and licensing guidance.

## Import behavior

```sh
dotnet run --project apps/howell-sync -- import --provider gbif --query 'Python regius' --limit 5 --dry-run
dotnet run --project apps/howell-sync -- import --provider gbif --query 'Python regius' --limit 5
dotnet run --project apps/howell-sync -- import --provider gbif --taxon <numeric-usage-key> --limit 1
```

Exactly one scope is required. Query is full-text relevance search, not descendants.
Taxon is one exact numeric v1 usage. `--limit` bounds taxonomy records processed, including
skips, from 1 to 1,000 (default 20); it does not count HTTP attempts or names. Search page
sizes shrink to the remaining limit. Pagination validates returned offsets, limits,
endOfRecords and result sizes; repeated keys/pages and non-advancing empty pages fail.
No further pages are fetched after the limit or end marker.

Defaults: concurrency 1, request interval one second, 100 attempts/run, three retries,
30-second attempt timeout, two-second initial backoff and 60-second maximum wait.
All endpoint calls and retries share gates and budget. Retryable statuses are
429/500/502/503/504, plus transport failures/timeouts. Backoff is exponential with jitter;
Retry-After seconds/date are lower bounds. Excessive server waits fail rather than retrying
early. Permanent HTTP errors, invalid identifiers and malformed JSON do not retry.
These defaults are application policy, not published GBIF rate guarantees.

## Taxonomy mapping

Names trim/collapse whitespace and compare case-insensitively. Full provider scientific
names retain authorship in external metadata; canonicalName supplies a local name when
available. No heuristic qualifier removal or fuzzy merging is used. Only accepted,
non-deleted species-rank records create Species. Subspecies/higher ranks are not flattened.

Exact Animalia classification maps Mammalia/Aves/Reptilia/Amphibia to existing business
groups. An explicit class whitelist maps Fish; an explicit animal phylum whitelist maps
Invertebrate. The runbook enumerates these rules. Unknown classification produces issues,
with no Other fallback or new group creation.

Common names preserve language and provider/dataset/source attribution. Preferred local
CommonName is never overwritten. Oversized name/language/source fields are skipped with
issues. Synonyms become aliases and separate external references only when their accepted
usage already resolves safely within the same dataset. Unresolved accepted IDs are preserved
in issues; no recursive relationship fetch occurs. Import the accepted usage and restart.

Automatic renames preserve internal IDs and historical aliases. Curated scientific names,
groups, preferred common names and existing aliases remain protected. Provider status/rank/
deletion changes update external metadata while preserving links and recording TaxonomyChanged.
Unresolved taxonomy changes also block subsequent promotion and dependent synonym resolution.
No Species is automatically deleted, merged, deactivated or reassigned.

## Persistence

Matching prioritizes the existing provider/dataset/key reference. Otherwise uniquely
compatible normalized scientific/canonical names associate with existing Species, including
seeds; multiple or classification-incompatible matches become issues. New internal IDs are
generated by Professor, never taken from GBIF.

References retain provider `gbif`, returned dataset UUID, usage key, original scientific
name, raw status, accepted key and observation timestamps. Missing dataset identity is not
fabricated. Names, aliases and references deduplicate against existing rows, with current
PostgreSQL unique indexes as the final defense. Repeated imports refresh timestamps rather
than duplicating identities. Existing references for other providers are retained.

All HTTP for a record finishes before its transaction. One taxon, its names/aliases/references,
issues and cumulative progress commit together. A failed batch rolls back completely;
previous commits remain. The catalog advisory lock serializes cooperating writers. A uniqueness
conflict fails safely and can be retried by restarting the bounded operation after investigation.

## Synchronization history

CatalogSyncRun retains provider, configured dataset, operation, start/completion, status and
committed counts. Record conflicts use CatalogSyncIssue with dataset/external key where
available. Fatal provider diagnostics include safe reason codes, HTTP attempt/throttle counts
and available record identity; raw payloads/configuration secrets are not persisted.

Successful scopes with issues become CompletedWithIssues. Budget exhaustion or later-page
failure becomes Failed with actual committed progress and a stopping issue, exit 1. Cancellation
uses Cancelled and exit 130. The model has no partial-completion status; partial imports are
never labeled fully successful. Invalid/conflicting records count as skipped. Existing-record
updates include reference metadata refreshes; rolled-back batches do not contribute counts.

## Recovery and dry run

Cancellation covers HTTP, pacing, retry waits, pagination, records and EF. The active batch
rolls back, prior commits remain, bounded cleanup finalizes history where possible, and the
physical session closes/releases the advisory lock. Existing crash/abandoned-run recovery is
unchanged. If history finalization cannot reach PostgreSQL, the next writing worker recovers it.

No durable cursor is implemented. The supported recovery is an idempotent restart of the same
small bounded scope. Search order may change, so this is not snapshot continuation. Scope and
limit must remain in the operator's invocation/job configuration; the run schema does not store
them. Unresolved synonyms can require a second invocation after the accepted record is present.

Dry runs fetch data and evaluate proposed additions/updates/conflicts. Proposed entities remain
visible across read-only batches to avoid double counting. No catalog, history or issue writes
persist; PostgreSQL READ ONLY transactions enforce the boundary. Results are labeled dry-run
in logs and describe proposals, not imported data.

## Testing

All tests use existing Mary Ann suites. HTTP tests use controlled responses and virtual time;
integration tests use disposable real PostgreSQL 16 containers, not a development or production
catalog. Initial test-source compilation errors were corrected before the results below.

| Check | Result | Evidence |
|---|---|---|
| Howell Sync build | PASS | `dotnet build apps/howell-sync/howell-sync.csproj --no-restore`; zero warnings/errors |
| Skipper build | PASS | `dotnet build apps/skipper-api/skipper-api.csproj --no-restore`; zero warnings/errors |
| Existing backend unit tests | PASS | Full unit suite: 64 passed, zero failed/skipped |
| Existing backend integration tests | PASS | Full PostgreSQL suite: 57 passed, zero failed/skipped |
| HTTP client tests | PASS | 34 GBIF unit cases within the full unit suite |
| Rate limits/retries | PASS | 429; Retry-After seconds/date/absent/excessive; exponential jitter; retries and budget; transient/permanent failures |
| Timeout/cancellation/pacing/concurrency | PASS | Virtual timers and controlled HTTP handlers, including cancellation during HTTP, retry wait and pacing |
| Pagination | PASS | Empty/final/multiple pages, limit stop, later failures, repeated usage/name pages and malformed pagination |
| Species import | PASS | Accepted records, seeded name normalization, ambiguity, groups, invalid data, multiple names/languages and other-provider references |
| Synonyms/status changes | PASS | Deferred synonyms, accepted linkage, duplicate import, status transitions and blocked promotion/dependent synonym resolution |
| Rename | PASS | Stable Species IDs, historical alias, curated-name/group conflicts |
| PostgreSQL persistence | PASS | 29 GBIF integration cases within the full PostgreSQL suite |
| Transactions/recovery | PASS | Atomic uniqueness rollback, budget before name completion, committed progress, restart, cancellation and existing crash/lock tests |
| Dry run | PASS | Proposed additions/conflicts, cross-batch deduplication, no persisted catalog/history/issues and existing read-only enforcement tests |
| Executable CLI smoke checks | PASS | Help exits 0; missing scope/contact exit 2. The contact check stalled in the sandbox, was terminated, and passed outside the sandbox with a 20-second timeout |
| Manual live GBIF import | NOT RUN | No real project contact/User-Agent was supplied; fixtures use a clearly test-only contact. No live import or API readback is claimed |

## Known limitations

- The explicit Backbone compatibility default is frozen; COL XR import is not implemented.
  Dataset/API strategy needs renewed review before adopting a newer taxonomy.
- Only accepted Species and three unambiguous synonym statuses are supported. Subspecies,
  pro-parte/misapplied/doubtful records and unknown classifications require review.
- Synonym coverage is limited to the requested scope and already-resolved accepted usages;
  unresolved relationships are issues, not invented Species or automatic fetch chains.
- Conflict resolution is outside this ticket. Open TaxonomyChanged issues intentionally stop
  automatic reconciliation until reviewed/resolved/ignored.
- Restart is idempotent replay, with no durable offset or stable search snapshot.
- Provider fields outside the current neutral schema (constituent dataset, taxonID, hierarchy,
  provider modification dates, complete dataset licence/citation) are not persisted. Names
  with oversized citation metadata are skipped with an issue rather than truncated.
- The global lock protects cooperating Howell writers; it is not a general uniqueness rule
  on scientific names for arbitrary external database writers.
- No real live GBIF import, production import or live Skipper readback was performed.

## Follow-up dependencies

- **Ticket 5 — Automated Catalog Synchronization:** supply real contact, explicit dataset/scope,
  finite budgets and invocation recovery policy. Interpret exit 1 as possibly partially committed;
  account for frozen-Backbone limitations and unresolved-issue review. No scheduling added here.
- **Ticket 6 — Multiple Animal Variant Assignments:** preserve the stable internal Species IDs;
  this provider imports no breeds, morphs or genetics.
- **Ticket 7 — Catalog Search Improvements:** use local common names/aliases/reference metadata;
  do not introduce GBIF availability into Skipper/Gilligan request paths.
- **Ticket 8 — Catalog Documentation and Operations:** expand dataset attribution/licensing,
  conflict resolution and restart runbooks, and plan verified COL XR identity migration.

Operational details and configuration examples are in
[the Howell Sync runbook](../apps/howell-sync/README.md).
