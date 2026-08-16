using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skipper_api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEnclosureCleaningActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EnclosureCleaningActivities",
                columns: table => new
                {
                    ActivityEventId = table.Column<long>(type: "bigint", nullable: false),
                    CleaningType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    WaterChangePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    SubstrateChanged = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    EquipmentCleaned = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnclosureCleaningActivities", x => x.ActivityEventId);
                    table.ForeignKey(
                        name: "FK_EnclosureCleaningActivities_ActivityEvents_ActivityEventId",
                        column: x => x.ActivityEventId,
                        principalTable: "ActivityEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnclosureCleaningActivities_CleaningType",
                table: "EnclosureCleaningActivities",
                column: "CleaningType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnclosureCleaningActivities");
        }
    }
}
