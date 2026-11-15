# Catalog search performance

Measured on November 14, 2026 using PostgreSQL 16-alpine in Docker Desktop on the local arm64 macOS development host (.NET SDK 10.0.301, Node 25.4.0), .NET 10/Npgsql, and the actual AnimalCatalogController SQL captured by a DbCommandInterceptor. Each size used a fresh disposable database with all Professor migrations. There are 16 seeded Species plus the indicated synthetic Species, one alternate name and one alias per synthetic Species, and the same number of synthetic variants assigned to Species 4 (a deliberately dense variant catalog). ANALYZE ran before measurement.

Times below are PostgreSQL EXPLAIN (ANALYZE, BUFFERS) execution milliseconds, one measured plan per query after an API execution warmed the query. These are representative observations, not p95 guarantees. HTTP latency, serialization and first EF compilation are excluded. Real GBIF was not used to build these fixtures.

| Query | 1,000 | 10,000 | 100,000 |
|---|---:|---:|---:|
| exact | 5.152 | 26.460 | 108.822 |
| partial | 7.319 | 42.544 | 18.622 |
| common | 9.883 | 47.902 | 10.432 |
| alternate | 6.342 | 60.536 | 118.800 |
| alias | 6.856 | 61.503 | 113.643 |
| category | 0.283 | 1.060 | 7.931 |
| combined | 8.061 | 52.242 | 13.224 |
| empty | 0.880 | 5.483 | 26.036 |
| no-match | 2.769 | 23.781 | 6.638 |
| offset | 2.475 | 17.240 | 60.140 |
| variant | 0.870 | 6.419 | 5.556 |

Every request uses pageSize 25 and returns at most 25 items. Exact returns one match; partial/common/alternate/alias each match 100 fixture names and return 25; category/combined use AnimalGroupId 3; empty returns 25; no-match returns zero. Offset uses page 1000 (offset 24975), empty for the two smaller datasets and 25 rows at 100000. Variant searches use SpeciesId 4 and 100 name matches (25 returned). Query text and actual row counts/index plans are retained in [full EXPLAIN output](catalog-search-plans.txt).

The original OR/correlated matching query measured approximately 489–697 ms at 100000 Species. Replacing candidate matching with a UNION of Species IDs removed the repeated per-Species alternate-name probes. A control measurement of the union query without the two new name indexes measured common-name/combined/no-match searches around 67–77 ms; adding the name indexes brought those cases to roughly 3–13 ms in the indexed runs. Some exact/alternate searches were slower with the planner's indexed plan (about 106–126 ms versus 75–80 ms in the control), but remained below the initial 200 ms guideline. This tradeoff is why only the missing name indexes were added, rather than every possible functional/composite index.

The final 100000-Species first-page samples meet the 200 ms database guideline and the dense Species-specific variant sample meets 100 ms. Very short/common substrings can still match most rows and require sorting many candidates; this is not a guarantee for arbitrary terms or colder/slower environments. Offset is bounded to 100000; keyset pagination is deferred because measured bounded offset behavior is adequate and ranking changes with the search term.

## Reproduce

```bash
CRITTEROPS_PERFORMANCE=1 \
CRITTEROPS_PERFORMANCE_OUTPUT=/tmp/critterops-performance.txt \
dotnet test tests/skipper/Integration --filter FullyQualifiedName~CatalogPerformanceTests
```

This opt-in test owns disposable Testcontainers databases and never inserts synthetic rows into the configured application database. Inspect actual rows, buffers, query shape and index names in the output. Ordinary integration runs do not execute the expensive fixture unless CRITTEROPS_PERFORMANCE=1.

## Real expanded catalog

After the real expanded run, Professor held 1553 Species (1500 qualify for expanded). The
same captured production query shape, executed with literal parameters through EXPLAIN
ANALYZE after ANALYZE, measured: exact `Python bivittatus` 1.279 ms (one row), alternate/common
`Burmese python` 1.082 ms (one row), `Drosophila` 2.102 ms (26 lookahead rows), and no-match
1.179 ms (zero rows). [Full real plans](catalog-real-plans.txt) include actual buffers, row counts
and index choices. No synthetic rows were inserted into this database.
