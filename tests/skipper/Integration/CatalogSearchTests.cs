using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using skipper_api.Controllers;
using skipper_api.Data;
using skipper_api.Domain.Animals;
using skipper_api.Dtos.Animals;
using Xunit;

namespace Skipper.Api.Tests.Data;

public class CatalogSearchTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static CatalogPageDto<T> Page<T>(ActionResult<CatalogPageDto<T>> result) => Assert.IsType<CatalogPageDto<T>>(Assert.IsType<OkObjectResult>(result.Result).Value);
    [Fact]
    public async Task DisplayNameUsesCuratedThenFirstEnglishThenFirstAvailableName()
    {
        await using var db = new ProfessorDbContext(await fixture.CreateDatabaseAsync());
        var species = new Species { ScientificName = "Display fixture", AnimalGroupId = 3,
            CommonNames = [new() { Name = "Nom français", LanguageCode = "fra" },
                new() { Name = "First English", LanguageCode = "eng" },
                new() { Name = "Second English", LanguageCode = "en" }] };
        db.Species.Add(species);
        await db.SaveChangesAsync();
        var api = new AnimalCatalogController(db);
        async Task<string?> Name() => Assert.Single(Page(await api.Species(new() { Search = "Display fixture" }, default)).Items).CommonName;
        Assert.Equal("First English", await Name());
        species.CommonName = "Curated";
        await db.SaveChangesAsync();
        Assert.Equal("Curated", await Name());
        species.CommonName = null;
        db.SpeciesCommonNames.RemoveRange(species.CommonNames.Where(n => n.LanguageCode != "fra"));
        await db.SaveChangesAsync();
        Assert.Equal("Nom français", await Name());
        db.SpeciesCommonNames.RemoveRange(species.CommonNames);
        await db.SaveChangesAsync();
        Assert.Null(await Name());
    }

    [Fact]
    public async Task NamesAliasesRankingWhitespaceAndPagination()
    {
        await using var db = new ProfessorDbContext(await fixture.CreateDatabaseAsync());
        var species = new Species { ScientificName = "Royal test", CommonName = "Royal", AnimalGroupId = 3, PreferredVariantType = VariantType.Morph,
            CommonNames = [new() { Name = "Royal python" }, new() { Name = "Python royal" }],
            Aliases = [new() { Name = "Ancient python", AliasType = SpeciesAliasType.Synonym }, new() { Name = "Royal python", AliasType = SpeciesAliasType.Historical }] };
        db.Species.Add(species);
        await db.SaveChangesAsync();
        var api = new AnimalCatalogController(db);
        foreach (var term in new[] { " ROYAL\t  python ", "Ancient PYTHON", "Royal test" })
        {
            var item = Assert.Single(Page(await api.Species(new() { Search = term }, default)).Items);
            Assert.Equal(species.Id, item.Id); Assert.Equal("Royal test", item.ScientificName); Assert.Equal(VariantType.Morph, item.PreferredVariantType);
        }
        Assert.Equal(4, Page(await api.Species(new() { Search = "python regius" }, default)).Items[0].Id);
        Assert.Empty(Page(await api.Species(new() { Search = "ancient", AnimalGroupId = 1 }, default)).Items);
        Assert.Empty(Page(await api.Species(new() { Search = "unlikely absent" }, default)).Items);
        Assert.Empty(Page(await api.Species(new() { Search = "_" }, default)).Items);
        Assert.Equal(Page(await api.Species(new(), default)).Items, Page(await api.Species(new() { Search = " \t " }, default)).Items);
        foreach (var request in new[] { new CatalogSearchRequestDto { Page = 0 }, new() { Page = -1 }, new() { Page = int.MaxValue }, new() { PageSize = 0 }, new() { PageSize = 101 }, new() { Page = 10000 } })
        {
            var bad = Assert.IsAssignableFrom<ObjectResult>((await new AnimalCatalogController(db).Species(request, default)).Result);
            Assert.Equal(400, bad.StatusCode);
            Assert.Equal(400, Assert.IsAssignableFrom<ObjectResult>((await new AnimalCatalogController(db).Variants(request, default)).Result).StatusCode);
        }
        Assert.IsType<BadRequestObjectResult>((await new AnimalCatalogController(db).Variants(new(), default)).Result);
        Assert.Empty(Page(await api.Variants(new() { SpeciesId = species.Id }, default)).Items);
        Assert.Equal("Albino", Assert.Single(Page(await api.Variants(new() { SpeciesId = 4, Search = "  ALB " }, default)).Items).Name);
    }
    [Fact]
    public async Task RankingLiteralUnicodeAndHardMaximumRemainDeterministic()
    {
        await using var db = new ProfessorDbContext(await fixture.CreateDatabaseAsync());
        db.Species.AddRange(
            new Species { ScientificName = "Rankingterm", AnimalGroupId = 3 },
            new Species { ScientificName = "Ranking second", CommonName = "Rankingterm", AnimalGroupId = 3 },
            new Species { ScientificName = "Ranking third", AnimalGroupId = 3, Aliases = [new() { Name = "Rankingterm" }] },
            new Species { ScientificName = "Ranking fourth", CommonName = "Rankingterm prefix", AnimalGroupId = 3 },
            new Species { ScientificName = "Ranking fifth", CommonName = "Partial Rankingterm", AnimalGroupId = 3 },
            new Species { ScientificName = "Unicode fixture", CommonName = "Élan 100%_wild", AnimalGroupId = 1 });
        db.Species.AddRange(Enumerable.Range(1, 120).Select(i => new Species { ScientificName = "Bounded fixture " + i.ToString("D3"), AnimalGroupId = 1 }));
        await db.SaveChangesAsync();
        var api = new AnimalCatalogController(db);
        Assert.Equal(new[] { "Rankingterm", "Ranking second", "Ranking third", "Ranking fourth", "Ranking fifth" },
            Page(await api.Species(new() { Search = "rankingterm" }, default)).Items.Select(x => x.ScientificName));
        Assert.Single(Page(await api.Species(new() { Search = "élan 100%_" }, default)).Items);
        var first = Page(await api.Species(new() { Search = "Bounded fixture", PageSize = 100 }, default));
        var next = Page(await api.Species(new() { Search = "Bounded fixture", PageSize = 100, Page = 2 }, default));
        Assert.Equal(100, first.Items.Count); Assert.True(first.HasMore);
        Assert.Equal(20, next.Items.Count); Assert.False(next.HasMore);
        Assert.Empty(first.Items.Select(x => x.Id).Intersect(next.Items.Select(x => x.Id)));
    }

}
