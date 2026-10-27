using HowellSync.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using skipper_api.Data;
using skipper_api.Domain.Animals;

namespace HowellSync.Providers.Gbif;

// All HTTP has finished before this service is invoked inside the existing batch transaction.
public sealed class GbifSpeciesReconciler(ILogger logger)
{
    // Dry runs retain proposed entities across read-only batches, so later records see earlier proposals.
    private readonly List<Species> proposed = [];
    public async Task<CatalogCounts> ApplyAsync(ProfessorDbContext db, CatalogExecutionContext context,
        GbifTaxon taxon, IReadOnlyList<GbifVernacularName> names, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var now = DateTime.UtcNow;
        var id = taxon.Key?.ToString();
        var dataset = Guid.TryParse(taxon.DatasetKey, out var guid) ? guid.ToString() : null;
        void Issue(string type, string description)
        {
            logger.LogWarning("GBIF record {ExternalIdentifier}: {IssueType}; {Description}; dry run {DryRun}", id, type, description, context.DryRun);
            if (!context.DryRun) db.CatalogSyncIssues.Add(new CatalogSyncIssue {
                CatalogSyncRunId = context.RunId, Provider = "gbif", DatasetIdentifier = dataset, ExternalIdentifier = id,
                IssueType = type, Description = description, CreatedAt = now });
        }
        CatalogCounts Skip(string type, string description) { Issue(type, description); return new(Processed: 1, Skipped: 1); }
        if (taxon.Key is null or <= 0 || dataset is null)
            return Skip("MalformedTaxon", "A positive usage key and dataset UUID are required; no provenance was inferred.");

        var reference = await db.SpeciesExternalReferences.Include(x => x.Species).ThenInclude(x => x.CommonNames)
            .Include(x => x.Species).ThenInclude(x => x.Aliases).Include(x => x.Species).ThenInclude(x => x.ExternalReferences)
            .SingleOrDefaultAsync(x => x.Provider == "gbif" && x.DatasetIdentifier == dataset && x.ExternalIdentifier == id, token);
        reference = proposed.SelectMany(x => x.ExternalReferences).SingleOrDefault(x => x.Provider == "gbif" && x.DatasetIdentifier == dataset && x.ExternalIdentifier == id) ?? reference;
        var oldStatus = reference?.ExternalTaxonomicStatus;
        void Metadata(SpeciesExternalReference value)
        {
            value.ExternalScientificName = taxon.ScientificName;
            value.ExternalTaxonomicStatus = taxon.TaxonomicStatus;
            value.ExternalAcceptedIdentifier = taxon.AcceptedKey?.ToString();
            value.LastCheckedAt = now; value.UpdatedAt = now;
        }
        if (taxon.ScientificName?.Length > 500 || taxon.TaxonomicStatus?.Length > 200)
            return Skip("MalformedTaxon", "Provider name or status exceeds catalog storage bounds.");
        // Preserve the original link even when the provider changes status. Never merge/reassign automatically.
        if (reference is not null)
        {
            Metadata(reference);
            if (oldStatus != taxon.TaxonomicStatus ||
                await db.CatalogSyncIssues.AnyAsync(x => x.Provider == "gbif" && x.DatasetIdentifier == dataset && x.ExternalIdentifier == id &&
                    x.IssueType == "TaxonomyChanged" && x.Status != CatalogIssueStatus.Resolved && x.Status != CatalogIssueStatus.Ignored, token) || taxon.Rank != "SPECIES" || taxon.Deleted is not null)
                return Skip("TaxonomyChanged", "Provider metadata refreshed; existing Species relationship retained for review.");
        }
        if (taxon.Rank != "SPECIES" || taxon.Deleted is not null)
            return Skip("UnsupportedRankOrDeleted", "Only non-deleted species-rank records are supported.");
        var scientific = GbifTaxonMapper.Clean(taxon.ScientificName);
        var canonical = GbifTaxonMapper.Clean(taxon.CanonicalName);
        var localName = canonical.Length > 0 ? canonical : scientific;
        if (scientific.Length == 0 || localName.Length is 0 or > 100)
            return Skip("InvalidScientificName", "Scientific name is missing or the selected local name exceeds 100 characters.");
        var synonym = GbifTaxonMapper.IsSynonym(taxon.TaxonomicStatus);
        if (taxon.TaxonomicStatus != "ACCEPTED" && !synonym)
            return Skip("UnsupportedTaxonomicStatus", "Status is unsupported, doubtful, misapplied, pro-parte, or unknown; no canonical species created.");

        Species? species = reference?.Species;
        var created = false;
        if (synonym)
        {
            var acceptedId = taxon.AcceptedKey?.ToString();
            if (taxon.AcceptedKey is null or <= 0 || taxon.AcceptedKey == taxon.Key)
                return Skip("UnresolvedSynonym", "Missing or self-referencing accepted usage key.");
            var accepted = await db.SpeciesExternalReferences.Include(x => x.Species).ThenInclude(x => x.Aliases)
                .Include(x => x.Species).ThenInclude(x => x.CommonNames).Include(x => x.Species).ThenInclude(x => x.ExternalReferences)
                .SingleOrDefaultAsync(x => x.Provider == "gbif" && x.DatasetIdentifier == dataset && x.ExternalIdentifier == acceptedId, token);
            accepted = proposed.SelectMany(x => x.ExternalReferences).SingleOrDefault(x => x.Provider == "gbif" && x.DatasetIdentifier == dataset && x.ExternalIdentifier == acceptedId) ?? accepted;
            if (accepted is null || accepted.ExternalTaxonomicStatus != "ACCEPTED" ||
                await db.CatalogSyncIssues.AnyAsync(x => x.Provider == "gbif" && x.DatasetIdentifier == dataset && x.ExternalIdentifier == acceptedId &&
                    x.IssueType == "TaxonomyChanged" && x.Status != CatalogIssueStatus.Resolved && x.Status != CatalogIssueStatus.Ignored, token))
                return Skip("UnresolvedSynonym", $"Accepted usage {acceptedId} is not resolved locally in this dataset. Import it first, then restart this scope.");
            if (species is not null && !ReferenceEquals(species, accepted.Species) && (species.Id == 0 || species.Id != accepted.Species.Id))
                return Skip("SynonymRelationshipConflict", "Existing external reference retained; accepted relationship requires manual review.");
            species = accepted.Species;
        }
        else
        {
            var groupName = GbifTaxonMapper.Group(taxon);
            var group = groupName is null ? null : await db.AnimalGroups.SingleOrDefaultAsync(x => x.Name == groupName && x.IsActive, token);
            if (group is null) return Skip("UnrecognizedAnimalGroup", "Classification cannot be confidently mapped to an active existing AnimalGroup.");
            // PostgreSQL performs whitespace/case normalization; no fuzzy matching and no full catalog materialization.
            var scientificKey = GbifTaxonMapper.Key(scientific);
            var canonicalKey = GbifTaxonMapper.Key(localName);
            var candidates = await db.Species.FromSqlInterpolated($"SELECT * FROM \"Species\" WHERE lower(btrim(regexp_replace(\"ScientificName\", '\\s+', ' ', 'g'))) IN ({scientificKey}, {canonicalKey})")
                .Include(x => x.CommonNames).Include(x => x.Aliases).Include(x => x.ExternalReferences).ToListAsync(token);
            candidates.RemoveAll(x => x.Id != 0 && proposed.Any(p => p.Id == x.Id));
            candidates.AddRange(proposed.Where(x => GbifTaxonMapper.Key(x.ScientificName) == scientificKey || GbifTaxonMapper.Key(x.ScientificName) == canonicalKey)
                .Where(x => !candidates.Any(c => ReferenceEquals(c, x) || (x.Id != 0 && c.Id == x.Id))));
            if (species is null)
            {
                if (candidates.Count > 1 || candidates.Any(x => x.AnimalGroupId != group.Id))
                    return Skip("AmbiguousSpeciesMatch", "Scientific/canonical names have multiple matches or incompatible classification; no merge performed.");
                species = candidates.SingleOrDefault();
                if (species is null)
                {
                    species = new Species { ScientificName = localName, AnimalGroupId = group.Id, IsManuallyManaged = false,
                        TaxonomicStatus = TaxonomicStatus.Accepted, CreatedAt = now };
                    created = true;
                    if (!context.DryRun) db.Species.Add(species);
                }
            }
            if (species.AnimalGroupId != group.Id)
                return Skip("ClassificationConflict", "Existing AnimalGroup preserved; classification changes require review.");
            if (GbifTaxonMapper.Key(species.ScientificName) != canonicalKey)
            {
                if (species.IsManuallyManaged || candidates.Any(x => !ReferenceEquals(x, species) && (species.Id == 0 || x.Id != species.Id)))
                    Issue("ScientificNameConflict", "Local scientific name preserved because it is curated or the proposed name conflicts with another Species.");
                else
                {
                    AddAlias(species, species.ScientificName, SpeciesAliasType.Historical, "gbif:" + dataset, now);
                    species.ScientificName = localName;
                }
            }
        }
        if (reference is null)
        {
            reference = new SpeciesExternalReference { Species = species!, Provider = "gbif", DatasetIdentifier = dataset,
                ExternalIdentifier = id!, CreatedAt = now };
            species!.ExternalReferences.Add(reference);
        }
        Metadata(reference);
        var source = "gbif:" + dataset;
        if (synonym)
        {
            if (scientific.Length > 300) Issue("InvalidAlias", "Synonym reference preserved, but the full scientific name exceeds the alias storage bound.");
            else AddAlias(species!, scientific, SpeciesAliasType.Synonym, source, now);
        }
        foreach (var name in names)
        {
            token.ThrowIfCancellationRequested();
            if (name is null) { Issue("InvalidCommonName", "Null common-name entry skipped."); continue; }
            var clean = GbifTaxonMapper.Clean(name.VernacularName);
            var language = string.IsNullOrWhiteSpace(name.Language) ? null : name.Language.Trim().ToLowerInvariant();
            // Include both provider/dataset and source citation when representable; do not silently truncate provenance.
            var attribution = string.IsNullOrWhiteSpace(name.Source) ? source : source + " | " + name.Source.Trim();
            if (clean.Length is 0 or > 300 || language?.Length > 35 || attribution.Length > 300)
            { Issue("InvalidCommonName", "Common name, language, or source exceeds catalog bounds; entry skipped."); continue; }
            if (!species!.CommonNames.Any(x => GbifTaxonMapper.Key(x.Name) == GbifTaxonMapper.Key(clean) &&
                string.Equals(x.LanguageCode?.Trim(), language, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Source?.Trim(), attribution, StringComparison.OrdinalIgnoreCase)))
                species.CommonNames.Add(new SpeciesCommonName { Species = species, Name = clean, LanguageCode = language, Source = attribution, CreatedAt = now });
        }
        // Never choose/overwrite the preferred common name or manually managed status/group.
        if (!species!.IsManuallyManaged) { species.LastSyncedAt = now; species.UpdatedAt = now; }
        if (context.DryRun)
        {
            proposed.RemoveAll(x => x.Id != 0 && x.Id == species.Id);
            if (!proposed.Contains(species)) proposed.Add(species);
        }
        return new(Processed: 1, Created: created ? 1 : 0, Updated: created ? 0 : 1);
    }

    private static void AddAlias(Species species, string name, SpeciesAliasType type, string source, DateTime now)
    {
        if (name.Length > 300) return;
        if (!species.Aliases.Any(x => GbifTaxonMapper.Key(x.Name) == GbifTaxonMapper.Key(name) && x.AliasType == type && x.Source == source))
            species.Aliases.Add(new SpeciesAlias { Species = species, Name = name, AliasType = type, Source = source, CreatedAt = now });
    }
}
