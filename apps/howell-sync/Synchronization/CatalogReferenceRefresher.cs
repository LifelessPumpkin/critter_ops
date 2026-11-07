using System.Text.RegularExpressions;
using HowellSync.Execution;
using Microsoft.EntityFrameworkCore;
using skipper_api.Data;
using skipper_api.Domain.Animals;

namespace HowellSync.Synchronization;

// Provider adapters supply selected normalized fields. No provider DTOs or HTTP inside reconciliation.
public sealed record CatalogName(string Name, string? Language, string Source);
public sealed record CatalogTaxon(string Identifier, string? Dataset, string? ScientificName, string? CanonicalName,
    string? Status, string? AcceptedIdentifier, bool IsSpecies, bool Deleted, bool IsSynonym,
    string? GroupName, IReadOnlyList<CatalogName> Names);

public sealed class CatalogReferenceRefresher
{
    public static string Clean(string? value) => Regex.Replace(value?.Trim() ?? "", @"\s+", " ");
    public static string Key(string? value) => Clean(value).ToLowerInvariant();

    public async Task<CatalogCounts> ApplyAsync(ProfessorDbContext db, CatalogExecutionContext context,
        SpeciesExternalReference reference, CatalogTaxon? taxon, CancellationToken token)
    {
        var species = reference.Species;
        var now = DateTime.UtcNow;
        void Issue(string type, string description, string condition = "") => CatalogIssueRecorder.Add(db, context.RunId,
            reference.Provider, reference.DatasetIdentifier, reference.ExternalIdentifier, type, description, condition, reference);
        reference.LastCheckedAt = now;
        if (taxon is null)
        {
            Issue("MissingExternalRecord", "Provider returned HTTP 404 for this exact identifier; local identity and relationships retained.");
            return new(Processed: 1, Skipped: 1);
        }
        if (reference.ExternalIdentifier != taxon.Identifier || reference.DatasetIdentifier != taxon.Dataset || taxon.Dataset is null)
        {
            Issue("ExternalIdentityConflict", "Identifier/dataset differs or is missing; original reference retained.", taxon.Identifier + ":" + taxon.Dataset);
            return new(Processed: 1, Skipped: 1);
        }
        if (string.IsNullOrWhiteSpace(taxon.ScientificName) || taxon.ScientificName.Length > 500 ||
            taxon.CanonicalName?.Length > 500 || string.IsNullOrWhiteSpace(taxon.Status) || taxon.Status.Length > 200)
        {
            Issue("MalformedTaxon", "Missing or oversized provider name/status; previous metadata retained.");
            return new(Processed: 1, Skipped: 1);
        }
        var changed = Key(reference.ExternalScientificName) != Key(taxon.ScientificName) ||
            Key(reference.ExternalCanonicalName) != Key(taxon.CanonicalName) || reference.ExternalTaxonomicStatus != taxon.Status ||
            reference.ExternalAcceptedIdentifier != taxon.AcceptedIdentifier;
        var statusChanged = reference.ExternalTaxonomicStatus != taxon.Status;
        var acceptedChanged = reference.ExternalAcceptedIdentifier != taxon.AcceptedIdentifier;
        var historical = reference.ExternalCanonicalName;
        if (statusChanged || !taxon.IsSpecies || taxon.Deleted)
            Issue("TaxonomyChanged", "Provider status/rank/deletion changed; internal status and relationships retained for review.",
                $"{taxon.Status}:{taxon.IsSpecies}:{taxon.Deleted}");
        if (acceptedChanged || taxon.IsSynonym)
        {
            var accepted = taxon.AcceptedIdentifier is null ? null : await db.SpeciesExternalReferences.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Provider == reference.Provider && x.DatasetIdentifier == reference.DatasetIdentifier &&
                    x.ExternalIdentifier == taxon.AcceptedIdentifier, token);
            if (acceptedChanged || accepted is null || accepted.SpeciesId != species.Id || accepted.ExternalTaxonomicStatus != "ACCEPTED")
                Issue("AcceptedTaxonConflict", "Accepted relationship changed or cannot be resolved locally; no merge or reassignment performed.", taxon.AcceptedIdentifier ?? "missing");
        }
        if (changed)
        {
            reference.ExternalScientificName = taxon.ScientificName;
            reference.ExternalCanonicalName = taxon.CanonicalName;
            reference.ExternalTaxonomicStatus = taxon.Status;
            reference.ExternalAcceptedIdentifier = taxon.AcceptedIdentifier;
            reference.UpdatedAt = now;
        }
        var source = reference.Provider + ":" + reference.DatasetIdentifier;
        bool Alias(string? name, SpeciesAliasType type)
        {
            var clean = Clean(name);
            if (clean.Length is 0 or > 300 || species.Aliases.Any(x => Key(x.Name) == Key(clean) && x.AliasType == type && Key(x.Source) == Key(source))) return false;
            species.Aliases.Add(new SpeciesAlias { Species = species, Name = clean, AliasType = type, Source = source, CreatedAt = now });
            return true;
        }
        var localName = Clean(string.IsNullOrWhiteSpace(taxon.CanonicalName) ? taxon.ScientificName : taxon.CanonicalName);
        if (Key(historical) != Key(taxon.CanonicalName)) changed |= Alias(historical, SpeciesAliasType.Historical);
        if (taxon.Deleted) changed |= Alias(taxon.ScientificName, SpeciesAliasType.Deprecated);
        if (taxon.IsSynonym) changed |= Alias(taxon.ScientificName, SpeciesAliasType.Synonym);
        var blocked = statusChanged || acceptedChanged || taxon.Deleted || !taxon.IsSpecies || taxon.Status != "ACCEPTED" ||
            await db.CatalogSyncIssues.AnyAsync(x => x.SpeciesExternalReferenceId == reference.Id && x.IssueType == "TaxonomyChanged" &&
                (x.Status == CatalogIssueStatus.Open || x.Status == CatalogIssueStatus.Investigating), token);
        if (!blocked && Key(species.ScientificName) != Key(localName))
        {
            var key = Key(localName);
            var collision = await db.Species.FromSqlInterpolated($"SELECT * FROM \"Species\" WHERE lower(btrim(regexp_replace(\"ScientificName\", '\\s+', ' ', 'g'))) = {key}")
                .AnyAsync(x => x.Id != species.Id, token);
            // Boolean ownership alone cannot distinguish a subsequently edited imported field.
            if (species.IsManuallyManaged || reference.LastAppliedScientificName is null ||
                Key(reference.LastAppliedScientificName) != Key(species.ScientificName) || localName.Length is 0 or > 100 || collision ||
                species.ExternalReferences.Any(x => x.Id != reference.Id && x.LastAppliedScientificName is not null))
                Issue("ScientificNameConflict", "Curated, unknown, competing, or edited field ownership; local scientific name retained.", Key(species.ScientificName) + ":" + key);
            else
            {
                changed |= Alias(species.ScientificName, SpeciesAliasType.Historical);
                species.ScientificName = localName;
                reference.LastAppliedScientificName = localName;
                changed = true;
            }
        }
        var group = taxon.GroupName is null ? null : await db.AnimalGroups.SingleOrDefaultAsync(x => x.Name == taxon.GroupName && x.IsActive, token);
        if (group is null || group.Id != species.AnimalGroupId)
            Issue("ClassificationConflict", "Local AnimalGroup retained; provider classification requires review.", taxon.GroupName ?? "unknown");
        if (taxon.IsSpecies && !taxon.Deleted && taxon.Status == "ACCEPTED")
        {
            var absent = species.CommonNames.Where(x => (x.Source == source || x.Source?.StartsWith(source + " | ", StringComparison.Ordinal) == true) &&
                !taxon.Names.Any(n => Key(n.Name) == Key(x.Name) && Key(n.Language) == Key(x.LanguageCode) && Key(n.Source) == Key(x.Source)))
                .Select(x => $"{Key(x.Name)}:{Key(x.LanguageCode)}:{Key(x.Source)}").OrderBy(x => x, StringComparer.Ordinal).ToArray();
            if (absent.Length > 0)
                Issue("CommonNamesAbsent", "Previously recorded provider names are absent or changed in this response; retained without deletion.", string.Join("\n", absent));
        }
        foreach (var name in taxon.Names)
        {
            var clean = Clean(name.Name);
            if (clean.Length is 0 or > 300 || name.Language?.Length > 35 || name.Source.Length > 300)
            { Issue("InvalidCommonName", "Common name, language or attribution exceeds storage bounds."); continue; }
            if (species.CommonNames.Any(x => Key(x.Name) == Key(clean) && Key(x.LanguageCode) == Key(name.Language) && Key(x.Source) == Key(name.Source))) continue;
            species.CommonNames.Add(new SpeciesCommonName { Species = species, Name = clean, LanguageCode = name.Language, Source = name.Source, CreatedAt = now });
            changed = true;
        }
        // Absence from a response never deletes names. Preferred common name and local group/status remain curated.
        species.LastSyncedAt = now;
        if (changed) species.UpdatedAt = now;
        return new(Processed: 1, Updated: changed ? 1 : 0, Unchanged: changed ? 0 : 1);
    }
}
