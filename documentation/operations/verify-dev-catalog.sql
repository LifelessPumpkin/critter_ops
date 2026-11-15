-- Read-only verification. For profile-specific counts use scripts/verify-dev-catalog.sh.
SELECT count(*) AS total_species,
       count(*) FILTER (WHERE "Id" BETWEEN 1 AND 16) AS original_seed_ids,
       count(*) FILTER (WHERE NOT "IsManuallyManaged") AS importer_managed_species,
       count(*) FILTER (WHERE "CommonName" IS NOT NULL) AS with_primary_common_name,
       count(*) FILTER (WHERE "PreferredVariantType" IS NOT NULL) AS with_preferred_variant_type,
       count(*) FILTER (WHERE "PreferredVariantType" IS NULL) AS uses_variant_fallback
FROM "Species";
SELECT count(DISTINCT "SpeciesId") AS distinct_gbif_backed_species,
       count(*) AS gbif_external_references,
       count(DISTINCT "SpeciesId") FILTER (WHERE "ExternalTaxonomicStatus" = 'ACCEPTED') AS accepted_gbif_backed_species,
       count(*) FILTER (WHERE "ExternalTaxonomicStatus" <> 'ACCEPTED') AS nonaccepted_references
FROM "SpeciesExternalReferences" WHERE "Provider" = 'gbif';
SELECT g."Name" AS animal_category, count(*) AS species,
       count(*) FILTER (WHERE EXISTS (SELECT 1 FROM "SpeciesCommonNames" n WHERE n."SpeciesId" = s."Id")) AS with_alternate_common_names,
       count(*) FILTER (WHERE EXISTS (SELECT 1 FROM "SpeciesAliases" a WHERE a."SpeciesId" = s."Id")) AS with_aliases,
       count(*) FILTER (WHERE EXISTS (SELECT 1 FROM "SpeciesExternalReferences" r WHERE r."SpeciesId" = s."Id")) AS with_external_references
FROM "Species" s JOIN "AnimalGroups" g ON g."Id" = s."AnimalGroupId"
GROUP BY g."Name" ORDER BY g."Name";
SELECT "Id", "Scope"::jsonb->>'Profile' AS profile, "Status", "StartedAt", "CompletedAt",
       "RecordsCreated", "RecordsUpdated", "RecordsUnchanged", "RecordsSkipped", "RecordsFailed",
       "HttpRequests", "HttpRetries", "IssuesGenerated", "RetryOfRunId", "CompletionReason"
FROM "CatalogSyncRuns" ORDER BY "Id" DESC LIMIT 10;
SELECT "CatalogSyncRunId", "IssueType", "Status", count(*) AS issues
FROM "CatalogSyncIssues" GROUP BY "CatalogSyncRunId", "IssueType", "Status" ORDER BY "CatalogSyncRunId" DESC;
-- Both duplicate checks should normally return no rows. Review curated homonyms rather than auto-merging them.
SELECT "Provider", "DatasetIdentifier", "ExternalIdentifier", count(*)
FROM "SpeciesExternalReferences" GROUP BY 1,2,3 HAVING count(*) > 1;
SELECT "AnimalGroupId", lower("ScientificName"), count(*)
FROM "Species" GROUP BY 1,2 HAVING count(*) > 1;
SELECT a."Id", a."Name", a."SpeciesId", s."ScientificName", g."Name" AS category, s."PreferredVariantType"
FROM "Animals" a JOIN "Species" s ON s."Id"=a."SpeciesId" JOIN "AnimalGroups" g ON g."Id"=s."AnimalGroupId"
ORDER BY a."Id" DESC LIMIT 10;
