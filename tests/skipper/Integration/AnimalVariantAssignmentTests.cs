using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using skipper_api.Data;
using skipper_api.Data.Migrations;
using skipper_api.Domain.Animals;
using skipper_api.Domain.Enclosures;
using skipper_api.Dtos.Animals;
using skipper_api.Services.Animals;
using Xunit;

namespace Skipper.Api.Tests.Data;

public class AnimalVariantAssignmentTests(PostgresFixture database) : IClassFixture<PostgresFixture>
{
    private static async Task<int> Habitat(ProfessorDbContext db) {
        var e = new Enclosure { Name = "Assignments", Location = "Test", Type = EnclosureType.Terrarium, Mobility = EnclosureMobility.Fixed, Status = EnclosureStatus.Active, CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow };
        db.Enclosures.Add(e); await db.SaveChangesAsync(); return e.Id;
    }
    private static CreateAnimalRequestDto Create(int enclosure, List<VariantAssignmentRequestDto> values) => new() {
        EnclosureId = enclosure, Name = "Python", SpeciesId = 4, Status = AnimalStatus.Active,
        Sex = AnimalSex.Unknown, AcquiredDate = new(2026, 1, 1), VariantAssignments = values
    };
    private static UpdateAnimalRequestDto Update(AnimalResponseDto animal, List<VariantAssignmentRequestDto>? values = null) => new() {
        EnclosureId = animal.EnclosureId, Name = "Updated", Status = animal.Status, Sex = animal.Sex,
        AcquiredDate = animal.AcquiredDate, VariantAssignments = values
    };

    [Fact]
    public async Task MixedCollections_ReadReplacePreserveIdentityAndRejectAtomically() {
        await using var db = new ProfessorDbContext(await database.CreateDatabaseAsync());
        var enclosure = await Habitat(db); var service = new AnimalService(db);
        var extra = new AnimalVariant { SpeciesId = 4, Name = "Fixture Pastel", VariantType = VariantType.Morph };
        db.AnimalVariants.Add(extra); await db.SaveChangesAsync();
        var created = (await service.CreateAsync(Create(enclosure, [new() { AnimalVariantId = 3 }, new() { AnimalVariantId = extra.Id }, new() { CustomValue = " Local  Line ", VariantType = VariantType.Strain }, new() { CustomValue = "Other line" }]))).Animal!;
        Assert.Equal(4, created.VariantAssignments.Count);
        Assert.Equal(new[] { 0, 1, 2, 3 }, created.VariantAssignments.Select(x => x.DisplayOrder));
        Assert.Equal(" Local  Line ", created.VariantAssignments[2].CustomValue);
        Assert.Equal(4, (await service.GetByIdAsync(created.Id))!.VariantAssignments.Count);
        Assert.Equal(4, Assert.Single((await service.SearchAsync(new())).Items).VariantAssignments.Count);
        var retained = (await service.UpdateAsync(created.Id, Update(created))).Animal!;
        Assert.Equal(created.VariantAssignments.Select(x => x.AssignmentId), retained.VariantAssignments.Select(x => x.AssignmentId));
        var invalid = Update(created); invalid.SpeciesId = 1; invalid.AnimalGroupId = 1;
        Assert.Equal(UpdateAnimalResultStatus.InvalidReference, (await service.UpdateAsync(created.Id, invalid)).Status);
        Assert.Equal(4, (await service.GetByIdAsync(created.Id))!.SpeciesId);
        var first = created.VariantAssignments[0];
        var custom = created.VariantAssignments[2];
        var edited = (await service.UpdateAsync(created.Id, Update(created, [new() { AnimalVariantId = 3 }, new() { AssignmentId = custom.AssignmentId, CustomValue = "Edited line", VariantType = VariantType.Strain }]))).Animal!;
        Assert.Equal(first.AssignmentId, edited.VariantAssignments[0].AssignmentId);
        Assert.Equal(custom.AssignmentId, edited.VariantAssignments[1].AssignmentId);
        Assert.Equal("Edited line", edited.VariantAssignments[1].DisplayName);
        var variant = await db.AnimalVariants.FindAsync(3); variant!.IsActive = false; await db.SaveChangesAsync();
        Assert.Equal(UpdateAnimalResultStatus.Updated, (await service.UpdateAsync(created.Id, Update(edited))).Status);
        Assert.Equal(CreateAnimalResultStatus.InvalidReference, (await service.CreateAsync(Create(enclosure, [new() { AnimalVariantId = 3 }]))).Status);
        var clear = Update(edited, []); clear.SpeciesId = null; clear.CustomSpeciesName = "Custom species";
        Assert.Empty((await service.UpdateAsync(created.Id, clear)).Animal!.VariantAssignments);
        var customSpecies = Create(enclosure, [new() { CustomValue = "Custom trait" }]); customSpecies.SpeciesId = null; customSpecies.CustomSpeciesName = "Unlisted";
        Assert.Single((await service.CreateAsync(customSpecies)).Animal!.VariantAssignments);
    }

