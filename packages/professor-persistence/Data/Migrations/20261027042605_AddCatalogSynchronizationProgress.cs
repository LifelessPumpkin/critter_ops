using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skipper_api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogSynchronizationProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalCanonicalName",
                table: "SpeciesExternalReferences",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastAppliedScientificName",
                table: "SpeciesExternalReferences",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CheckpointComplete",
                table: "CatalogSyncRuns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CompletionReason",
                table: "CatalogSyncRuns",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HttpRequests",
                table: "CatalogSyncRuns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HttpRetries",
                table: "CatalogSyncRuns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IssuesGenerated",
                table: "CatalogSyncRuns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastCommittedReferenceId",
                table: "CatalogSyncRuns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RecordsUnchanged",
                table: "CatalogSyncRuns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceIds",
                table: "CatalogSyncRuns",
                type: "character varying(12000)",
                maxLength: 12000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryOfRunId",
                table: "CatalogSyncRuns",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "CatalogSyncRuns",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Fingerprint",
                table: "CatalogSyncIssues",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogSyncRuns_RetryOfRunId",
                table: "CatalogSyncRuns",
                column: "RetryOfRunId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogSyncIssues_Fingerprint",
                table: "CatalogSyncIssues",
                column: "Fingerprint",
                unique: true,
                filter: "\"Fingerprint\" IS NOT NULL AND \"Status\" IN ('Open', 'Investigating')");

            migrationBuilder.AddForeignKey(
                name: "FK_CatalogSyncRuns_CatalogSyncRuns_RetryOfRunId",
                table: "CatalogSyncRuns",
                column: "RetryOfRunId",
                principalTable: "CatalogSyncRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CatalogSyncRuns_CatalogSyncRuns_RetryOfRunId",
                table: "CatalogSyncRuns");

            migrationBuilder.DropIndex(
                name: "IX_CatalogSyncRuns_RetryOfRunId",
                table: "CatalogSyncRuns");

            migrationBuilder.DropIndex(
                name: "IX_CatalogSyncIssues_Fingerprint",
                table: "CatalogSyncIssues");

            migrationBuilder.DropColumn(
                name: "ExternalCanonicalName",
                table: "SpeciesExternalReferences");

            migrationBuilder.DropColumn(
                name: "LastAppliedScientificName",
                table: "SpeciesExternalReferences");

            migrationBuilder.DropColumn(
                name: "CheckpointComplete",
                table: "CatalogSyncRuns");

            migrationBuilder.DropColumn(
                name: "CompletionReason",
                table: "CatalogSyncRuns");

            migrationBuilder.DropColumn(
                name: "HttpRequests",
                table: "CatalogSyncRuns");

            migrationBuilder.DropColumn(
                name: "HttpRetries",
                table: "CatalogSyncRuns");

            migrationBuilder.DropColumn(
                name: "IssuesGenerated",
                table: "CatalogSyncRuns");

            migrationBuilder.DropColumn(
                name: "LastCommittedReferenceId",
                table: "CatalogSyncRuns");

            migrationBuilder.DropColumn(
                name: "RecordsUnchanged",
                table: "CatalogSyncRuns");

            migrationBuilder.DropColumn(
                name: "ReferenceIds",
                table: "CatalogSyncRuns");

            migrationBuilder.DropColumn(
                name: "RetryOfRunId",
                table: "CatalogSyncRuns");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "CatalogSyncRuns");

            migrationBuilder.DropColumn(
                name: "Fingerprint",
                table: "CatalogSyncIssues");
        }
    }
}
