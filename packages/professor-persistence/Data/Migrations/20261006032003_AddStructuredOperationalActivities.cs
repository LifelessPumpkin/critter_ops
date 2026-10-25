using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skipper_api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStructuredOperationalActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EnclosureWaterChangeActivities",
                columns: table => new
                {
                    ActivityEventId = table.Column<long>(type: "bigint", nullable: false),
                    WaterChangePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnclosureWaterChangeActivities", x => x.ActivityEventId);
                    table.ForeignKey(
                        name: "FK_EnclosureWaterChangeActivities_ActivityEvents_ActivityEvent~",
                        column: x => x.ActivityEventId,
                        principalTable: "ActivityEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InspectionActivities",
                columns: table => new
                {
                    ActivityEventId = table.Column<long>(type: "bigint", nullable: false),
                    Result = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionActivities", x => x.ActivityEventId);
                    table.ForeignKey(
                        name: "FK_InspectionActivities_ActivityEvents_ActivityEventId",
                        column: x => x.ActivityEventId,
                        principalTable: "ActivityEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceActivities",
                columns: table => new
                {
                    ActivityEventId = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceActivities", x => x.ActivityEventId);
                    table.ForeignKey(
                        name: "FK_MaintenanceActivities_ActivityEvents_ActivityEventId",
                        column: x => x.ActivityEventId,
                        principalTable: "ActivityEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InspectionActivities_Result",
                table: "InspectionActivities",
                column: "Result");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnclosureWaterChangeActivities");

            migrationBuilder.DropTable(
                name: "InspectionActivities");

            migrationBuilder.DropTable(
                name: "MaintenanceActivities");
        }
    }
}
