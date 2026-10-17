using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using skipper_api.Controllers;
using skipper_api.Data;
using skipper_api.Domain.Animals;
using skipper_api.Domain.Enclosures;
using skipper_api.Dtos.Animals;
using skipper_api.Services.Animals;
using Xunit;

namespace Skipper.Api.Tests.Data;

public class AnimalCatalogIntegrationTests(PostgresFixture database) : IClassFixture<PostgresFixture>
{
    private static T Value<T>(ActionResult<T> result) => Assert.IsAssignableFrom<T>(Assert.IsType<OkObjectResult>(result.Result).Value);

    [Fact]
    public async Task CatalogSearch_IsCaseInsensitivePagedAndScoped_WithNonuniqueOrMissingCommonNames()
    {
        await using var db = new ProfessorDbContext(await database.CreateDatabaseAsync());
        var api = new AnimalCatalogController(db);
        var groups = Value(await api.Groups(default));
        Assert.Equal(7, groups.Count);
        Assert.Contains(groups, x => x.Name == "Bird");
        foreach (var term in new[] { "BALL PYTHON", "python REGIUS" })
        {
            var page = Value(await api.Species(new() { Search = term }, default));
            Assert.Equal(4, Assert.Single(page.Items).Id);
        }
        Assert.Empty(Value(await api.Species(new() { Search = "python", AnimalGroupId = 1 }, default)).Items);
        var first = Value(await api.Species(new() { PageSize = 2 }, default));
        var second = Value(await api.Species(new() { PageSize = 2, Page = 2 }, default));
        Assert.True(first.HasMore);
        Assert.Equal(2, first.Items.Count);
        Assert.Empty(first.Items.Select(x => x.Id).Intersect(second.Items.Select(x => x.Id)));
        db.Species.AddRange(
            new Species { CommonName = null, ScientificName = "Testus alpha", AnimalGroupId = 7 },
            new Species { CommonName = "Shared name", ScientificName = "Testus beta", AnimalGroupId = 7 },
            new Species { CommonName = "Shared name", ScientificName = "Testus gamma", AnimalGroupId = 7 });
        await db.SaveChangesAsync();
        Assert.Null(Assert.Single(Value(await api.Species(new() { Search = "Testus alpha" }, default)).Items).CommonName);
        Assert.Equal(2, Value(await api.Species(new() { Search = "Shared name" }, default)).Items.Count);
        Assert.Single(Value(await api.Variants(new() { Search = "ALBINO", SpeciesId = 4 }, default)).Items);
        Assert.Empty(Value(await api.Variants(new() { Search = "Albino", SpeciesId = 1 }, default)).Items);
        Assert.Equal(2, Value(await api.Variants(new() { SpeciesId = 1 }, default)).Items.Count);
        Assert.Single(Value(await api.Variants(new() { SpeciesId = 1, PageSize = 1, Page = 2 }, default)).Items);
        Assert.Empty(Value(await api.Species(new() { Search = "%" }, default)).Items);
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task AnimalReferences_CreateResolveRejectAndClear_WithoutBreakingLegacyEdits()
    {
        await using var db = new ProfessorDbContext(await database.CreateDatabaseAsync());
        var enclosure = Enclosure(); db.Enclosures.Add(enclosure); await db.SaveChangesAsync();
        var service = new AnimalService(db);
        CreateAnimalRequestDto Request() => new() { EnclosureId = enclosure.Id, Name = "Mango", SpeciesId = 4, AnimalGroupId = 3, AnimalVariantId = 3, Status = AnimalStatus.Active, Sex = AnimalSex.Unknown, AcquiredDate = new DateOnly(2026, 1, 1) };
        var created = await service.CreateAsync(Request());
        Assert.Equal(CreateAnimalResultStatus.Created, created.Status);
        Assert.Equal("Ball python", created.Animal!.Species);
        Assert.Equal("Albino", created.Animal.SubspeciesOrMorph);
        Assert.Equal("Reptile", created.Animal.AnimalGroupName);
        var read = await service.GetByIdAsync(created.Animal.Id);
        Assert.Equal("Python regius", read!.SpeciesScientificName);
        var badSpecies = Request(); badSpecies.SpeciesId = 999999;
        Assert.Equal(CreateAnimalResultStatus.InvalidReference, (await service.CreateAsync(badSpecies)).Status);
        var badVariant = Request(); badVariant.AnimalVariantId = 999999;
        Assert.Equal(CreateAnimalResultStatus.InvalidReference, (await service.CreateAsync(badVariant)).Status);
        var wrongVariant = Request(); wrongVariant.AnimalVariantId = 1;
        Assert.Equal(CreateAnimalResultStatus.InvalidReference, (await service.CreateAsync(wrongVariant)).Status);
        var wrongGroup = Request(); wrongGroup.AnimalGroupId = 1;
        Assert.Equal(CreateAnimalResultStatus.InvalidReference, (await service.CreateAsync(wrongGroup)).Status);
        var custom = Request(); custom.SpeciesId = null; custom.AnimalVariantId = null; custom.Species = "Unlisted species"; custom.SubspeciesOrMorph = "Local variety";
        var fallback = await service.CreateAsync(custom);
        Assert.Equal("Unlisted species", fallback.Animal!.Species);
        Assert.Equal("Local variety", fallback.Animal.SubspeciesOrMorph);
        var customVariant = Request(); customVariant.AnimalVariantId = null; customVariant.SubspeciesOrMorph = "Unlisted morph";
        Assert.Equal("Unlisted morph", (await service.CreateAsync(customVariant)).Animal!.SubspeciesOrMorph);
        var update = new UpdateAnimalRequestDto { EnclosureId = enclosure.Id, Name = "Renamed", Species = read.Species, SubspeciesOrMorph = read.SubspeciesOrMorph, AnimalType = read.AnimalType, Status = read.Status, Sex = read.Sex, AcquiredDate = read.AcquiredDate };
        var edited = await service.UpdateAsync(read.Id, update);
        Assert.Equal(4, edited.Animal!.SpeciesId);
        // Catalog renames must not make old clients lose a reference when round-tripping resolved text.
        var renamedVariant = await db.AnimalVariants.SingleAsync(x => x.Id == 3);
        renamedVariant.Name = "Albino (reviewed)";
        await db.SaveChangesAsync();
        update.SubspeciesOrMorph = (await service.GetByIdAsync(read.Id))!.SubspeciesOrMorph;
        Assert.Equal(3, (await service.UpdateAsync(read.Id, update)).Animal!.AnimalVariantId);
        update.SubspeciesOrMorph = "Albino";
        update.SpeciesId = null; update.AnimalVariantId = null;
        var cleared = await service.UpdateAsync(read.Id, update);
        Assert.Null(cleared.Animal!.SpeciesId);
        Assert.Null(cleared.Animal.AnimalVariantId);
        Assert.Equal("Ball python", cleared.Animal.Species);
        Assert.Equal("Albino", cleared.Animal.SubspeciesOrMorph);
        custom.AnimalType = AnimalType.Herptile; custom.AnimalGroupId = null;
        var legacy = await service.CreateAsync(custom);
        Assert.Equal(AnimalType.Herptile, legacy.Animal!.AnimalType);
        Assert.Null(legacy.Animal.AnimalGroupId);
        var search = await service.SearchAsync(new() { Lifecycle = AnimalLifecycle.All, AnimalTypes = [AnimalType.Herptile] });
        Assert.Equal(legacy.Animal.Id, Assert.Single(search.Items).Id);
        var groupSearch = await service.SearchAsync(new() { Lifecycle = AnimalLifecycle.All, AnimalGroupIds = [3] });
        Assert.Contains(groupSearch.Items, x => x.Id == read.Id);
        Assert.DoesNotContain(groupSearch.Items, x => x.Id == legacy.Animal.Id);
    }

    [Fact]
    public async Task Migration_PreservesHistoricalTextAndAmbiguousTypes_AndIsRepeatable()
    {
        var options = await database.CreateDatabaseAsync();
        await using var db = new ProfessorDbContext(options);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261006032003_AddStructuredOperationalActivities");
        var enclosure = Enclosure(); db.Enclosures.Add(enclosure); await db.SaveChangesAsync();
        foreach (var type in new[] { "Herptile", "Aquatic", "Avian", "Mammal" })
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "Animals" ("EnclosureId", "Name", "Species", "SubspeciesOrMorph", "AnimalType", "Status", "Sex", "AcquiredDate", "BirthDateIsEstimated", "CreatedAt", "UpdatedAt")
                VALUES ({enclosure.Id}, {type}, {"Historical species"}, {"Historical morph"}, {type}, 'Active', 'Unknown', DATE '2026-01-01', false, NOW(), NOW())
                """);
        }
        await migrator.MigrateAsync();
        await migrator.MigrateAsync();
        var animals = await db.Animals.AsNoTracking().ToListAsync();
        Assert.Equal(4, animals.Count);
        Assert.All(animals, animal => { Assert.Equal("Historical species", animal.Species); Assert.Equal("Historical morph", animal.SubspeciesOrMorph); Assert.Null(animal.SpeciesId); Assert.Null(animal.AnimalGroupId); Assert.Equal(animal.Name, animal.AnimalType.ToString()); });
        Assert.Equal(16, await db.Species.CountAsync());
        Assert.Equal(4, await db.AnimalVariants.CountAsync());
    }

    [Fact]
    public async Task LargeCatalog_RemainsBoundedAndDeterministic()
    {
        await using var db = new ProfessorDbContext(await database.CreateDatabaseAsync());
        db.Species.AddRange(Enumerable.Range(1, 2000).Select(i => new Species { CommonName = $"Search fixture {i:D4}", ScientificName = $"Fixture taxon {i:D4}", AnimalGroupId = 7 }));
        await db.SaveChangesAsync();
        var api = new AnimalCatalogController(db);
        var page = Value(await api.Species(new() { Search = "fixture", AnimalGroupId = 7, PageSize = 20, Page = 2 }, default));
        Assert.Equal(20, page.Items.Count);
        Assert.True(page.HasMore);
        Assert.Equal("Search fixture 0021", page.Items[0].CommonName);
        Assert.Equal(page.Items.Select(x => x.Id), Value(await api.Species(new() { Search = "fixture", AnimalGroupId = 7, PageSize = 20, Page = 2 }, default)).Items.Select(x => x.Id));
    }

    private static Enclosure Enclosure() => new() { Name = "Habitat", Type = EnclosureType.Terrarium, Location = "Room", Mobility = EnclosureMobility.Fixed, Status = EnclosureStatus.Active, CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow };
}
