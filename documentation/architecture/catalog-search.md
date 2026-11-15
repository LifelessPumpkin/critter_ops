# Catalog search contracts

`GET /api/species?search=python&animalGroupId=3&page=1&pageSize=25` and
`GET /api/animal-variants?speciesId=4&search=albino&page=1&pageSize=25` return
`{ items, page, pageSize, hasMore }`. Species items contain `id`, nullable `commonName`,
`scientificName`, `animalGroupId`, `animalGroupName`, and nullable `preferredVariantType`.
Variant items contain `id`, `speciesId`, `name`, and `variantType`. No taxonomy graphs are returned.

Default page size is 25; allowed size is 1–100. Page starts at 1; offset `(page-1)*pageSize`
cannot exceed 100000. Invalid IDs, oversized terms (>200 characters), invalid sizes/pages,
integer overflow inputs and excessive offsets return HTTP 400. Empty searches remain bounded.
Unknown positive categories/species return empty pages. SpeciesId is required for variant search.
`hasMore` uses one lookahead row, avoiding a full count query. At most 101 rows are read and 100
returned. Page ordering is deterministic for a fixed catalog; concurrent catalog changes can
shift offset pages. The bounded offset policy meets measured needs without keyset complexity.

Species match scientific names, primary common names, SpeciesCommonNames and SpeciesAliases
case-insensitively using escaped PostgreSQL ILIKE. Search input is trimmed and repeated whitespace
collapsed. Percent, underscore and backslash are literal. A union of matching Species IDs lets
each name table use its index and prevents duplicate canonical results. Alias matches return the
linked Species' current display names, not the historical alias as primary text.

Ranks: exact scientific name, exact primary common name, exact alternate name/alias, prefix of
any searchable name, then substring. Ties use primary common name (or scientific name), scientific
name, and Species.Id. Empty searches use the same display-name tie-breakers without ranking.
Variants use Species filtering and escaped ILIKE on Name, ordered by Name then Id. Queries are
AsNoTracking DTO projections fully executed in PostgreSQL, with no Include graphs or N+1 queries.

Existing pg_trgm GIN indexes on Species scientific/common names and AnimalVariants.Name are
retained, as are category and Species-specific variant B-tree indexes. The new EF migration adds
only GIN trigram indexes on SpeciesCommonNames.Name and SpeciesAliases.Name. Exact-name metadata
indexes remain for reconciliation. No database extension beyond the existing pg_trgm is required.
See measurements for the benefits and the cases where the planner chooses sequential scans.

Gilligan's shared Add/Edit fields fetch only while a selector is open, one page of 25 at a time.
Selected Species is retained separately. Text input invalidates the previous request immediately;
nonempty searches wait 300 ms before fetching, empty searches fetch the initial page immediately.
AbortController plus an active-generation guard covers typing, clearing, category changes,
Species changes, closing/reopening and unmounting—even when a mock/transport ignores abort.
Errors have a local Retry action; loading/empty feedback stays at the selector, not the form banner.
Next/Previous buttons page results without growing an unbounded client catalog. Existing Ginger
components are reused; other combobox callers retain their default debounce behavior.

Display names preserve the curated Species.CommonName when present. Otherwise, catalog and Animal responses select the first stored English common name (eng/en), then the first stored common name in any language, using record ID for stable ordering. Provider ratings are not stored, so this is a first-name fallback, not a rating. Gilligan shows the scientific name once when no distinct common name exists. Existing imported names are used immediately without a reimport or migration.
