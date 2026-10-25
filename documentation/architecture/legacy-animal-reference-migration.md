# Ticket 2: legacy animal reference migration

## Status and scope

This implementation supplies a deterministic PostgreSQL data migration, preservation audit, explicit structured/custom write contracts, reference-safe edits, and regression coverage. No production database was accessed or migrated. Production classification totals and cleanup readiness are therefore unknown. Destructive cleanup remains intentionally incomplete: the existing text columns still store permanent custom data, and `AnimalType` can contain operational information that cannot be reconstructed from catalog groups. Do not drop them based on a successful EF command or a clean test fixture.

No external service, catalog expansion, worker, multi-variant assignment, deployment configuration, or production backup is introduced.

## Inventory from the actual schema

| Property / database column | Type / nullable | Purpose and consumers | Replacement / migration behavior | Cleanup |
| --- | --- | --- | --- | --- |
| `Animal.Species` / `Animals.Species` | varchar(100), NOT NULL | Historically both custom species and duplicated structured labels; animal service, display projections, historical fixtures | `SpeciesId` is identity when present. Otherwise this same column implements API `customSpeciesName`. Exact historical text remains available as `originalSpeciesText`. Unique matches populate the ID without changing text. | Retain: permanent custom storage and potentially conflicting original descriptions. |
| `Animal.SubspeciesOrMorph` / `Animals.SubspeciesOrMorph` | varchar(100), nullable | Custom breed, morph, locality, subspecies or variant; previously also a duplicated catalog label | `AnimalVariantId` is identity when present. Otherwise this same column implements `customVariantName`. Original text is exposed separately. | Retain: permanent optional custom storage. |
| `Animal.AnimalType` / `Animals.AnimalType` | varchar(50), NOT NULL enum conversion | Operational category; legacy filters, group fallback and form initialization | Unique exact `AnimalGroup.LegacyAnimalType` mapping populates `AnimalGroupId`. The original value is never rewritten by migration. | Retain: Herptile/Aquatic and unknown historical strings cannot safely be collapsed into another group. |
| `AnimalGroupId`, `SpeciesId`, `AnimalVariantId` | nullable integer FKs | Existing structured references | Valid existing IDs take priority. No animals are recreated. Existing mismatches are retained and diagnosed. | Keep. |
| API `species`, `subspeciesOrMorph`, `speciesName`, `variantName` | derived response strings | Existing table, card, activity/detail consumers | Central service projections prefer catalog names and fall back to custom text. These response fields are display values, not write fields. | Keep derived display values; remove obsolete request fields. |

There is no separate legacy Breed or Variant column and no dedicated pre-existing custom column. Adding another pair of custom columns would duplicate the existing storage. API names clarify that storage without an unnecessary schema copy. Scientific names live in `Species.ScientificName`; scientific strings historically stored in `Animals.Species` remain intact.

## Model and write contracts

Create requests support `animalGroupId`, `speciesId` or `customSpeciesName`, and optional `animalVariantId` or `customVariantName`. `animalType` remains an operational category for ambiguous legacy groups. A structured variant must belong to the selected species. Unknown/inactive new references, incompatible groups, and structured/custom conflicts are rejected with existing HTTP 400 conventions. Existing inactive selections remain usable on unchanged records.

The obsolete request fields `species` and `subspeciesOrMorph` have been removed. JSON unmapped-member handling rejects those inputs. Gilligan and repository tests use the new write contract. Response display fields retain their previous names so list/detail/activity consumers continue to receive resolved names.

New structured records no longer copy catalog display labels into text storage (`Species` uses its required empty sentinel; optional variant text is null). Custom values are stored exactly, including meaningful whitespace, up to the existing 100-character bounds. New animals still require structured or nonblank custom species; variants remain optional, including for custom species.

Updates distinguish omitted reference fields from explicit nulls. Omit the entire reference section for unrelated edits; this preserves incomplete records, inactive references, original text, and conflicting historical classifications. Explicit `animalVariantId: null` and `customVariantName: null` remove a variant. Supply the desired species ID/custom value when changing species, and explicitly clear or replace an incompatible variant. Existing custom species is preserved when editing only the variant. The remaining non-reference fields follow the existing PUT conventions.

Reads return structured IDs, group/name, scientific name, variant type, and display names. `customSpeciesName` and `customVariantName` are populated when the corresponding structured reference is absent. `originalSpeciesText` and `originalVariantText` expose preserved storage even when a structured reference takes precedence. Projection queries resolve related names server-side; there is no per-animal lookup loop.

## Matching and preservation

Migration `20261017210000_MigrateLegacyAnimalReferences` follows Ticket 1's catalog metadata migration. It runs transactionally through EF, takes locks against concurrent animal/catalog writes, and snapshots every animal's ID, raw reference fields, reference IDs, and enclosure ID in `AnimalReferenceMigrationAudit`. It updates only the three reference-ID columns. Animal IDs, sequences, enclosure IDs, activity relationships, timestamps and unrelated values are untouched.

