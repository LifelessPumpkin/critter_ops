-- Run read-only immediately after migration, before reopening animal writes.
-- Later intentional edits/deletions can differ from this immutable migration snapshot.
BEGIN TRANSACTION ISOLATION LEVEL REPEATABLE READ READ ONLY;
SELECT * FROM "AnimalReferenceMigrationSummary";
SELECT "AnimalId", "SpeciesOutcome", "VariantOutcome", "GroupOutcome", "Issues", "Validated"
FROM "AnimalReferenceMigrationAudit" WHERE NOT "Validated" OR cardinality("Issues") > 0 ORDER BY "AnimalId";

SELECT count(*) AS "IdentityOrTextDifferences"
FROM "AnimalReferenceMigrationAudit" d FULL JOIN "Animals" a ON a."Id" = d."AnimalId"
WHERE a."Id" IS NULL OR d."AnimalId" IS NULL
   OR d."Original" ->> 'Species' IS DISTINCT FROM a."Species"
   OR d."Original" ->> 'SubspeciesOrMorph' IS DISTINCT FROM a."SubspeciesOrMorph"
   OR d."Original" ->> 'AnimalType' IS DISTINCT FROM a."AnimalType"
   OR (d."Original" ->> 'EnclosureId')::integer IS DISTINCT FROM a."EnclosureId"
   OR ((d."Original" ->> 'SpeciesId') IS NOT NULL AND (d."Original" ->> 'SpeciesId')::integer IS DISTINCT FROM a."SpeciesId")
   OR ((d."Original" ->> 'AnimalVariantId') IS NOT NULL AND (d."Original" ->> 'AnimalVariantId')::integer IS DISTINCT FROM a."AnimalVariantId")
   OR ((d."Original" ->> 'AnimalGroupId') IS NOT NULL AND (d."Original" ->> 'AnimalGroupId')::integer IS DISTINCT FROM a."AnimalGroupId");

SELECT a."Id" AS "InvalidReferenceAnimalId"
FROM "Animals" a
LEFT JOIN "Enclosures" e ON e."Id" = a."EnclosureId"
LEFT JOIN "Species" s ON s."Id" = a."SpeciesId"
LEFT JOIN "AnimalVariants" v ON v."Id" = a."AnimalVariantId"
LEFT JOIN "AnimalGroups" g ON g."Id" = a."AnimalGroupId"
WHERE e."Id" IS NULL OR (a."SpeciesId" IS NOT NULL AND s."Id" IS NULL)
   OR (a."AnimalVariantId" IS NOT NULL AND v."Id" IS NULL)
   OR (a."AnimalGroupId" IS NOT NULL AND g."Id" IS NULL)
   OR (v."Id" IS NOT NULL AND v."SpeciesId" IS DISTINCT FROM a."SpeciesId")
   OR (s."Id" IS NOT NULL AND g."Id" IS NOT NULL AND s."AnimalGroupId" <> g."Id");

-- No DROP statements: Species/SubspeciesOrMorph still implement permanent custom storage.
-- A clean classification report is necessary but insufficient to remove these columns.
COMMIT;
