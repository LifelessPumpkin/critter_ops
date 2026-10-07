using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace skipper_api.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ProfessorDbContext))]
    [Migration("20260816000000_AddActivityLedger")]
    public partial class AddActivityLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    PerformedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SourceReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Metadata = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActivityEventAnimals",
                columns: table => new
                {
                    ActivityEventId = table.Column<long>(type: "bigint", nullable: false),
                    AnimalId = table.Column<int>(type: "integer", nullable: false),
                    RelationshipType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityEventAnimals", x => new { x.ActivityEventId, x.AnimalId, x.RelationshipType });
                    table.ForeignKey(
                        name: "FK_ActivityEventAnimals_ActivityEvents_ActivityEventId",
                        column: x => x.ActivityEventId,
                        principalTable: "ActivityEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityEventAnimals_Animals_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActivityEventEnclosures",
                columns: table => new
                {
                    ActivityEventId = table.Column<long>(type: "bigint", nullable: false),
                    EnclosureId = table.Column<int>(type: "integer", nullable: false),
                    RelationshipType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityEventEnclosures", x => new { x.ActivityEventId, x.EnclosureId, x.RelationshipType });
                    table.ForeignKey(
                        name: "FK_ActivityEventEnclosures_ActivityEvents_ActivityEventId",
                        column: x => x.ActivityEventId,
                        principalTable: "ActivityEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityEventEnclosures_Enclosures_EnclosureId",
                        column: x => x.EnclosureId,
                        principalTable: "Enclosures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEventAnimals_AnimalId",
                table: "ActivityEventAnimals",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEventAnimals_AnimalId_ActivityEventId",
                table: "ActivityEventAnimals",
                columns: new[] { "AnimalId", "ActivityEventId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEventEnclosures_EnclosureId",
                table: "ActivityEventEnclosures",
                column: "EnclosureId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEventEnclosures_EnclosureId_ActivityEventId",
                table: "ActivityEventEnclosures",
                columns: new[] { "EnclosureId", "ActivityEventId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEvents_EventType",
                table: "ActivityEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEvents_OccurredAt",
                table: "ActivityEvents",
                column: "OccurredAt");

            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    old_event record;
                    new_event_id bigint;
                BEGIN
                    FOR old_event IN SELECT * FROM "AnimalTimelineEvents" ORDER BY "Id" LOOP
                        INSERT INTO "ActivityEvents" (
                            "EventType", "OccurredAt", "Title", "Notes", "PerformedBy",
                            "SourceReferenceId", "SourceType", "Metadata", "CreatedAt", "UpdatedAt")
                        VALUES (
                            old_event."EventType", old_event."OccurredAt", old_event."Title", old_event."Description", old_event."PerformedBy",
                            old_event."SourceReferenceId", old_event."SourceType", old_event."Metadata", old_event."CreatedAt", old_event."UpdatedAt")
                        RETURNING "Id" INTO new_event_id;

                        INSERT INTO "ActivityEventAnimals" ("ActivityEventId", "AnimalId", "RelationshipType")
                        VALUES (new_event_id, old_event."AnimalId", 'Primary');

                        INSERT INTO "ActivityEventEnclosures" ("ActivityEventId", "EnclosureId", "RelationshipType")
                        VALUES (new_event_id, old_event."EnclosureId", 'Primary');
                    END LOOP;

                    FOR old_event IN SELECT * FROM "EnclosureTimelineEvents" ORDER BY "Id" LOOP
                        INSERT INTO "ActivityEvents" (
                            "EventType", "OccurredAt", "Title", "Notes", "PerformedBy",
                            "SourceReferenceId", "SourceType", "Metadata", "CreatedAt", "UpdatedAt")
                        VALUES (
                            old_event."EventType", old_event."OccurredAt", old_event."Title", old_event."Description", old_event."PerformedBy",
                            old_event."SourceReferenceId", old_event."SourceType", old_event."Metadata", old_event."CreatedAt", old_event."UpdatedAt")
                        RETURNING "Id" INTO new_event_id;

                        INSERT INTO "ActivityEventEnclosures" ("ActivityEventId", "EnclosureId", "RelationshipType")
                        VALUES (new_event_id, old_event."EnclosureId", 'Primary');
                    END LOOP;
                END $$;
                """);

            migrationBuilder.DropTable(
                name: "AnimalTimelineEvents");

            migrationBuilder.DropTable(
                name: "EnclosureTimelineEvents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnimalTimelineEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AnimalId = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_AnimalTimelineEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnimalTimelineEvents_Animals_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AnimalTimelineEvents_Enclosures_EnclosureId",
                        column: x => x.EnclosureId,
                        principalTable: "Enclosures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.Sql(
                """
                INSERT INTO "AnimalTimelineEvents" (
                    "AnimalId", "EnclosureId", "EventType", "OccurredAt", "Title", "Description", "PerformedBy",
                    "SourceReferenceId", "SourceType", "Metadata", "CreatedAt", "UpdatedAt")
                SELECT
                    animal."AnimalId",
                    enclosure."EnclosureId",
                    CASE activity."EventType"
                        WHEN 'Feeding' THEN 'Feeding'
                        WHEN 'Treatment' THEN 'Treatment'
                        WHEN 'Note' THEN 'Note'
                        WHEN 'Task' THEN 'Task'
                        ELSE 'Other'
                    END,
                    activity."OccurredAt",
                    activity."Title",
                    activity."Notes",
                    activity."PerformedBy",
                    activity."SourceReferenceId",
                    activity."SourceType",
                    activity."Metadata",
                    activity."CreatedAt",
                    activity."UpdatedAt"
                FROM "ActivityEvents" activity
                JOIN "ActivityEventAnimals" animal ON animal."ActivityEventId" = activity."Id"
                JOIN "ActivityEventEnclosures" enclosure ON enclosure."ActivityEventId" = activity."Id"
                WHERE animal."RelationshipType" = 'Primary'
                  AND enclosure."RelationshipType" = 'Primary';

                INSERT INTO "EnclosureTimelineEvents" (
                    "EnclosureId", "EventType", "OccurredAt", "Title", "Description", "PerformedBy",
                    "SourceReferenceId", "SourceType", "Metadata", "CreatedAt", "UpdatedAt")
                SELECT
                    enclosure."EnclosureId",
                    CASE activity."EventType"
                        WHEN 'WaterTest' THEN 'WaterTest'
                        WHEN 'Cleaning' THEN 'Cleaning'
                        WHEN 'Feeding' THEN 'Feeding'
                        WHEN 'Task' THEN 'Task'
                        ELSE 'Other'
                    END,
                    activity."OccurredAt",
                    activity."Title",
                    activity."Notes",
                    activity."PerformedBy",
                    activity."SourceReferenceId",
                    activity."SourceType",
                    activity."Metadata",
                    activity."CreatedAt",
                    activity."UpdatedAt"
                FROM "ActivityEvents" activity
                JOIN "ActivityEventEnclosures" enclosure ON enclosure."ActivityEventId" = activity."Id"
                WHERE enclosure."RelationshipType" = 'Primary';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_AnimalTimelineEvents_AnimalId",
                table: "AnimalTimelineEvents",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_AnimalTimelineEvents_AnimalId_OccurredAt_Id",
                table: "AnimalTimelineEvents",
                columns: new[] { "AnimalId", "OccurredAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AnimalTimelineEvents_EnclosureId",
                table: "AnimalTimelineEvents",
                column: "EnclosureId");

            migrationBuilder.CreateIndex(
                name: "IX_AnimalTimelineEvents_EventType",
                table: "AnimalTimelineEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_AnimalTimelineEvents_OccurredAt",
                table: "AnimalTimelineEvents",
                column: "OccurredAt");

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

            migrationBuilder.DropTable(
                name: "ActivityEventAnimals");

            migrationBuilder.DropTable(
                name: "ActivityEventEnclosures");

            migrationBuilder.DropTable(
                name: "ActivityEvents");
        }
    }
}
