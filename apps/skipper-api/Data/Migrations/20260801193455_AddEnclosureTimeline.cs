using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace skipper_api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEnclosureTimeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EnclosureTimelineEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EnclosureId = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    PerformedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SourceReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Metadata = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnclosureTimelineEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnclosureTimelineEvents_Enclosures_EnclosureId",
                        column: x => x.EnclosureId,
                        principalTable: "Enclosures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnclosureTimelineEvents_EnclosureId",
                table: "EnclosureTimelineEvents",
                column: "EnclosureId");

            migrationBuilder.CreateIndex(
                name: "IX_EnclosureTimelineEvents_EnclosureId_OccurredAt",
                table: "EnclosureTimelineEvents",
                columns: new[] { "EnclosureId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EnclosureTimelineEvents_EventType",
                table: "EnclosureTimelineEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_EnclosureTimelineEvents_OccurredAt",
                table: "EnclosureTimelineEvents",
                column: "OccurredAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnclosureTimelineEvents");
        }
    }
}
