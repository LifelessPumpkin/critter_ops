using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using skipper_api.Data;
using skipper_api.Data.Migrations;
using skipper_api.Domain.Animals;
using skipper_api.Domain.Enclosures;
using skipper_api.Dtos.Animals;
using skipper_api.Services.Animals;
using Xunit;

namespace Skipper.Api.Tests.Data;

public class LegacyAnimalReferenceMigrationTests(PostgresFixture database) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task PopulatedMigration_PreservesExactTextAndIdentity_ReportsAmbiguityAndConflicts()
    {
        await using var db = new ProfessorDbContext(await database.CreateDatabaseAsync());
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261017184321_ExpandAnimalReferenceCatalog");
        var enclosure = new Enclosure { Name = "Migration fixture", Type = EnclosureType.Terrarium, Location = "Room", Mobility = EnclosureMobility.Fixed, Status = EnclosureStatus.Active, CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow };
        db.Enclosures.Add(enclosure); await db.SaveChangesAsync();
        db.SpeciesCommonNames.Add(new() { SpeciesId = 4, Name = "Royal python", CreatedAt = DateTime.UtcNow });
        db.SpeciesAliases.Add(new() { SpeciesId = 4, Name = "Historical python", CreatedAt = DateTime.UtcNow });
        db.Species.AddRange(new Species { Id = 5001, ScientificName = "Ambiguus one", CommonName = "Shared", AnimalGroupId = 3 },
            new Species { Id = 5002, ScientificName = "Ambiguus two", CommonName = "Shared", AnimalGroupId = 3 });
        db.AnimalVariants.AddRange(new AnimalVariant { SpeciesId = 4, Name = "Shared morph", VariantType = VariantType.Morph },
            new AnimalVariant { SpeciesId = 4, Name = "Shared morph", VariantType = VariantType.Strain });
        await db.SaveChangesAsync();
        var inputs = new (string Species, string? Variant, string Type, int? SpeciesId, int? VariantId)[] {
            ("  BALL  PYTHON\t", " ALBINO ", "Reptile", null, null),
            ("Python regius", null, "Reptile", null, null),
            ("Royal python", "Shared morph", "Reptile", null, null),
            ("Historical python", "Local strain", "Reptile", null, null),
            ("Shared", "Albino", "Reptile", null, null),
            ("Unidentified hybrid", "Regional variety", "Herptile", null, null),
            ("", "", "Aquatic", null, null),
            ("Conflicting text", "Conflicting morph", "Reptile", 4, 3),
            ("Ball python", "Poodle", "Reptile", 4, 1),
            ("Python regius", null, "Mammal", null, null),
        };
        foreach (var (species, variant, type, speciesId, variantId) in inputs)
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "Animals" ("EnclosureId", "Name", "Species", "SubspeciesOrMorph", "AnimalType", "SpeciesId", "AnimalVariantId", "Status", "Sex", "AcquiredDate", "BirthDateIsEstimated", "CreatedAt", "UpdatedAt")
                VALUES ({enclosure.Id}, {species}, {species}, {variant}, {type}, {speciesId}, {variantId}, 'Active', 'Unknown', DATE '2026-01-01', false, NOW(), NOW())
                """);
        var unchangedColumns = await UnchangedColumns(db);
        var before = await db.Animals.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
        await migrator.MigrateAsync();
        await migrator.MigrateAsync();
        var after = await db.Animals.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
        Assert.Equal(unchangedColumns, await UnchangedColumns(db));
        Assert.Equal(before.Select(x => x.Id), after.Select(x => x.Id));
        for (var i = 0; i < before.Count; i++) {
            Assert.Equal(before[i].Species, after[i].Species);
            Assert.Equal(before[i].SubspeciesOrMorph, after[i].SubspeciesOrMorph);
            Assert.Equal(before[i].AnimalType, after[i].AnimalType);
            Assert.Equal(before[i].EnclosureId, after[i].EnclosureId);
        }
        Assert.All(after.Take(4), x => Assert.Equal(4, x.SpeciesId));
        Assert.Equal(3, after[0].AnimalVariantId);
        Assert.Null(after[2].AnimalVariantId);
        Assert.Null(after[4].SpeciesId);
        Assert.Null(after[5].AnimalGroupId);
        Assert.Null(after[6].AnimalGroupId);
        Assert.Equal(4, after[7].SpeciesId);
        Assert.Equal(1, after[8].AnimalVariantId);
        Assert.Null(after[9].SpeciesId);
        var report = await db.Database.SqlQueryRaw<MigrationReport>("SELECT * FROM \"AnimalReferenceMigrationSummary\"").SingleAsync();
        Assert.Equal(10, report.AnimalsInspected);
        Assert.Equal(4, report.SpeciesMatched);
        Assert.Equal(1, report.SpeciesAmbiguous);
        Assert.Equal(1, report.VariantsMatched);
        Assert.Equal(1, report.VariantsAmbiguous);
        Assert.Equal(8, report.GroupsMapped);
        Assert.False(report.ClassificationValidated);
        // Re-running the same SQL preserves classifications and the original audit snapshot.
        await using (var transaction = await db.Database.BeginTransactionAsync()) {
            await db.Database.ExecuteSqlRawAsync(MigrateLegacyAnimalReferences.Sql.Replace("{", "{{").Replace("}", "}}"));
            await transaction.CommitAsync();
        }
        var service = new AnimalService(db);
        var read = (await service.GetByIdAsync(after[7].Id))!;
        Assert.Equal("Ball python", read.SpeciesName);
        Assert.Equal("Conflicting text", read.OriginalSpeciesText);
        var edited = await service.UpdateAsync(read.Id, new UpdateAnimalRequestDto { EnclosureId = enclosure.Id, Name = "Renamed", Status = read.Status, Sex = read.Sex, AcquiredDate = read.AcquiredDate });
        Assert.Equal(UpdateAnimalResultStatus.Updated, edited.Status);
        Assert.Equal("Conflicting text", edited.Animal!.OriginalSpeciesText);
        Assert.Equal("Conflicting morph", edited.Animal.OriginalVariantText);
        Assert.Equal(4, edited.Animal.SpeciesId);
        Assert.Equal(3, edited.Animal.AnimalVariantId);
        Assert.Contains((await service.SearchAsync(new() { AnimalGroupIds = [3] })).Items, x => x.Id == read.Id);
    }

    [Fact]
    public async Task EmptyMigration_IsValidated_AndCustomApiValuesRemainExact()
    {
        await using var db = new ProfessorDbContext(await database.CreateDatabaseAsync());
        var report = await db.Database.SqlQueryRaw<MigrationReport>("SELECT * FROM \"AnimalReferenceMigrationSummary\"").SingleAsync();
        Assert.Equal(0, report.AnimalsInspected);
        Assert.True(report.ClassificationValidated);
        Assert.False(db.Database.HasPendingModelChanges());
        var enclosure = new Enclosure { Name = "Test", Location = "Test", Type = EnclosureType.Terrarium, Mobility = EnclosureMobility.Fixed, Status = EnclosureStatus.Active, CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow };
        db.Enclosures.Add(enclosure); await db.SaveChangesAsync();
        var service = new AnimalService(db);
        var request = new CreateAnimalRequestDto { EnclosureId = enclosure.Id, Name = "Custom", CustomSpeciesName = "  Rare hybrid  ", CustomVariantName = " Local strain ", Status = AnimalStatus.Active, Sex = AnimalSex.Unknown, AcquiredDate = new(2026, 1, 1) };
        var created = (await service.CreateAsync(request)).Animal!;
        Assert.Equal(request.CustomSpeciesName, created.CustomSpeciesName);
        Assert.Equal(request.CustomVariantName, created.CustomVariantName);
        request.SpeciesId = 4;
        Assert.Equal(CreateAnimalResultStatus.InvalidReference, (await service.CreateAsync(request)).Status);
        var update = new UpdateAnimalRequestDto { EnclosureId = enclosure.Id, Name = "Custom", Status = created.Status, Sex = created.Sex, AcquiredDate = created.AcquiredDate, CustomVariantName = null, AnimalVariantId = null };
        var edited = (await service.UpdateAsync(created.Id, update)).Animal!;
        Assert.Null(edited.CustomVariantName);
        Assert.Equal(created.CustomSpeciesName, edited.CustomSpeciesName);
        var structured = (await service.CreateAsync(new CreateAnimalRequestDto { EnclosureId = enclosure.Id, Name = "Historical group", SpeciesId = 4, Status = AnimalStatus.Active, Sex = AnimalSex.Unknown, AcquiredDate = new(2026, 1, 1) })).Animal!;
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Animals\" SET \"AnimalGroupId\" = NULL, \"AnimalType\" = 'Herptile' WHERE \"Id\" = {structured.Id}");
        db.ChangeTracker.Clear();
        var variantEdit = (await service.UpdateAsync(structured.Id, new UpdateAnimalRequestDto { EnclosureId = enclosure.Id, Name = structured.Name, Status = structured.Status, Sex = structured.Sex, AcquiredDate = structured.AcquiredDate, AnimalVariantId = 3 })).Animal!;
        Assert.Null(variantEdit.AnimalGroupId);
        Assert.Equal(AnimalType.Herptile, variantEdit.AnimalType);
        Assert.Equal(3, variantEdit.AnimalVariantId);
    }

    [Fact]
    public async Task Migration_HandlesNullsAndInvalidHistoricalIds_WithoutInventingReferences()
    {
        await using var db = new ProfessorDbContext(await database.CreateDatabaseAsync());
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261017184321_ExpandAnimalReferenceCatalog");
        // Simulate a historical database with relaxed nullability and pre-existing broken references.
        await db.Database.ExecuteSqlRawAsync("""
            ALTER TABLE "Animals" ALTER COLUMN "Species" DROP NOT NULL;
            INSERT INTO "Enclosures" ("Name", "Type", "Location", "Mobility", "Status", "CreatedDate", "UpdatedDate")
            VALUES ('Fixture', 'Terrarium', 'Room', 'Fixed', 'Active', NOW(), NOW());
            INSERT INTO "Species" ("ScientificName", "CommonName", "AnimalGroupId", "IsActive")
            VALUES ('Python regius', 'Distinct common name', 3, true);
            SET session_replication_role = replica;
            INSERT INTO "Animals" ("EnclosureId", "Name", "Species", "AnimalType", "Status", "Sex", "AcquiredDate", "BirthDateIsEstimated", "CreatedAt", "UpdatedAt", "SpeciesId", "AnimalVariantId")
            VALUES (1, 'Null fields', NULL, 'Other', 'Active', 'Unknown', DATE '2026-01-01', false, NOW(), NOW(), NULL, NULL),
                   (1, 'Invalid references', 'Preserve this', 'Unknown historical group', 'Active', 'Unknown', DATE '2026-01-01', false, NOW(), NOW(), 99999, 99999),
                   (1, 'Duplicate scientific name', 'Python regius', 'Reptile', 'Active', 'Unknown', DATE '2026-01-01', false, NOW(), NOW(), NULL, NULL);
            SET session_replication_role = origin;
            """);
        await migrator.MigrateAsync();
        Assert.Equal(1, await db.Database.SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM \"Animals\" WHERE \"Species\" IS NULL AND \"SpeciesId\" IS NULL AND \"AnimalVariantId\" IS NULL").SingleAsync());
        var issues = await db.Database.SqlQueryRaw<string>("SELECT unnest(\"Issues\") AS \"Value\" FROM \"AnimalReferenceMigrationAudit\"").ToListAsync();
        Assert.Equal(1, await db.Database.SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM \"AnimalReferenceMigrationAudit\" WHERE \"SpeciesOutcome\" = 'ambiguous'").SingleAsync());
        Assert.Contains("invalid-species-id", issues);
        Assert.Contains("invalid-variant-id", issues);
        Assert.Contains("unmapped-group", issues);
        Assert.False((await db.Database.SqlQueryRaw<MigrationReport>("SELECT * FROM \"AnimalReferenceMigrationSummary\"").SingleAsync()).ClassificationValidated);
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => migrator.MigrateAsync("20261017184321_ExpandAnimalReferenceCatalog"));
    }

    private static Task<List<string>> UnchangedColumns(ProfessorDbContext db) => db.Database.SqlQueryRaw<string>("""
        SELECT (to_jsonb(animal) - 'AnimalGroupId' - 'SpeciesId' - 'AnimalVariantId')::text AS "Value"
        FROM "Animals" animal ORDER BY "Id"
        """).ToListAsync();

    public class MigrationReport
    {
        public long AnimalsInspected { get; set; }
        public long SpeciesMatched { get; set; }
        public long SpeciesAmbiguous { get; set; }
        public long VariantsMatched { get; set; }
        public long VariantsAmbiguous { get; set; }
        public long GroupsMapped { get; set; }
        public bool ClassificationValidated { get; set; }
    }
}
