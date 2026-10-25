using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace skipper_api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEnclosure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Enclosures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Location = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SizeLabel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Length = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    Width = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    Height = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    DimensionUnit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Volume = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    VolumeUnit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Material = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MaxAnimalCapacity = table.Column<int>(type: "integer", nullable: true),
                    Mobility = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SafetyRating = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enclosures", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Enclosures_Location",
                table: "Enclosures",
                column: "Location");

            migrationBuilder.CreateIndex(
                name: "IX_Enclosures_Name",
                table: "Enclosures",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Enclosures_Status",
                table: "Enclosures",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Enclosures_Type",
                table: "Enclosures",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Enclosures");
        }
    }
}