Normalization is comparison-only: collapse PostgreSQL whitespace runs, trim and lowercase. It never normalizes stored source text. There is no fuzzy, partial, biological-parent or hybrid parsing.

Species priority:

1. Retain existing SpeciesId and report invalid IDs or text/group disagreement.
2. Search exact normalized scientific name across the catalog; multiple candidates stop matching.
3. Search common name and `SpeciesCommonNames` within the uniquely mapped/existing group, counting distinct species IDs.
4. Search `SpeciesAliases` within that group.
5. Preserve unmatched/ambiguous text as custom storage.

A unique scientific match cannot overwrite a conflicting explicit group. With no exact group mapping, scientific identity may be assigned while the operational type and null group remain intact. Common-name and alias matching is deliberately conservative: without a mapped group it preserves custom text, even if there is a global common-name candidate. No catalog rows are created, and inactive historical catalog rows remain valid identities for migration.

Variant matching uses the selected species ID plus normalized full name. Names shared across species do not cause global matching. Multiple same-species candidates (including distinct VariantTypes) remain custom because the Animal row has no historical VariantType that can safely disambiguate them. An existing variant is retained and checked for species/text disagreement. Blank values stay blank/null without being turned into invented custom descriptions.

Group mapping uses a unique exact stored `LegacyAnimalType`, not a guessed biological category. `Herptile`, `Aquatic`, missing or unknown group strings remain original operational data and generate investigation issues. Explicit group/species mismatches are retained, not overwritten.

## Diagnostics and validation gate

`AnimalReferenceMigrationAudit` contains one immutable original snapshot and outcomes per animal; it deliberately has no FK so an intentional later deletion does not erase diagnostics. It stores no animal name or notes. Treat the retained reference text as application data with the same access controls as Animals.

`AnimalReferenceMigrationSummary` reports inspected count, species/variant matches, custom preservations, ambiguous matches, mapped groups, investigation count and classification validation. Custom counts include ambiguous values. Existing structured references are not counted as newly matched. Conflicting text remains in original storage/audit and is counted as an investigation issue.

The migration verifies the exact set of animal IDs (therefore count), raw text including null/whitespace, operational categories and enclosure IDs. It raises and rolls back if these change or an enclosure FK is broken. Existing invalid catalog IDs and species/group/variant inconsistencies are diagnosed and prevent classification validation. Existing structured IDs are never replaced. An empty database validates successfully.

Run `documentation/operations/validate-animal-reference-migration.sql` immediately after migration, while writes remain paused. It reports summary/issues, identity/text/previous-reference differences and invalid relationships. Zero differences and no invalid references are required. Review each investigation issue against the retained originals. A successful classification is not authorization to drop custom-storage columns.

No destructive migration is delivered. Even a clean production report cannot make `Species` and `SubspeciesOrMorph` obsolete while they implement custom storage. Removing/renaming physical columns would require a separate storage migration, mapping update, verification of exact originals and reviewed resolution of conflicts. Removing `AnimalType` additionally requires a permanent representation of ambiguous operational categories. These blockers are not bypassed to make the ticket appear complete.

EF history makes normal migration application repeatable. The SQL's audit insertion and per-row validation flags also prevent reclassification on immediate repeat execution, as exercised in PostgreSQL tests. Do not replay the raw SQL after intentional animal edits or deletions: its strict original-snapshot validation is intentionally a migration-time check, not an ongoing reconciliation job.

## Gilligan

Add Animal sends IDs or explicit custom fields, not duplicated catalog labels. Existing session draft keys/shapes and enclosure-return workflow are unchanged, so drafts still restore all prior form values. Edit Animal snapshots the initial reference state and omits reference payloads on unrelated changes, preserving historical and incomplete data. Group changes keep species/variant names as custom values, clear incompatible IDs, and show a review notice. Species changes invalidate the variant ID while preserving its text as custom data. Users can explicitly remove the variant with None.

Card and detail/activity views show scientific identity and preserved original descriptions where those differ from the resolved names. Shared response types include custom and original text. Existing table/filter displays still use server-resolved names; operational-type filters remain supported because their meaning has not been removed from the data.

## Recovery and rollback

Take a backup using the project's established operational process before any production migration; none is taken automatically here. Test restore and record the migration boundary. The original snapshot is enough to inspect initial reference assignments/text, but is not a full database backup and does not encode subsequent edits or all relationships.

`Down` removes the audit view/table only when there were no migrated animals. With a populated audit it raises rather than silently overwrite subsequent edits or delete diagnostics. Recovery requires a reviewed forward correction or restoration of a verified backup. Do not claim that destructive column removal would be reversible without one.

## Follow-up tickets

- Ticket 3 worker must not rerun this migration as startup reconciliation.
- Tickets 4–5 imports/synchronization can improve the catalog but must not silently reclassify custom animals or erase historical text.
- Ticket 6 should migrate the single variant FK into assignments only after preservation/conflict review; this ticket introduces no assignment table.
- Ticket 7 can expand alias/common-name search independently; migration matching remains frozen and deterministic.
- Ticket 8 should incorporate the validation SQL, audit handling, backup/restore procedure and retained-column decisions into operations documentation.
