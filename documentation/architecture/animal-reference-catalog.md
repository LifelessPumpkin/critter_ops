# Animal reference catalog and enclosure return workflow

Animal Group (shown as Animal Category in Gilligan) is a business category, not a formal biological
class. Professor owns catalog persistence. Howell Sync imports and refreshes external taxonomy;
ordinary application requests query local data through Skipper. No catalog administration UI exists.

## Persistence and identity

- AnimalGroups have stable IDs, name, sort order, active flag, and an explicit legacy AnimalType
  mapping. Groups are Mammal, Bird, Reptile, Amphibian, Fish, Invertebrate, Other; Bird maps to Avian.
- Species have a required scientific name, optional primary common name, group, active flag, and
  optional preferred variant type. Names are not globally unique identifiers.
- AnimalVariants define a species-specific name/type, optional description, and active flag.
  Types include Breed, Morph, Variety, Strain, Mutation, Subspecies and Other.
- Animals have optional group/Species IDs and permanent custom species text. Multiple
  `AnimalVariantAssignment` rows are authoritative for structured and custom characteristics.
  Legacy single-variant columns remain historical evidence, not live assignment storage.

Internal IDs remain authoritative across provider changes. Selecting a catalog species never creates
another global row. Custom species and custom variants remain supported without provider recognition.
See [catalog metadata](animal-catalog-metadata.md) for provenance/ownership/constraints and
[variant assignments](../animal-variant-assignments.md) for collection integrity and lifecycle.

## Write and read contracts

Create/PUT use `speciesId` or `customSpeciesName`, optional `animalGroupId` and operational
`animalType`, plus `variantAssignments`. A structured species supplies its group if omitted.
New animals require a catalog or nonblank custom species. Conflicting IDs/custom values,
incompatible group/species/variants, unavailable new selections, and oversized custom text are
rejected. Existing inactive references can be retained on unchanged records. Custom text is
preserved exactly; it does not create global catalog values.

Animal PUT still requires the normal animal fields. Omitting its reference section preserves
historical/incomplete values; explicit changes have validation and null-clearing rules. Assignment
omission/null preserves the collection; `[]` clears it. Species changes require explicit resolution
of incompatible structured assignments and never discard custom values. See the
[assignment API](../animal-variant-assignments.md#api) for full replacement and identity semantics.
Deprecated single-variant request adapters cannot be combined with the collection.

Reads resolve catalog display names server-side and return IDs, scientific names, preferred type,
custom/original species text and the assignment collection. Legacy single-variant aliases derive
from the first assignment. The old `species` and `subspeciesOrMorph` request fields are rejected;
resolved response strings are display values, not write fields. Preserved operational AnimalType
values, including ambiguous Herptile/Aquatic, are not guessed into catalog groups.

## Migrations and seeds

Professor owns the complete ordered migration history. Apply migrations through Skipper's
[EF instructions](../../apps/skipper-api/README.md#ef-core-migrations), not a hand-selected initial pair.
The initial catalog migrations add seven groups, sixteen species and four variants with stable IDs.
They preserve existing Animals/text and add pg_trgm search indexes. The migration role must be able
to install that extension, or an administrator must preinstall it. Index rollback retains the extension.

The [legacy-reference migration](legacy-animal-reference-migration.md) subsequently performs exact,
conservative matching with retained audit data. The [assignment migration](../animal-variant-assignments.md#migration-and-verification)
backfills multiple values without deleting original animal fields. Populated rollback has explicit
safeguards; audit snapshots do not replace backups. No column cleanup is authorized by a successful
migration. Preserve internal/seed IDs, custom values and ambiguous original classifications.

For catalog expansion use [Howell Sync](../../apps/howell-sync/README.md) and the
[local catalog guide](../operations/local-catalog-setup.md). The curated seed catalog includes
husbandry labels below formal species rank; it does not assert a full biological rank hierarchy.
Howell's GBIF mapper conservatively accepts its documented provider ranks/classifications.

## Read APIs and search

`GET /api/animal-groups` returns active groups ordered by SortOrder/ID. Species and variant searches
return bounded pages with `hasMore`. [Catalog search](catalog-search.md) owns query parameters,
25-row defaults, required Species scope for variants, ranking, alias/common-name matching,
limits, index behavior and display-name fallback. Do not load the entire catalog into Gilligan.

Animal search supports both `animalGroupIds` and legacy `animalTypes`. Group filtering includes
unclassified legacy animals only when their original AnimalType exactly matches the group's
explicit mapping. Reptile/Fish filters do not assume Herptile/Aquatic identity. All and legacy
filters retain access to historical animals.

## Gilligan workflow

Add/Edit share bounded species/variant selectors with loading, empty, retry and paging feedback.
Requests are protected against stale responses; selected values survive paging and search changes.
Structured species determines/locks category. A manually selected category can constrain initial
species search. Custom species allow manual classification. Variants appear as removable/editable
chips; labels/new custom types follow Species.PreferredVariantType with Variant/Other fallback.
Species changes preserve selections and flag incompatibilities for explicit resolution. See the
[assignment UI contract](../animal-variant-assignments.md#gilligan) for accessibility, errors and drafts.

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

Recommendation (engineering judgment): start with a reviewed, versioned COL Base subset, then consider licensed GBIF/iNaturalist common-name enrichment. Keep source, release, identifiers, license and attribution alongside an offline staging import; review ambiguities before promotion. Taxonomy changes require local ID reconciliation rather than replacing existing animal references. Breed/morph/variety/strain records need separate curated husbandry sources and rights checks; general taxonomy is insufficient. Future external-service outages must never block Add Animal. This assessment predates the GBIF implementation. Howell now supplies offline import/synchronization and provenance; see its runbook above. COL, NCBI, and iNaturalist adapters remain unimplemented. Licensing statements here are dated research, not a current redistribution approval.
