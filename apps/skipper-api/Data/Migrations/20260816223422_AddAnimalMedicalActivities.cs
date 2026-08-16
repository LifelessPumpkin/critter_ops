using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skipper_api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAnimalMedicalActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnimalMedicationActivities",
                columns: table => new
                {
                    ActivityEventId = table.Column<long>(type: "bigint", nullable: false),
                    MedicationName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Dose = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    DoseUnit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Route = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Result = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimalMedicationActivities", x => x.ActivityEventId);
                    table.ForeignKey(
                        name: "FK_AnimalMedicationActivities_ActivityEvents_ActivityEventId",
                        column: x => x.ActivityEventId,
                        principalTable: "ActivityEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnimalTreatmentActivities",
                columns: table => new
                {
                    ActivityEventId = table.Column<long>(type: "bigint", nullable: false),
                    TreatmentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TreatmentName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Result = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimalTreatmentActivities", x => x.ActivityEventId);
                    table.ForeignKey(
                        name: "FK_AnimalTreatmentActivities_ActivityEvents_ActivityEventId",
                        column: x => x.ActivityEventId,
                        principalTable: "ActivityEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnimalMedicationActivities_MedicationName",
                table: "AnimalMedicationActivities",
                column: "MedicationName");

            migrationBuilder.CreateIndex(
                name: "IX_AnimalMedicationActivities_Route",
                table: "AnimalMedicationActivities",
                column: "Route");

            migrationBuilder.CreateIndex(
                name: "IX_AnimalTreatmentActivities_TreatmentName",
                table: "AnimalTreatmentActivities",
                column: "TreatmentName");

            migrationBuilder.CreateIndex(
                name: "IX_AnimalTreatmentActivities_TreatmentType",
                table: "AnimalTreatmentActivities",
                column: "TreatmentType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnimalMedicationActivities");

            migrationBuilder.DropTable(
                name: "AnimalTreatmentActivities");
        }
    }
}
