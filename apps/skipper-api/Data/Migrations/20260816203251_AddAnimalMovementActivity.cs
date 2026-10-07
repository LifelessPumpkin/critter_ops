using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skipper_api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAnimalMovementActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnimalMovementActivities",
                columns: table => new
                {
                    ActivityEventId = table.Column<long>(type: "bigint", nullable: false),
                    FromEnclosureId = table.Column<int>(type: "integer", nullable: false),
                    ToEnclosureId = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimalMovementActivities", x => x.ActivityEventId);
                    table.ForeignKey(
                        name: "FK_AnimalMovementActivities_ActivityEvents_ActivityEventId",
                        column: x => x.ActivityEventId,
                        principalTable: "ActivityEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnimalMovementActivities_Enclosures_FromEnclosureId",
                        column: x => x.FromEnclosureId,
                        principalTable: "Enclosures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AnimalMovementActivities_Enclosures_ToEnclosureId",
                        column: x => x.ToEnclosureId,
                        principalTable: "Enclosures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnimalMovementActivities_FromEnclosureId",
                table: "AnimalMovementActivities",
                column: "FromEnclosureId");

            migrationBuilder.CreateIndex(
                name: "IX_AnimalMovementActivities_ToEnclosureId",
                table: "AnimalMovementActivities",
                column: "ToEnclosureId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnimalMovementActivities");
        }
    }
}
