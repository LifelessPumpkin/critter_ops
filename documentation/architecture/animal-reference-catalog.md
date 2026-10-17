# Animal reference catalog and enclosure return workflow

Animal Group is a business category, not a formal biological taxonomic class. Professor owns the catalog; ordinary entry queries only Skipper. No external taxonomy integration or reference administration UI is implemented.

## Persistence and compatibility

- `AnimalGroups`: stable ID, name, sort order, active flag, and legacy AnimalType mapping. Initial groups are Mammal, Bird, Reptile, Amphibian, Fish, Invertebrate, Other. Bird maps to the existing Avian enum for older API consumers.
- `Species`: nullable common name, required scientific name, group FK, active flag, optional preferred variant type. Common names are deliberately not unique. A scientific name alone can identify an option; common-name-only or unidentified animals use animal-local custom text.
- `AnimalVariants`: species FK, name, variant type, optional description, active flag. Types: Breed, Morph, Variety, Strain, Mutation, Subspecies, Other. Uniqueness is scoped to species + type + name.
- `Animals`: nullable AnimalGroupId, SpeciesId, AnimalVariantId. Existing Species, SubspeciesOrMorph, AnimalType columns remain. Referential deletes are restricted.

Structured IDs are authoritative. Reads resolve names through joins; without IDs, the existing text is authoritative. Species/variant text fields are snapshots when catalog references are assigned and remain usable by older consumers. Supplying both an ID and text resolves to the catalog value. Custom text never creates a global catalog entry. Species determines its group if a group is omitted. A custom species without a group uses the existing Other enum without inventing a catalog classification.

Create/update reject unavailable IDs, cross-species variants, group/species mismatches, blank required species, custom values longer than 100 characters, and nonblank-but-whitespace-only custom variants. A variant is optional. Existing inactive references may be retained during unrelated edits, but cannot be newly assigned. Normal DTO validation still requires name, enclosure, status, sex, and acquisition date. The UI also enforces reference text length limits.

PUT remains a full update. Older clients omitting reference-ID properties preserve existing links when they round-trip unchanged resolved text and AnimalType. Modern clients send explicit null to clear an ID; changing species clears its structured variant. Explicitly changing classification replaces the animal's text snapshots. Migration itself never changes those snapshots.

Animal responses retain `species`, `subspeciesOrMorph`, `animalType`, and add group/species/variant IDs, `animalGroupName`, `speciesScientificName`, `variantType`, `preferredVariantType`, and resolved `speciesName`/`variantName` aliases. List and detail projections resolve names server-side without per-row queries.

## Migrations and seeds

Apply `20261017170530_AddAnimalReferenceCatalog`, then `20261017170959_AddAnimalCatalogSearchIndexes` with the established EF migration process. Never point development verification at production. These additive migrations create catalog tables, relationships, indexes, seven groups, sixteen species and four variants; they leave every existing Animal row and text column intact. No mapping is attempted, including unambiguous-looking values. Herptile and Aquatic remain exactly as recorded; they require an informed user decision before reclassification.

EF `HasData` uses fixed IDs and the migration history supplies repeatability; there are no catalog writes in application startup. Reapplying the migration process does not duplicate seeds. The search migration requires PostgreSQL `pg_trgm` to be available and a migration role permitted to install it, or an administrator to preinstall it. Its rollback removes the new indexes but intentionally retains the shared extension. Rolling back the catalog migration removes catalog links/tables; normal forward deployment preserves legacy data.

