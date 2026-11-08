using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace skipper_api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAnimalVariantAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnimalVariantAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AnimalId = table.Column<int>(type: "integer", nullable: false),
                    AnimalVariantId = table.Column<int>(type: "integer", nullable: true),
                    CustomValue = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NormalizedCustomValue = table.Column<string>(type: "text", nullable: true, computedColumnSql: "lower(trim(regexp_replace(\"CustomValue\", '[[:space:]]+', ' ', 'g')))", stored: true),
                    VariantType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimalVariantAssignments", x => x.Id);
                    table.CheckConstraint("CK_Assignment_Type", "\"VariantType\" IS NULL OR \"VariantType\" IN ('Breed','Morph','Variety','Strain','Mutation','Subspecies','Other')");
                    table.CheckConstraint("CK_Assignment_Value", "(\"AnimalVariantId\" IS NOT NULL AND \"CustomValue\" IS NULL AND \"VariantType\" IS NULL) OR (\"AnimalVariantId\" IS NULL AND \"NormalizedCustomValue\" <> '' AND \"CustomValue\" IS NOT NULL AND \"VariantType\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_AnimalVariantAssignments_AnimalVariants_AnimalVariantId",
                        column: x => x.AnimalVariantId,
                        principalTable: "AnimalVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AnimalVariantAssignments_Animals_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnimalVariantAssignments_AnimalId_AnimalVariantId",
                table: "AnimalVariantAssignments",
                columns: new[] { "AnimalId", "AnimalVariantId" },
                unique: true,
                filter: "\"AnimalVariantId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AnimalVariantAssignments_AnimalId_VariantType_NormalizedCus~",
                table: "AnimalVariantAssignments",
                columns: new[] { "AnimalId", "VariantType", "NormalizedCustomValue" },
                unique: true,
                filter: "\"AnimalVariantId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AnimalVariantAssignments_AnimalVariantId",
                table: "AnimalVariantAssignments",
                column: "AnimalVariantId");
            migrationBuilder.Sql(BackfillSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $guard$ BEGIN
                  IF EXISTS (SELECT FROM "AnimalVariantAssignments") THEN
                    RAISE EXCEPTION 'Assignments must be exported and reviewed before rollback; legacy columns cannot represent all assignments.';
                  END IF;
                END $guard$;
                DROP TABLE "AnimalVariantAssignmentMigrationAudit";
                """);
            migrationBuilder.DropTable(
                name: "AnimalVariantAssignments");
        }
        // Repeatable only during migration validation, before application writes resume.
        public const string BackfillSql = """
            LOCK TABLE "Animals", "AnimalVariants", "AnimalVariantAssignments" IN SHARE ROW EXCLUSIVE MODE;
            CREATE TABLE IF NOT EXISTS "AnimalVariantAssignmentMigrationAudit" (
                "AnimalId" integer PRIMARY KEY, "Original" jsonb NOT NULL,
                "Issues" text[] NOT NULL DEFAULT '{}', "Validated" boolean NOT NULL DEFAULT false
            );
            INSERT INTO "AnimalVariantAssignmentMigrationAudit" ("AnimalId", "Original")
              SELECT "Id", to_jsonb(a) FROM "Animals" a ON CONFLICT DO NOTHING;
            INSERT INTO "AnimalVariantAssignments" ("AnimalId", "AnimalVariantId", "DisplayOrder", "CreatedAt", "UpdatedAt")
              SELECT a."Id", v."Id", 0, a."CreatedAt", a."UpdatedAt"
              FROM "Animals" a JOIN "AnimalVariants" v ON v."Id" = a."AnimalVariantId"
              ON CONFLICT DO NOTHING;
            -- Preserve exact display text. A normalized catalog-name duplicate is represented by the structured assignment.
            INSERT INTO "AnimalVariantAssignments" ("AnimalId", "CustomValue", "VariantType", "DisplayOrder", "CreatedAt", "UpdatedAt")
              SELECT a."Id", a."SubspeciesOrMorph", coalesce(s."PreferredVariantType", 'Other'),
                CASE WHEN v."Id" IS NULL THEN 0 ELSE 1 END, a."CreatedAt", a."UpdatedAt"
              FROM "Animals" a LEFT JOIN "AnimalVariants" v ON v."Id" = a."AnimalVariantId"
              LEFT JOIN "Species" s ON s."Id" = a."SpeciesId"
              WHERE trim(regexp_replace(a."SubspeciesOrMorph", '[[:space:]]+', ' ', 'g')) <> ''
                AND (v."Id" IS NULL OR lower(trim(regexp_replace(a."SubspeciesOrMorph", '[[:space:]]+', ' ', 'g'))) <> lower(trim(regexp_replace(v."Name", '[[:space:]]+', ' ', 'g'))))
              ON CONFLICT DO NOTHING;
            UPDATE "AnimalVariantAssignmentMigrationAudit" audit SET "Issues" =
              ARRAY(SELECT issue FROM (VALUES
                (CASE WHEN a."AnimalVariantId" IS NOT NULL AND v."Id" IS NULL THEN 'invalid-variant-id' END),
                (CASE WHEN v."Id" IS NOT NULL AND a."SpeciesId" IS DISTINCT FROM v."SpeciesId" THEN 'incompatible-species' END),
                (CASE WHEN v."Id" IS NOT NULL AND trim(coalesce(a."SubspeciesOrMorph", '')) <> '' AND lower(trim(regexp_replace(a."SubspeciesOrMorph", '[[:space:]]+', ' ', 'g'))) <> lower(trim(regexp_replace(v."Name", '[[:space:]]+', ' ', 'g'))) THEN 'distinct-legacy-text-preserved' END)
              ) issues(issue) WHERE issue IS NOT NULL)
              FROM "Animals" a LEFT JOIN "AnimalVariants" v ON v."Id" = a."AnimalVariantId"
              WHERE a."Id" = audit."AnimalId";
            DO $validate$ BEGIN
              IF EXISTS (SELECT FROM "AnimalVariantAssignmentMigrationAudit" audit FULL JOIN "Animals" a ON a."Id" = audit."AnimalId"
                WHERE a."Id" IS NULL OR audit."AnimalId" IS NULL OR to_jsonb(a) <> audit."Original") THEN
                RAISE EXCEPTION 'Animal identity or data changed during assignment migration';
              END IF;
              IF EXISTS (SELECT FROM "Animals" a JOIN "AnimalVariants" v ON v."Id" = a."AnimalVariantId"
                WHERE NOT EXISTS (SELECT FROM "AnimalVariantAssignments" x WHERE x."AnimalId" = a."Id" AND x."AnimalVariantId" = v."Id")) THEN
                RAISE EXCEPTION 'Structured assignment missing';
              END IF;
              IF EXISTS (SELECT FROM "Animals" a WHERE trim(regexp_replace(a."SubspeciesOrMorph", '[[:space:]]+', ' ', 'g')) <> ''
                AND NOT EXISTS (SELECT FROM "AnimalVariantAssignments" x LEFT JOIN "AnimalVariants" v ON v."Id" = x."AnimalVariantId"
                  WHERE x."AnimalId" = a."Id" AND (x."CustomValue" = a."SubspeciesOrMorph" OR lower(trim(regexp_replace(v."Name", '[[:space:]]+', ' ', 'g'))) = lower(trim(regexp_replace(a."SubspeciesOrMorph", '[[:space:]]+', ' ', 'g')))))) THEN
                RAISE EXCEPTION 'Custom assignment missing';
              END IF;
            END $validate$;
            UPDATE "AnimalVariantAssignmentMigrationAudit" SET "Validated" = cardinality("Issues") = 0;
            """;
    }
}
