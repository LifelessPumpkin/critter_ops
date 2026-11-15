using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using skipper_api.Data;

namespace HowellSync.Execution;

public sealed record DevelopmentCatalogProfile
{
    public string Name { get; init; } = "";
    public string[] Genera { get; init; } = [];
    public int MinimumSpecies { get; init; }
    public int TargetSpecies { get; init; }
    public int MaximumSpecies { get; init; }
    public int MaximumExaminedRecords { get; init; }
    // Presets use the provider's existing setting names and validation, not another HTTP client.
    public Dictionary<string, string?> Provider { get; init; } = new();

    public static DevelopmentCatalogProfile Read(IConfiguration config, string name)
    {
        if (name is not ("demo" or "expanded")) throw new ArgumentException("Unknown development profile; use demo or expanded.");
        var profile = config.GetSection($"DevelopmentCatalog:Profiles:{name}").Get<DevelopmentCatalogProfile>()
            ?? throw new ArgumentException("Development profile is not configured.");
        profile.Validate(name);
        return profile;
    }
    public void Validate(string name)
    {
        if (Name != name || Genera.Length is < 1 or > 30 || Genera.Distinct().Count() != Genera.Length ||
            Genera.Any(x => !Regex.IsMatch(x, "^[A-Z][a-z]{1,39}$")) ||
            MinimumSpecies < (name == "demo" ? 100 : 1000) || TargetSpecies < MinimumSpecies ||
            MaximumSpecies < TargetSpecies || MaximumSpecies > (name == "demo" ? 200 : 5000) ||
            MaximumExaminedRecords < MaximumSpecies || MaximumExaminedRecords > 20000)
            throw new ArgumentException("Invalid profile identity, genera, or independent record bounds.");
        string[] required = ["MaximumRequestsPerRun", "MinimumRequestInterval", "MaximumConcurrentRequests", "PageSize", "RequestTimeout"];
        if (required.Any(x => !Provider.ContainsKey(x)) || Provider.Keys.Any(x => !required.Contains(x)))
            throw new ArgumentException("Profile must explicitly configure the five supported provider limits.");
    }
    public IConfigurationSection ProviderConfiguration(IConfiguration config) => new ConfigurationBuilder()
        .AddConfiguration(config)
        .AddInMemoryCollection(Provider.Select(x => new KeyValuePair<string, string?>("Providers:gbif:" + x.Key, x.Value)))
        .Build().GetSection("Providers:gbif");

    public bool ContainsCanonicalName(string? name) => name is not null && Genera.Any(g => name.StartsWith(g + " ", StringComparison.Ordinal));

    // Only accepted provider usages with matching canonical genus names qualify. Synonym references,
    // unlinked seeds, other datasets, and unrelated genera cannot satisfy a profile's minimum.
    public IQueryable<int> SpeciesIds(ProfessorDbContext db, string dataset)
    {
        IQueryable<int> ids = db.SpeciesExternalReferences.Where(x => false).Select(x => x.SpeciesId);
        foreach (var genus in Genera)
        {
            var prefix = genus + " ";
            ids = ids.Union(db.SpeciesExternalReferences.Where(x => x.Provider == "gbif" && x.DatasetIdentifier == dataset &&
                x.ExternalTaxonomicStatus == "ACCEPTED" && x.ExternalCanonicalName != null && x.ExternalCanonicalName.StartsWith(prefix) &&
                x.Species.IsActive && x.Species.AnimalGroup.IsActive &&
                x.Species.TaxonomicStatus != skipper_api.Domain.Animals.TaxonomicStatus.Synonym &&
                x.Species.TaxonomicStatus != skipper_api.Domain.Animals.TaxonomicStatus.Deprecated)
                .Select(x => x.SpeciesId));
        }
        return ids.Distinct();
    }
}
