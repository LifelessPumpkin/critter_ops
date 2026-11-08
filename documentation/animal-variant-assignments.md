# Multiple animal variant assignments (Ticket 6)

`Animal.VariantAssignments` is the authoritative characteristic collection. An animal has zero or more `AnimalVariantAssignment` rows, each with its own generated integer identity, animal FK, display order and UTC timestamps. It contains exactly one catalog `AnimalVariantId` or permanent `CustomValue`. Custom variants never require a catalog record or external provider recognition. This is descriptive metadata; genetic calculations are out of scope.

## Integrity and lifecycle

Structured names and types come from the existing catalog row. Assignment `VariantType` is null for structured values. Custom values preserve exact display text, including whitespace; an omitted custom type falls back to species `PreferredVariantType`, then `Other`. The same custom text in different types is permitted.

PostgreSQL generates a lowercase comparison key after trimming and collapsing whitespace. A filtered unique index enforces `(AnimalId, VariantType, NormalizedCustomValue)` for custom assignments, and another enforces `(AnimalId, AnimalVariantId)` for structured assignments. CHECK constraints enforce exclusive values, nonblank custom text, and valid custom types. Application validation checks duplicates, identifiers, lengths, types and species before mutation. Database uniqueness is the concurrent-write safeguard; callers receive a validation response on constraint conflicts.

Structured variants require a matching structured species ID. Custom species support custom variants. Species changes require explicit removal or replacement of incompatible assignments; custom assignments are preserved. Inactive catalog variants remain readable and retainable, but cannot be newly selected. Animal deletion cascades assignments; referenced catalog variants cannot be deleted. Other catalog deletion policies are unchanged.

## API

Create and update accept `variantAssignments`, for example:

```json
{
  "speciesId": 4,
  "variantAssignments": [
    { "animalVariantId": 3 },
    { "customValue": "Local Line", "variantType": "Strain" }
  ]
}
```

Include the normal required animal fields (`enclosureId`, `name`, `status`, `sex`, `acquiredDate`). POST omission/null creates an empty collection. PUT omission/null means no assignment change; `[]` explicitly clears; a supplied array is the desired complete collection. This deliberately preserves existing partial-reference PUT semantics. Array order determines display order.

`assignmentId` is optional on update. When present it must belong to the animal and appear only once. It supports editing custom text while retaining identity. When omitted, unchanged catalog IDs or normalized custom/type matches retain their IDs. IDs are not accepted on create. Validation precedes all animal/assignment mutations and one EF SaveChanges transaction saves the aggregate. Invalid input preserves species, assignments and unrelated properties.

List, search, detail, create and update return `variantAssignments` with `assignmentId`, `animalVariantId`, `speciesId`, `displayName`, `customValue`, `variantType`, `isCustom`, and `displayOrder`. Read projections fetch the collection without per-assignment API calls. Existing name/status/group/enclosure filtering and pagination are unchanged; there was no variant filter to convert.

The old single-value request fields remain a deprecated input adapter for existing clients, translated into assignments. They cannot be combined with `variantAssignments`. Deprecated single-value response aliases derive from the first assignment; they are not the permanent collection contract. Gilligan uses only the new collection. Removing these aliases is a coordinated client API breaking change. New writes do not populate the legacy variant columns.

## Migration and verification

`20261107183835_AddAnimalVariantAssignments` creates the table, constraints and indexes, then backfills while holding table locks. It snapshots every entire animal row into `AnimalVariantAssignmentMigrationAudit.Original` before backfill. It does not update animals or reset their sequence.

* Valid catalog references become structured assignments, including inactive references.
* Meaningful custom text becomes a custom assignment with exact original text.
* A normalized text duplicate of the referenced catalog name is represented once; original spelling remains in the legacy column and audit.
* Distinct mixed text becomes a separate custom assignment and records `distinct-legacy-text-preserved` for review.
* Invalid catalog IDs remain in the legacy snapshot and record `invalid-variant-id`. They cannot become a valid FK. Incompatible historical species/variant pairs remain readable and record `incompatible-species`.

Executable migration checks compare full animal JSON and IDs/counts against the audit, verify every valid structured reference and every meaningful text value is represented, and raise an exception if preservation fails. FK and unique constraints protect the assignment graph. Backfill uses conflict-safe inserts and can be repeated **only during migration verification before application writes resume**. It is not a reconciliation job to rerun after users edit assignments.

Review before any future cleanup:

```sql
SELECT "AnimalId", "Issues", "Original"
FROM "AnimalVariantAssignmentMigrationAudit"
WHERE NOT "Validated" ORDER BY "AnimalId";

SELECT count(*) AS animals_inspected,
       count(*) FILTER (WHERE "Validated") AS validated,
       count(*) FILTER (WHERE NOT "Validated") AS needing_review
FROM "AnimalVariantAssignmentMigrationAudit";
```

`Animal.AnimalVariantId` and `SubspeciesOrMorph` are retained as historical evidence, not live sources of truth. No destructive column cleanup is included because deployment-specific conflicts need review. Rollback refuses to drop a populated assignment table; export/review or restore a verified backup instead. No production database was migrated by development tests.

## Gilligan

Add and Edit share the existing form and Ginger controls. Selecting a search result or custom value appends an assignment. A scrollable selected list supports individual removal and inline custom text editing. Custom entry has an explicit type selector; labels follow species preferences and mixed types use “Variants & Traits.” Species/group changes retain assignments and display incompatibility errors until the user explicitly resolves them. Submission checks duplicates and compatibility; API errors remain visible without clearing the form.

Detail displays all assignments and custom/type metadata, including inactive catalog values. The list displays two names plus an overflow count, with a full tooltip and detail link. Draft storage preserves the array, type, IDs and order across enclosure creation and restores the actual enclosure ID. Still-valid old drafts are upgraded from their single selection; malformed collection entries are rejected.

## Limits and follow-ups

Historical invalid references and distinct legacy text require operator review; retained columns are intentional. No genetics, imports, catalog administration, Howell Sync changes or deployment changes were introduced. Updates use normal aggregate replacement semantics, without a new optimistic-concurrency/version API. A concurrent constraint conflict requires refresh and retry.

Ticket 7 should measure collection projection/query plans with large animal datasets before adding catalog search features. Preserve pagination and use `Any` for any future assignment filter. Ticket 8 should document operator audit review, backup/rollback procedures, coordinated retirement of deprecated API aliases, and a separately validated legacy-column cleanup.
