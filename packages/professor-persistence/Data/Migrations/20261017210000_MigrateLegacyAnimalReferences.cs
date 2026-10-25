using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace skipper_api.Data.Migrations;

// Data-only migration: the existing text columns remain permanent custom/original storage.
[DbContext(typeof(ProfessorDbContext))]
[Migration("20261017210000_MigrateLegacyAnimalReferences")]
public class MigrateLegacyAnimalReferences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(Sql);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DO $rollback$ BEGIN
            IF EXISTS (SELECT FROM "AnimalReferenceMigrationAudit") THEN
                RAISE EXCEPTION 'Cannot automatically reverse animal classification. Restore a verified backup or review preserved originals and subsequent edits.';
            END IF;
        END $rollback$;
        DROP VIEW "AnimalReferenceMigrationSummary";
        DROP TABLE "AnimalReferenceMigrationAudit";
        """);

    public const string Sql = """
        LOCK TABLE "Animals", "AnimalGroups", "Species", "SpeciesCommonNames", "SpeciesAliases", "AnimalVariants" IN SHARE ROW EXCLUSIVE MODE;
        CREATE TABLE IF NOT EXISTS "AnimalReferenceMigrationAudit" (
            "AnimalId" integer PRIMARY KEY,
            "Original" jsonb NOT NULL,
            "SpeciesOutcome" text NOT NULL DEFAULT 'unchanged',
            "VariantOutcome" text NOT NULL DEFAULT 'unchanged',
            "GroupOutcome" text NOT NULL DEFAULT 'unchanged',
            "Issues" text[] NOT NULL DEFAULT '{}',
            "Validated" boolean NOT NULL DEFAULT false
        );
        -- No foreign key: diagnostics must survive a later, intentional animal deletion.
        INSERT INTO "AnimalReferenceMigrationAudit" ("AnimalId", "Original")
        SELECT "Id", jsonb_build_object('Species', "Species", 'SubspeciesOrMorph', "SubspeciesOrMorph",
            'AnimalType', "AnimalType", 'AnimalGroupId', "AnimalGroupId", 'SpeciesId', "SpeciesId",
            'AnimalVariantId', "AnimalVariantId", 'EnclosureId', "EnclosureId") FROM "Animals"
        ON CONFLICT DO NOTHING;

        CREATE OR REPLACE FUNCTION pg_temp.animal_reference_normalize(value text) RETURNS text
        LANGUAGE sql IMMUTABLE AS $normalize$
            SELECT lower(trim(regexp_replace(coalesce(value, ''), '[[:space:]]+', ' ', 'g')))
        $normalize$;

        DO $migration$
        DECLARE animal_row record; ids integer[]; selected_group integer; selected_species integer;
            selected_variant integer; species_group integer; issues text[];
            species_outcome text; variant_outcome text; group_outcome text; input text;
        BEGIN
          FOR animal_row IN SELECT animal.* FROM "Animals" animal
            JOIN "AnimalReferenceMigrationAudit" audit ON audit."AnimalId" = animal."Id"
            WHERE NOT audit."Validated" ORDER BY animal."Id"
          LOOP
            selected_group := animal_row."AnimalGroupId"; selected_species := animal_row."SpeciesId";
            selected_variant := animal_row."AnimalVariantId"; issues := '{}';
            species_outcome := 'existing'; variant_outcome := 'existing'; group_outcome := 'existing';
            IF selected_group IS NULL THEN
                SELECT array_agg("Id") INTO ids FROM "AnimalGroups" WHERE "LegacyAnimalType" = animal_row."AnimalType";
                IF cardinality(ids) = 1 THEN selected_group := ids[1]; group_outcome := 'mapped';
                ELSE group_outcome := 'retained'; issues := array_append(issues, 'unmapped-group'); END IF;
            ELSIF NOT EXISTS (SELECT FROM "AnimalGroups" WHERE "Id" = selected_group) THEN
                issues := array_append(issues, 'invalid-group-id');
            ELSIF NOT EXISTS (SELECT FROM "AnimalGroups" WHERE "Id" = selected_group AND "LegacyAnimalType" = animal_row."AnimalType") THEN
                issues := array_append(issues, 'group-conflict');
            END IF;
            input := pg_temp.animal_reference_normalize(animal_row."Species");
            IF selected_species IS NULL THEN
                species_outcome := CASE WHEN input = '' THEN 'empty' ELSE 'custom' END;
                IF input <> '' THEN
                    SELECT array_agg("Id") INTO ids FROM "Species" WHERE pg_temp.animal_reference_normalize("ScientificName") = input;
                    IF coalesce(cardinality(ids), 0) = 0 THEN
                        SELECT array_agg(DISTINCT s."Id") INTO ids FROM "Species" s
                        LEFT JOIN "SpeciesCommonNames" n ON n."SpeciesId" = s."Id"
                        WHERE (pg_temp.animal_reference_normalize(s."CommonName") = input OR pg_temp.animal_reference_normalize(n."Name") = input)
                          AND s."AnimalGroupId" = selected_group;
                    END IF;
                    IF coalesce(cardinality(ids), 0) = 0 THEN
                        SELECT array_agg(DISTINCT s."Id") INTO ids FROM "Species" s
                        JOIN "SpeciesAliases" n ON n."SpeciesId" = s."Id"
                        WHERE pg_temp.animal_reference_normalize(n."Name") = input AND s."AnimalGroupId" = selected_group;
                    END IF;
                    IF cardinality(ids) = 1 THEN
                        SELECT "AnimalGroupId" INTO species_group FROM "Species" WHERE "Id" = ids[1];
                        IF selected_group IS NULL OR selected_group = species_group THEN
                            selected_species := ids[1]; species_outcome := 'matched';
                        ELSE issues := array_append(issues, 'species-group-conflict'); END IF;
                    ELSIF cardinality(ids) > 1 THEN species_outcome := 'ambiguous'; END IF;
                END IF;
            ELSE
                IF NOT EXISTS (SELECT FROM "Species" WHERE "Id" = selected_species) THEN
                    issues := array_append(issues, 'invalid-species-id');
                ELSIF input <> '' AND NOT EXISTS (
                    SELECT FROM "Species" s WHERE s."Id" = selected_species AND
                        (pg_temp.animal_reference_normalize(s."ScientificName") = input OR pg_temp.animal_reference_normalize(s."CommonName") = input
                        OR EXISTS (SELECT FROM "SpeciesCommonNames" n WHERE n."SpeciesId" = s."Id" AND pg_temp.animal_reference_normalize(n."Name") = input)
                        OR EXISTS (SELECT FROM "SpeciesAliases" n WHERE n."SpeciesId" = s."Id" AND pg_temp.animal_reference_normalize(n."Name") = input))) THEN
                    issues := array_append(issues, 'species-text-conflict');
                END IF;
            END IF;
            IF selected_species IS NOT NULL AND selected_group IS NOT NULL AND EXISTS (
                SELECT FROM "Species" WHERE "Id" = selected_species AND "AnimalGroupId" <> selected_group) THEN
                issues := array_append(issues, 'species-group-conflict');
            END IF;
            input := pg_temp.animal_reference_normalize(animal_row."SubspeciesOrMorph");
            IF selected_variant IS NULL THEN
                variant_outcome := CASE WHEN input = '' THEN 'empty' ELSE 'custom' END;
                IF input <> '' AND selected_species IS NOT NULL THEN
                    SELECT array_agg("Id") INTO ids FROM "AnimalVariants"
                    WHERE "SpeciesId" = selected_species AND pg_temp.animal_reference_normalize("Name") = input;
                    IF cardinality(ids) = 1 THEN selected_variant := ids[1]; variant_outcome := 'matched';
                    ELSIF cardinality(ids) > 1 THEN variant_outcome := 'ambiguous'; END IF;
                END IF;
            ELSIF NOT EXISTS (SELECT FROM "AnimalVariants" WHERE "Id" = selected_variant) THEN
                issues := array_append(issues, 'invalid-variant-id');
            ELSE
                IF NOT EXISTS (SELECT FROM "AnimalVariants" WHERE "Id" = selected_variant AND "SpeciesId" = selected_species) THEN
                    issues := array_append(issues, 'variant-species-conflict');
                END IF;
                IF input <> '' AND NOT EXISTS (SELECT FROM "AnimalVariants" WHERE "Id" = selected_variant AND pg_temp.animal_reference_normalize("Name") = input) THEN
                    issues := array_append(issues, 'variant-text-conflict');
                END IF;
            END IF;
            UPDATE "Animals" SET "AnimalGroupId" = selected_group, "SpeciesId" = selected_species, "AnimalVariantId" = selected_variant WHERE "Id" = animal_row."Id";
            UPDATE "AnimalReferenceMigrationAudit" SET "SpeciesOutcome" = species_outcome, "VariantOutcome" = variant_outcome,
                "GroupOutcome" = group_outcome, "Issues" = issues WHERE "AnimalId" = animal_row."Id";
          END LOOP;
          -- Validate identity, enclosure relationships and exact text, including nulls and whitespace.
          IF EXISTS (SELECT FROM "AnimalReferenceMigrationAudit" d FULL JOIN "Animals" a ON a."Id" = d."AnimalId"
              WHERE d."AnimalId" IS NULL OR a."Id" IS NULL OR
              d."Original" ->> 'Species' IS DISTINCT FROM a."Species" OR
              d."Original" ->> 'SubspeciesOrMorph' IS DISTINCT FROM a."SubspeciesOrMorph" OR
              d."Original" ->> 'AnimalType' IS DISTINCT FROM a."AnimalType" OR
              (d."Original" ->> 'EnclosureId')::integer IS DISTINCT FROM a."EnclosureId") THEN
              RAISE EXCEPTION 'Animal reference migration preservation validation failed';
          END IF;
          IF EXISTS (SELECT FROM "Animals" a LEFT JOIN "Enclosures" e ON e."Id" = a."EnclosureId" WHERE e."Id" IS NULL) THEN
              RAISE EXCEPTION 'Animal reference migration found an invalid enclosure relationship';
          END IF;
          UPDATE "AnimalReferenceMigrationAudit" SET "Validated" = true;
        END $migration$;
        CREATE OR REPLACE VIEW "AnimalReferenceMigrationSummary" AS
        SELECT count(*) AS "AnimalsInspected",
            count(*) FILTER (WHERE "SpeciesOutcome" = 'matched') AS "SpeciesMatched",
            count(*) FILTER (WHERE "SpeciesOutcome" IN ('custom', 'ambiguous')) AS "CustomSpeciesPreserved",
            count(*) FILTER (WHERE "SpeciesOutcome" = 'ambiguous') AS "SpeciesAmbiguous",
            count(*) FILTER (WHERE "VariantOutcome" = 'matched') AS "VariantsMatched",
            count(*) FILTER (WHERE "VariantOutcome" IN ('custom', 'ambiguous')) AS "CustomVariantsPreserved",
            count(*) FILTER (WHERE "VariantOutcome" = 'ambiguous') AS "VariantsAmbiguous",
            count(*) FILTER (WHERE "GroupOutcome" = 'mapped') AS "GroupsMapped",
            count(*) FILTER (WHERE cardinality("Issues") > 0) AS "RecordsRequiringInvestigation",
            coalesce(bool_and("Validated" AND cardinality("Issues") = 0), true) AS "ClassificationValidated"
        FROM "AnimalReferenceMigrationAudit";
        -- Deliberately no DROP COLUMN: these text columns still serve permanent custom values.
        """;
}
