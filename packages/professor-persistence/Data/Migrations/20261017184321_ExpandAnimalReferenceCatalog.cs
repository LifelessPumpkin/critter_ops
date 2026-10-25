using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace skipper_api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExpandAnimalReferenceCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Species",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsManuallyManaged",
                table: "Species",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAt",
                table: "Species",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxonomicStatus",
                table: "Species",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Unresolved");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Species",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CatalogSyncRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DatasetIdentifier = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    OperationType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RecordsProcessed = table.Column<int>(type: "integer", nullable: false),
                    RecordsCreated = table.Column<int>(type: "integer", nullable: false),
                    RecordsUpdated = table.Column<int>(type: "integer", nullable: false),
                    RecordsSkipped = table.Column<int>(type: "integer", nullable: false),
                    RecordsFailed = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogSyncRuns", x => x.Id);
                    table.CheckConstraint("CK_CatalogSyncRuns_Counts", "\"RecordsProcessed\" >= 0 AND \"RecordsCreated\" >= 0 AND \"RecordsUpdated\" >= 0 AND \"RecordsSkipped\" >= 0 AND \"RecordsFailed\" >= 0");
                    table.CheckConstraint("CK_CatalogSyncRuns_Dataset", "\"DatasetIdentifier\" IS NULL OR (\"DatasetIdentifier\" = btrim(\"DatasetIdentifier\") AND length(\"DatasetIdentifier\") > 0)");
                    table.CheckConstraint("CK_CatalogSyncRuns_Provider", "\"Provider\" ~ '^[a-z0-9][a-z0-9._-]*$'");
                    table.CheckConstraint("CK_CatalogSyncRuns_Times", "\"CompletedAt\" IS NULL OR (\"StartedAt\" IS NOT NULL AND \"CompletedAt\" >= \"StartedAt\")");
                });

            migrationBuilder.CreateTable(
                name: "SpeciesAliases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SpeciesId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    AliasType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Source = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    NameKey = table.Column<string>(type: "text", nullable: true, computedColumnSql: "lower(btrim(\"Name\"))", stored: true),
                    SourceKey = table.Column<string>(type: "text", nullable: true, computedColumnSql: "nullif(lower(btrim(\"Source\")), '')", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeciesAliases", x => x.Id);
                    table.CheckConstraint("CK_SpeciesAliases_Name", "length(btrim(\"Name\")) > 0");
                    table.ForeignKey(
                        name: "FK_SpeciesAliases_Species_SpeciesId",
                        column: x => x.SpeciesId,
                        principalTable: "Species",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpeciesCommonNames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SpeciesId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    Source = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LanguageKey = table.Column<string>(type: "text", nullable: true, computedColumnSql: "nullif(lower(btrim(\"LanguageCode\")), '')", stored: true),
                    NameKey = table.Column<string>(type: "text", nullable: true, computedColumnSql: "lower(btrim(\"Name\"))", stored: true),
                    SourceKey = table.Column<string>(type: "text", nullable: true, computedColumnSql: "nullif(lower(btrim(\"Source\")), '')", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeciesCommonNames", x => x.Id);
                    table.CheckConstraint("CK_SpeciesCommonNames_Name", "length(btrim(\"Name\")) > 0");
                    table.ForeignKey(
                        name: "FK_SpeciesCommonNames_Species_SpeciesId",
                        column: x => x.SpeciesId,
                        principalTable: "Species",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpeciesExternalReferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SpeciesId = table.Column<int>(type: "integer", nullable: false),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DatasetIdentifier = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ExternalIdentifier = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ExternalScientificName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExternalTaxonomicStatus = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExternalAcceptedIdentifier = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    LastCheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeciesExternalReferences", x => x.Id);
                    table.CheckConstraint("CK_SpeciesExternalReferences_Dataset", "\"DatasetIdentifier\" IS NULL OR (\"DatasetIdentifier\" = btrim(\"DatasetIdentifier\") AND length(\"DatasetIdentifier\") > 0)");
                    table.CheckConstraint("CK_SpeciesExternalReferences_Identifier", "\"ExternalIdentifier\" = btrim(\"ExternalIdentifier\") AND length(\"ExternalIdentifier\") > 0");
                    table.CheckConstraint("CK_SpeciesExternalReferences_Provider", "\"Provider\" ~ '^[a-z0-9][a-z0-9._-]*$'");
                    table.ForeignKey(
                        name: "FK_SpeciesExternalReferences_Species_SpeciesId",
                        column: x => x.SpeciesId,
                        principalTable: "Species",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CatalogSyncIssues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CatalogSyncRunId = table.Column<int>(type: "integer", nullable: true),
                    SpeciesId = table.Column<int>(type: "integer", nullable: true),
                    SpeciesExternalReferenceId = table.Column<int>(type: "integer", nullable: true),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DatasetIdentifier = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ExternalIdentifier = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    IssueType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogSyncIssues", x => x.Id);
                    table.CheckConstraint("CK_CatalogSyncIssues_Dataset", "\"DatasetIdentifier\" IS NULL OR (\"DatasetIdentifier\" = btrim(\"DatasetIdentifier\") AND length(\"DatasetIdentifier\") > 0)");
                    table.CheckConstraint("CK_CatalogSyncIssues_Provider", "\"Provider\" ~ '^[a-z0-9][a-z0-9._-]*$'");
                    table.ForeignKey(
                        name: "FK_CatalogSyncIssues_CatalogSyncRuns_CatalogSyncRunId",
                        column: x => x.CatalogSyncRunId,
                        principalTable: "CatalogSyncRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CatalogSyncIssues_SpeciesExternalReferences_SpeciesExternal~",
                        column: x => x.SpeciesExternalReferenceId,
                        principalTable: "SpeciesExternalReferences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CatalogSyncIssues_Species_SpeciesId",
                        column: x => x.SpeciesId,
                        principalTable: "Species",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "IsManuallyManaged", "LastSyncedAt", "UpdatedAt" },
                values: new object[] { null, true, null, null });

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "IsManuallyManaged", "LastSyncedAt", "UpdatedAt" },
                values: new object[] { null, true, null, null });

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "IsManuallyManaged", "LastSyncedAt", "UpdatedAt" },
                values: new object[] { null, true, null, null });

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "IsManuallyManaged", "LastSyncedAt", "UpdatedAt" },
                values: new object[] { null, true, null, null });

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "IsManuallyManaged", "LastSyncedAt", "UpdatedAt" },
                values: new object[] { null, true, null, null });

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "IsManuallyManaged", "LastSyncedAt", "UpdatedAt" },
                values: new object[] { null, true, null, null });

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "IsManuallyManaged", "LastSyncedAt", "UpdatedAt" },
                values: new object[] { null, true, null, null });

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "IsManuallyManaged", "LastSyncedAt", "UpdatedAt" },
                values: new object[] { null, true, null, null });

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "CreatedAt", "IsManuallyManaged", "LastSyncedAt", "UpdatedAt" },
                values: new object[] { null, true, null, null });

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "CreatedAt", "IsManuallyManaged", "LastSyncedAt", "UpdatedAt" },
                values: new object[] { null, true, null, null });

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "CreatedAt", "IsManuallyManaged", "LastSyncedAt", "UpdatedAt" },
                values: new object[] { null, true, null, null });

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "CreatedAt", "IsManuallyManaged", "LastSyncedAt", "UpdatedAt" },
                values: new object[] { null, true, null, null });

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "CreatedAt", "IsManuallyManaged", "LastSyncedAt", "UpdatedAt" },
                values: new object[] { null, true, null, null });

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "CreatedAt", "IsManuallyManaged", "LastSyncedAt", "UpdatedAt" },
                values: new object[] { null, true, null, null });

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "CreatedAt", "IsManuallyManaged", "LastSyncedAt", "UpdatedAt" },
                values: new object[] { null, true, null, null });

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "CreatedAt", "IsManuallyManaged", "LastSyncedAt", "UpdatedAt" },
                values: new object[] { null, true, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogSyncIssues_CatalogSyncRunId",
                table: "CatalogSyncIssues",
                column: "CatalogSyncRunId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogSyncIssues_SpeciesExternalReferenceId",
                table: "CatalogSyncIssues",
                column: "SpeciesExternalReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogSyncIssues_SpeciesId",
                table: "CatalogSyncIssues",
                column: "SpeciesId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogSyncIssues_Status_IssueType",
                table: "CatalogSyncIssues",
                columns: new[] { "Status", "IssueType" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogSyncRuns_Provider_StartedAt",
                table: "CatalogSyncRuns",
                columns: new[] { "Provider", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogSyncRuns_Status_StartedAt",
                table: "CatalogSyncRuns",
                columns: new[] { "Status", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SpeciesAliases_NameKey",
                table: "SpeciesAliases",
                column: "NameKey");

            migrationBuilder.CreateIndex(
                name: "IX_SpeciesAliases_SpeciesId_NameKey_AliasType_SourceKey",
                table: "SpeciesAliases",
                columns: new[] { "SpeciesId", "NameKey", "AliasType", "SourceKey" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_SpeciesCommonNames_NameKey",
                table: "SpeciesCommonNames",
                column: "NameKey");

            migrationBuilder.CreateIndex(
                name: "IX_SpeciesCommonNames_SpeciesId_NameKey_LanguageKey_SourceKey",
                table: "SpeciesCommonNames",
                columns: new[] { "SpeciesId", "NameKey", "LanguageKey", "SourceKey" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_SpeciesExternalReferences_Provider_DatasetIdentifier_Extern~",
                table: "SpeciesExternalReferences",
                columns: new[] { "Provider", "DatasetIdentifier", "ExternalIdentifier" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_SpeciesExternalReferences_SpeciesId",
                table: "SpeciesExternalReferences",
                column: "SpeciesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogSyncIssues");

            migrationBuilder.DropTable(
                name: "SpeciesAliases");

            migrationBuilder.DropTable(
                name: "SpeciesCommonNames");

            migrationBuilder.DropTable(
                name: "CatalogSyncRuns");

            migrationBuilder.DropTable(
                name: "SpeciesExternalReferences");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Species");

            migrationBuilder.DropColumn(
                name: "IsManuallyManaged",
                table: "Species");

            migrationBuilder.DropColumn(
                name: "LastSyncedAt",
                table: "Species");

            migrationBuilder.DropColumn(
                name: "TaxonomicStatus",
                table: "Species");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Species");
        }
    }
}
