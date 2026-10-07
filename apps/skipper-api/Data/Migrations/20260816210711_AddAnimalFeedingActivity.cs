using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skipper_api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAnimalFeedingActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnimalFeedingActivities",
                columns: table => new
                {
                    ActivityEventId = table.Column<long>(type: "bigint", nullable: false),
                    Food = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Result = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimalFeedingActivities", x => x.ActivityEventId);
                    table.ForeignKey(
                        name: "FK_AnimalFeedingActivities_ActivityEvents_ActivityEventId",
                        column: x => x.ActivityEventId,
                        principalTable: "ActivityEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnimalFeedingActivities_Food",
                table: "AnimalFeedingActivities",
                column: "Food");

            migrationBuilder.CreateIndex(
                name: "IX_AnimalFeedingActivities_Result",
                table: "AnimalFeedingActivities",
                column: "Result");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnimalFeedingActivities");
        }
    }
}