Initial species: domestic dog, domestic cat, domestic ferret, ball python, bearded dragon, leopard gecko, corn snake, axolotl, goldfish, betta, budgerigar, cockatiel, guinea pig, rabbit, Madagascar hissing cockroach, Mexican red-knee tarantula. Variants: Labrador Retriever and Poodle for dog; Albino and Pastel for ball python. The curated scientific strings include domestic forms below species rank: Canis lupus familiaris and Mustela putorius furo. The operational Species catalog intentionally accommodates these familiar husbandry labels rather than asserting a formal rank hierarchy. These names are documented by [GBIF](https://www.gbif.org/es/taxon/5G6ZJ) and [NCBI](https://www.ncbi.nlm.nih.gov/bioproject/78213).

To expand the catalog, add reviewed records through a migration (small curated additions) or a future controlled offline importer. Reserve existing IDs, advance identity sequences after explicit-ID bulk imports, validate group/type relationships, retain source attribution and release metadata, and deactivate records rather than deleting referenced rows. Import and admin tooling are follow-up work.

## Read APIs and search

| Endpoint | Query parameters | Response |
| --- | --- | --- |
| GET /api/animal-groups | none | Active groups ordered by SortOrder, Id; Id, Name, SortOrder, LegacyAnimalType |
| GET /api/species | search, animalGroupId, page, pageSize | items with Id, CommonName, ScientificName, AnimalGroupId, AnimalGroupName, PreferredVariantType; page, pageSize, hasMore |
| GET /api/animal-variants | speciesId, search, page, pageSize | items with Id, SpeciesId, Name, VariantType; page, pageSize, hasMore |

Both searches accept empty text, default to 20 items, and cap at 100. Pages start at 1 (maximum 1,000,000); invalid values return HTTP 400 through model validation. Species sorts by common-name-or-scientific-name, scientific name, ID; variants by name, ID. Queries apply ILIKE filters and pagination in PostgreSQL and read one extra row to calculate hasMore. User `%`, `_`, and backslash are escaped as literal search text. Species searches match common and scientific names. Inactive records/groups are excluded from suggestions, without removing existing animal display values. Variant queries from the form always include SpeciesId.

B-tree indexes support group/species scope; GIN trigram indexes support substring ILIKE on common/scientific/variant names. Very short terms can still require scans; bounded results prevent a catalog download. Deep offset pagination and high-write imports should be measured before significantly larger catalog deployments; no Elasticsearch is required.

GET /api/animals/search also accepts `animalGroupIds` alongside legacy `animalTypes`. Filters combine their matches. A catalog-group filter includes unclassified legacy records only when their original AnimalType equals that group's explicit legacy mapping. Reptile/Fish filters do not guess that Herptile/Aquatic belong to them. All records remain accessible through All and explicit legacy filters.

## Gilligan workflow

The shared Add/Edit form loads group options from Skipper and uses Ginger Select/Combobox. Species and variant searches reuse the combobox's approximately 300 ms debounce. Each request has an AbortController and an active guard; results from superseded requests cannot overwrite the current result. Selected options retain their labels even outside the current result page. Initial, loading, empty and error states are provided. Catalog selectors allow the explicit custom-value action while a search is still pending.

Species displays common and scientific names. Type text then select `Use "…" as custom species` or the corresponding custom variant action. Selected custom options are labeled `(custom)`. With a custom species, variants remain custom and no global variant query is made. PreferredVariantType controls Breed/Morph/Variety/etc. labels centrally through catalog metadata; the fallback label is Variant. Selecting another incompatible group clears structured species and variant selections. Changing species clears variants; reselecting the same species retains them. Other form fields stay intact.

The repository previously had no Edit Animal route. `/animals/[id]/edit` now reuses the form and is linked from animal activity. It loads legacy or structured references and retains all existing optional fields, including fields not displayed in the compact form. The Animals list keeps its existing layout and supports both catalog groups and explicitly labeled legacy type filters. List/detail responses provide common display values for existing consumers.

### Create enclosure during Add Animal

1. Choose `+ Create new enclosure` inside the enclosure selector.
2. Save the entire Add Animal form, including selected-reference display metadata, to sessionStorage under `critterops:add-animal:<UUID>`.
3. Attach that UUID to the originating history entry and navigate to the existing `/enclosures/new?animalWorkflow=<UUID>` route.
4. Successful enclosure creation returns to `/animals/new?workflow=<UUID>&enclosureId=<actual ID>`. Cancel returns with only the workflow UUID.
5. The animal form restores only that workflow's draft. It refreshes enclosure options and separately fetches the returned ID so a new enclosure can be selected even if absent from the initial options response. Parallel results merge rather than overwrite each other.
6. Animal creation success or explicit Animal Cancel deletes that draft. Missing, malformed, completed or expired workflows show a warning and do not restore unrelated data. Drafts expire after six hours when read. Fresh `/animals/new` never restores arbitrary prior drafts.

Session storage isolates normal tabs; multiple workflows have separate UUID keys. Storage failure prevents the excursion and leaves entered fields in the form. Enclosure failures leave the existing enclosure form and preserved animal draft available. Missing returned enclosures show an error without losing animal fields; users can choose another enclosure. Session data contains only the form, no credentials. Reference loading never resets restored IDs. Cancel from a directly opened Add Enclosure continues returning to `/enclosures`.

## Future external data evaluation (research only, reviewed 2026-10-17)

| Source | Coverage, identifiers and access | Rights, quality and maintenance considerations |
| --- | --- | --- |
| Catalogue of Life / ChecklistBank | Global scientific checklist, synonyms, identifiers, downloadable ColDP/Darwin Core. Monthly and permanent annual releases. | Prefer a pinned Base Release for a reviewed initial import; Extended Release prioritizes coverage. COL's site states CC BY 4.0 unless indicated otherwise; verify the chosen archive's and constituent sources' license metadata and attribution before redistribution. Common names need separate coverage review. |
| GBIF Species API / checklist datasets | Broad checklist network, scientific and vernacular-name access, name matching, dataset/taxon identifiers, bulk checklists. | Pin dataset and release with IDs; do not treat a taxon key as immutable across all backbones/releases. Evaluate dataset-specific rights: GBIF-mediated occurrence data can be CC0, CC BY, or CC BY-NC; checklist requirements favor CC0/CC BY. Accuracy and release timing depend on the contributing checklist. |
| NCBI Taxonomy | TaxIDs, scientific names, some common names, sequence-linked coverage; full taxdump and programmatic access. | Strong for sequence-linked organisms, less appropriate as the sole husbandry catalog. NCBI warns it is not an authoritative nomenclatural source. Government-authored information is public domain under NCBI policy; check incorporated third-party material, preserve notices/acknowledgment. Account for merged/deleted IDs and frequent updates. |
| iNaturalist | Community-curated taxon IDs, scientific names and localized common names; APIs and full taxonomy Darwin Core archive. | Useful vernacular enrichment, subject to curator taxon changes and variable regional coverage. Verify the taxonomy archive's own current rights and release metadata. Do not infer taxonomy rights from the site's code license or observation/media licenses; observation/media defaults may be CC BY-NC. |

Sources: [COL downloads and release policy](https://www.catalogueoflife.org/data/download), [COL content license](https://www.catalogueoflife.org/), [GBIF Species API](https://techdocs.gbif.org/en/openapi/v1/species), [GBIF checklist requirements](https://www.gbif.org/uk/data-quality-requirements-checklists), [GBIF terms](https://www.gbif.org/terms?preview=true), [NCBI Taxonomy](https://www.ncbi.nlm.nih.gov/guide/taxonomy/), [NCBI nomenclature disclaimer](https://www.ncbi.nlm.nih.gov/Taxonomy/Browser/wwwtax.cgi), [NCBI policies](https://www.ncbi.nlm.nih.gov/home/about/policies/), [iNaturalist developer exports](https://www.inaturalist.org/pages/developers), [iNaturalist licensing](https://help.inaturalist.org/en/support/solutions/articles/151000175695).

Recommendation (engineering judgment): start with a reviewed, versioned COL Base subset, then consider licensed GBIF/iNaturalist common-name enrichment. Keep source, release, identifiers, license and attribution alongside an offline staging import; review ambiguities before promotion. Taxonomy changes require local ID reconciliation rather than replacing existing animal references. Breed/morph/variety/strain records need separate curated husbandry sources and rights checks; general taxonomy is insufficient. Future external-service outages must never block Add Animal. No importer, synchronizer, live external lookup or external ID schema is implemented in this ticket.
