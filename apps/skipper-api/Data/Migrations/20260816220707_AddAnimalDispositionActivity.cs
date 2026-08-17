using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skipper_api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAnimalDispositionActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnimalDispositionActivities",
                columns: table => new
                {
                    ActivityEventId = table.Column<long>(type: "bigint", nullable: false),
                    DispositionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    RecipientOrDestination = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimalDispositionActivities", x => x.ActivityEventId);
                    table.ForeignKey(
                        name: "FK_AnimalDispositionActivities_ActivityEvents_ActivityEventId",
                        column: x => x.ActivityEventId,
                        principalTable: "ActivityEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnimalDispositionActivities_DispositionType",
                table: "AnimalDispositionActivities",
                column: "DispositionType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnimalDispositionActivities");
        }
    }
}
