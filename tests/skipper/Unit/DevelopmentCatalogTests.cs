using HowellSync.Commands;
using HowellSync.Execution;
using Xunit;

namespace MaryAnn.Howell;
public class DevelopmentCatalogTests
{
    [Theory]
    [InlineData(null, "Host=localhost;Database=catalog_dev")]
    [InlineData("Production", "Host=localhost;Database=catalog_dev")]
    [InlineData("Development", "Host=prod.example.org;Database=catalog_dev")]
    [InlineData("Development", "Host=localhost;Database=production")]
    [InlineData("Development", "Host=localhost,prod.example.org;Database=catalog_dev")]
    [InlineData("Development", "")]
    public void RejectsUnsafeTarget(string? environment, string connection) =>
        Assert.ThrowsAny<ArgumentException>(() => DevelopmentCatalogImport.ValidateTarget(environment, connection));
    [Fact]
    public void AcceptsLocalDevelopmentAndParsesBoundedScope()
    {
        DevelopmentCatalogImport.ValidateTarget("Development", "Host=127.0.0.1;Database=catalog_dev");
        var command = CatalogCommand.Parse(["dev-import", "--provider", "gbif", "--dry-run"]);
        Assert.True(command.DryRun); Assert.Equal("demo", command.Profile);
        Assert.Equal("expanded", CatalogCommand.Parse(["dev-import", "--provider", "gbif", "--profile", "expanded"]).Profile);
        Assert.Throws<ArgumentException>(() => CatalogCommand.Parse(["dev-import", "--provider", "gbif", "--profile", "unknown"]));
        Assert.Throws<ArgumentException>(() => CatalogCommand.Parse(["dev-import", "--provider", "gbif", "--limit", "1001"]));
        Assert.Throws<ArgumentException>(() => CatalogCommand.Parse(["dev-import", "--provider", "gbif", "--taxon", "42"]));
    }
    [Fact]
    public void ProfilesEnforceIndependentBoundsAndCanonicalScope()
    {
        var valid = new DevelopmentCatalogProfile { Name = "demo", Genera = ["Python"], MinimumSpecies = 100, TargetSpecies = 150, MaximumSpecies = 200, MaximumExaminedRecords = 1000,
            Provider = new() { ["MaximumRequestsPerRun"] = "1000", ["MinimumRequestInterval"] = "00:00:00.250", ["MaximumConcurrentRequests"] = "1", ["PageSize"] = "100", ["RequestTimeout"] = "00:00:30" } };
        valid.Validate("demo");
        Assert.True(valid.ContainsCanonicalName("Python regius"));
        Assert.False(valid.ContainsCanonicalName("Other python"));
        Assert.False(valid.ContainsCanonicalName("Pythonella regius"));
        Assert.Throws<ArgumentException>(() => (valid with { TargetSpecies = 201 }).Validate("demo"));
        Assert.Throws<ArgumentException>(() => (valid with { MaximumSpecies = 5000 }).Validate("demo"));
        Assert.Throws<ArgumentException>(() => (valid with { MaximumExaminedRecords = 100 }).Validate("demo"));
        Assert.Throws<ArgumentException>(() => (valid with { Genera = ["Python", "Python"] }).Validate("demo"));
        Assert.Throws<ArgumentException>(() => (valid with { Provider = new() }).Validate("demo"));
        Assert.Throws<ArgumentException>(() => (valid with { Name = "expanded" }).Validate("expanded"));
    }
    [Fact]
    public void ExistingMapperRecognizesGbifSquamataWithoutGuessingMissingClasses()
    {
        Assert.Equal("Reptile", HowellSync.Providers.Gbif.GbifTaxonMapper.Group(new() { Kingdom = "Animalia", Class = "Squamata", Phylum = "Chordata" }));
        Assert.Null(HowellSync.Providers.Gbif.GbifTaxonMapper.Group(new() { Kingdom = "Animalia", Phylum = "Chordata" }));
    }

}