    [Fact]
    public async Task InvalidCollections_DoNotMutateAnimalOrAssignments() {
        await using var db = new ProfessorDbContext(await database.CreateDatabaseAsync());
        var enclosure = await Habitat(db); var service = new AnimalService(db);
        var animal = (await service.CreateAsync(Create(enclosure, [new() { CustomValue = "Original" }]))).Animal!;
        List<VariantAssignmentRequestDto>[] invalid = [
            [new()], [new() { CustomValue = "  " }], [new() { AnimalVariantId = 3, CustomValue = "Both" }],
            [new() { AnimalVariantId = 99999 }], [new() { AnimalVariantId = 1 }],
            [new() { AnimalVariantId = 3 }, new() { AnimalVariantId = 3 }],
            [new() { CustomValue = "Local  Line" }, new() { CustomValue = " local line " }],
            [new() { AssignmentId = 99999, CustomValue = "Other" }],
            [new() { AssignmentId = animal.VariantAssignments[0].AssignmentId, CustomValue = "One" }, new() { AssignmentId = animal.VariantAssignments[0].AssignmentId, CustomValue = "Two" }],
            [new() { CustomValue = "Invalid type", VariantType = (VariantType)999 }], [null!]
        ];
        foreach (var values in invalid) {
            Assert.Equal(UpdateAnimalResultStatus.InvalidReference, (await service.UpdateAsync(animal.Id, Update(animal, values))).Status);
            var read = (await service.GetByIdAsync(animal.Id))!;
            Assert.Equal("Python", read.Name); Assert.Equal("Original", Assert.Single(read.VariantAssignments).DisplayName);
        }
        Assert.Equal(CreateAnimalResultStatus.Created, (await service.CreateAsync(Create(enclosure, []))).Status);
        Assert.Equal(CreateAnimalResultStatus.Created, (await service.CreateAsync(Create(enclosure, [new() { CustomValue = "Same", VariantType = VariantType.Morph }, new() { CustomValue = "same", VariantType = VariantType.Strain }]))).Status);
    }

    [Fact]
    public async Task DatabaseConstraints_EnforceValuesUniquenessAndDeletePolicies() {
        await using var db = new ProfessorDbContext(await database.CreateDatabaseAsync());
        var service = new AnimalService(db);
        var animal = (await service.CreateAsync(Create(await Habitat(db), [new() { AnimalVariantId = 3 }, new() { CustomValue = "Local Line", VariantType = VariantType.Morph }]))).Animal!;
        foreach (var value in new[] { "(3, NULL, NULL)", "(NULL, ' local   LINE ', 'Morph')", "(NULL, NULL, NULL)", "(3, 'Both', 'Morph')", "(NULL, '   ', 'Morph')" }) {
            // SQL fragments are fixed fixture constants, never user input.
#pragma warning disable EF1002
            var error = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync($"INSERT INTO \"AnimalVariantAssignments\" (\"AnimalId\", \"AnimalVariantId\", \"CustomValue\", \"VariantType\", \"DisplayOrder\", \"CreatedAt\", \"UpdatedAt\") SELECT {animal.Id}, v.id::integer, v.custom::text, v.type::text, 0, now(), now() FROM (VALUES {value}) v(id, custom, type)"));
#pragma warning restore EF1002
            Assert.Contains(error.SqlState, new[] { PostgresErrorCodes.UniqueViolation, PostgresErrorCodes.CheckViolation });
        }
        await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync("DELETE FROM \"AnimalVariants\" WHERE \"Id\" = 3"));
        await service.DeleteAsync(animal.Id);
        Assert.Empty(await db.AnimalVariantAssignments.ToListAsync());
    }

    [Fact]
    public async Task Migration_PreservesMixedValuesAndWholeAnimalSnapshot_AndReruns() {
        await using var db = new ProfessorDbContext(await database.CreateDatabaseAsync());
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261017210000_MigrateLegacyAnimalReferences");
        var enclosure = await Habitat(db);
        foreach (var (id, custom) in new (int?, string?)[] { (3, null), (null, " Local Line "), (null, null), (3, "Distinct text"), (3, " ALBINO ") })
            await db.Database.ExecuteSqlInterpolatedAsync($"""
              INSERT INTO "Animals" ("EnclosureId", "Name", "Species", "SpeciesId", "AnimalVariantId", "SubspeciesOrMorph", "AnimalType", "Status", "Sex", "AcquiredDate", "BirthDateIsEstimated", "CreatedAt", "UpdatedAt")
              VALUES ({enclosure}, 'Preserved', 'Python regius', 4, {id}, {custom}, 'Reptile', 'Active', 'Unknown', DATE '2026-01-01', false, now(), now())
              """);
        await migrator.MigrateAsync();
        Assert.Equal(5, await db.AnimalVariantAssignments.CountAsync());
        await using (var tx = await db.Database.BeginTransactionAsync()) {
            await db.Database.ExecuteSqlRawAsync(AddAnimalVariantAssignments.BackfillSql.Replace("{", "{{").Replace("}", "}}")); await tx.CommitAsync();
        }
        Assert.Equal(5, await db.AnimalVariantAssignments.CountAsync());
        Assert.Equal(0, await db.Database.SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM \"Animals\" a JOIN \"AnimalVariantAssignmentMigrationAudit\" m ON a.\"Id\" = m.\"AnimalId\" WHERE to_jsonb(a) <> m.\"Original\"").SingleAsync());
        Assert.Equal(1, await db.Database.SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM \"AnimalVariantAssignmentMigrationAudit\" WHERE NOT \"Validated\"").SingleAsync());
        await Assert.ThrowsAsync<PostgresException>(() => migrator.MigrateAsync("20261017210000_MigrateLegacyAnimalReferences"));
    }
}
