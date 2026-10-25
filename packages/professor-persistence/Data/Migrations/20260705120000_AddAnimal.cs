using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace skipper_api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAnimal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Animals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EnclosureId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Species = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SubspeciesOrMorph = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AnimalType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Sex = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    BirthDateIsEstimated = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AcquiredDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DispositionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DispositionReason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MicrochipNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TagIdentifier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Animals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Animals_Enclosures_EnclosureId",
                        column: x => x.EnclosureId,
                        principalTable: "Enclosures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Animals_AnimalType",
                table: "Animals",
                column: "AnimalType");

            migrationBuilder.CreateIndex(
                name: "IX_Animals_EnclosureId",
                table: "Animals",
                column: "EnclosureId");

            migrationBuilder.CreateIndex(
                name: "IX_Animals_MicrochipNumber",
                table: "Animals",
                column: "MicrochipNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Animals_Name",
                table: "Animals",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Animals_Species",
                table: "Animals",
                column: "Species");

            migrationBuilder.CreateIndex(
                name: "IX_Animals_Status",
                table: "Animals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Animals_TagIdentifier",
                table: "Animals",
                column: "TagIdentifier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Animals");
        }
    }
}
