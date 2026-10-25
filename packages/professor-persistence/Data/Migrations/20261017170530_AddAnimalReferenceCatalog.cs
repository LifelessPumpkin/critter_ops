using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace skipper_api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAnimalReferenceCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnimalGroupId",
                table: "Animals",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AnimalVariantId",
                table: "Animals",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SpeciesId",
                table: "Animals",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AnimalGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LegacyAnimalType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimalGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Species",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CommonName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ScientificName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AnimalGroupId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PreferredVariantType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Species", x => x.Id);
                    table.CheckConstraint("CK_Species_ScientificName", "length(trim(\"ScientificName\")) > 0");
                    table.ForeignKey(
                        name: "FK_Species_AnimalGroups_AnimalGroupId",
                        column: x => x.AnimalGroupId,
                        principalTable: "AnimalGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AnimalVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SpeciesId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VariantType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimalVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnimalVariants_Species_SpeciesId",
                        column: x => x.SpeciesId,
                        principalTable: "Species",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "AnimalGroups",
                columns: new[] { "Id", "IsActive", "LegacyAnimalType", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, true, "Mammal", "Mammal", 10 },
                    { 2, true, "Avian", "Bird", 20 },
                    { 3, true, "Reptile", "Reptile", 30 },
                    { 4, true, "Amphibian", "Amphibian", 40 },
                    { 5, true, "Fish", "Fish", 50 },
                    { 6, true, "Invertebrate", "Invertebrate", 60 },
                    { 7, true, "Other", "Other", 70 }
                });

            migrationBuilder.InsertData(
                table: "Species",
                columns: new[] { "Id", "AnimalGroupId", "CommonName", "IsActive", "PreferredVariantType", "ScientificName" },
                values: new object[,]
                {
                    { 1, 1, "Domestic dog", true, "Breed", "Canis lupus familiaris" },
                    { 2, 1, "Domestic cat", true, "Breed", "Felis catus" },
                    { 3, 1, "Domestic ferret", true, null, "Mustela putorius furo" },
                    { 4, 3, "Ball python", true, "Morph", "Python regius" },
                    { 5, 3, "Bearded dragon", true, "Morph", "Pogona vitticeps" },
                    { 6, 3, "Leopard gecko", true, "Morph", "Eublepharis macularius" },
                    { 7, 3, "Corn snake", true, "Morph", "Pantherophis guttatus" },
                    { 8, 4, "Axolotl", true, "Morph", "Ambystoma mexicanum" },
                    { 9, 5, "Goldfish", true, "Variety", "Carassius auratus" },
                    { 10, 5, "Betta", true, "Strain", "Betta splendens" },
                    { 11, 2, "Budgerigar", true, "Mutation", "Melopsittacus undulatus" },
                    { 12, 2, "Cockatiel", true, "Mutation", "Nymphicus hollandicus" },
                    { 13, 1, "Guinea pig", true, "Breed", "Cavia porcellus" },
                    { 14, 1, "Rabbit", true, "Breed", "Oryctolagus cuniculus" },
                    { 15, 6, "Madagascar hissing cockroach", true, null, "Gromphadorhina portentosa" },
                    { 16, 6, "Mexican red-knee tarantula", true, null, "Brachypelma hamorii" }
                });

            migrationBuilder.InsertData(
                table: "AnimalVariants",
                columns: new[] { "Id", "Description", "IsActive", "Name", "SpeciesId", "VariantType" },
                values: new object[,]
                {
                    { 1, null, true, "Labrador Retriever", 1, "Breed" },
                    { 2, null, true, "Poodle", 1, "Breed" },
                    { 3, null, true, "Albino", 4, "Morph" },
                    { 4, null, true, "Pastel", 4, "Morph" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Animals_AnimalGroupId",
                table: "Animals",
                column: "AnimalGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Animals_AnimalVariantId",
                table: "Animals",
                column: "AnimalVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_Animals_SpeciesId",
                table: "Animals",
                column: "SpeciesId");

            migrationBuilder.CreateIndex(
                name: "IX_AnimalGroups_Name",
                table: "AnimalGroups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnimalVariants_SpeciesId_VariantType_Name",
                table: "AnimalVariants",
                columns: new[] { "SpeciesId", "VariantType", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Species_AnimalGroupId_ScientificName_Id",
                table: "Species",
                columns: new[] { "AnimalGroupId", "ScientificName", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_Animals_AnimalGroups_AnimalGroupId",
                table: "Animals",
                column: "AnimalGroupId",
                principalTable: "AnimalGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Animals_AnimalVariants_AnimalVariantId",
                table: "Animals",
                column: "AnimalVariantId",
                principalTable: "AnimalVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Animals_Species_SpeciesId",
                table: "Animals",
                column: "SpeciesId",
                principalTable: "Species",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Animals_AnimalGroups_AnimalGroupId",
                table: "Animals");

            migrationBuilder.DropForeignKey(
                name: "FK_Animals_AnimalVariants_AnimalVariantId",
                table: "Animals");

            migrationBuilder.DropForeignKey(
                name: "FK_Animals_Species_SpeciesId",
                table: "Animals");

            migrationBuilder.DropTable(
                name: "AnimalVariants");

            migrationBuilder.DropTable(
                name: "Species");

            migrationBuilder.DropTable(
                name: "AnimalGroups");

            migrationBuilder.DropIndex(
                name: "IX_Animals_AnimalGroupId",
                table: "Animals");

            migrationBuilder.DropIndex(
                name: "IX_Animals_AnimalVariantId",
                table: "Animals");

            migrationBuilder.DropIndex(
                name: "IX_Animals_SpeciesId",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "AnimalGroupId",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "AnimalVariantId",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "SpeciesId",
                table: "Animals");
        }
    }
}
