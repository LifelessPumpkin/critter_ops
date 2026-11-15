using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skipper_api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogAlternateNameSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SpeciesCommonNames_Name",
                table: "SpeciesCommonNames",
                column: "Name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_SpeciesAliases_Name",
                table: "SpeciesAliases",
                column: "Name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SpeciesCommonNames_Name",
                table: "SpeciesCommonNames");

            migrationBuilder.DropIndex(
                name: "IX_SpeciesAliases_Name",
                table: "SpeciesAliases");
        }
    }
}
