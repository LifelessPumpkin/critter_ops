using HowellSync.Commands;
using HowellSync.Synchronization;
using System.Text.Json;
using HowellSync.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using skipper_api.Domain.Animals;

namespace HowellSync.Providers.Gbif;

public sealed class GbifCatalogProvider(IHttpClientFactory clients, ILogger<GbifCatalogProvider> logger) : ICatalogProvider
{
    private GbifOptions? options;
    public string Name => "gbif";
    public IReadOnlySet<CatalogSyncOperation> SupportedOperations { get; } = new HashSet<CatalogSyncOperation> { CatalogSyncOperation.Import, CatalogSyncOperation.Synchronization };
    public void ValidateConfiguration(IConfigurationSection configuration) => options = GbifOptions.Read(configuration);
    public string? DatasetIdentifier => options?.DatasetIdentifier;
    public CatalogCommand ResolveDefaults(CatalogCommand command) => command with { Limit = command.Limit ?? options!.DefaultImportLimit };
    public void ValidateCommand(CatalogCommand command)
    {
        if (command.RetryRun is not null)
        {
            if (command.Query is not null || command.Taxon is not null || command.Limit is not null)
                throw new ArgumentException("Retry cannot override its stored scope.");
            return;
        }
        if (command.Taxon <= 0 || command.Limit is < 1 or > 1000 ||
            command.Query is not null && (string.IsNullOrWhiteSpace(command.Query) || command.Query.Length > 300) ||
            command.Name == "import" && (command.Query is null) == (command.Taxon is null) ||
            command.Name == "sync" && command.Query is not null)
            throw new ArgumentException("Import needs one explicit scope; sync refreshes existing references only.");
    }

    internal static CatalogTaxon Normalize(GbifTaxon taxon, IReadOnlyList<GbifVernacularName> names)
    {
        var dataset = Guid.TryParse(taxon.DatasetKey, out var guid) ? guid.ToString() : null;
        var source = "gbif:" + dataset;
        return new(taxon.Key?.ToString() ?? "", dataset, taxon.ScientificName, taxon.CanonicalName,
            taxon.TaxonomicStatus, taxon.AcceptedKey?.ToString(), taxon.Rank == "SPECIES", taxon.Deleted is not null,
            GbifTaxonMapper.IsSynonym(taxon.TaxonomicStatus), GbifTaxonMapper.Group(taxon),
            names.Select(n => new CatalogName(n?.VernacularName ?? "", string.IsNullOrWhiteSpace(n?.Language) ? null : n.Language.Trim().ToLowerInvariant(),
                string.IsNullOrWhiteSpace(n?.Source) ? source : source + " | " + n.Source.Trim())).ToArray());
    }

    private async Task RefreshAsync(GbifApiClient api, CatalogExecutionContext context, CancellationToken token)
    {
        var ids = context.ReferenceIds;
        if (!context.HasReferenceScope)
        {
            ids = await context.ReadAsync(db => db.SpeciesExternalReferences.AsNoTracking()
                .Where(x => x.Provider == Name && (x.DatasetIdentifier == options!.DatasetIdentifier || x.DatasetIdentifier == null) &&
                    (context.Command.Taxon == null || x.ExternalIdentifier == context.Command.Taxon.ToString()))
                .OrderBy(x => x.Id).Select(x => x.Id).Take(context.Command.Limit ?? options!.DefaultImportLimit).ToArrayAsync(token));
        }
        context.ReferenceIds = ids;
        await context.BatchAsync(async (db, ct) => {
            if (!context.DryRun) {
                var run = await db.CatalogSyncRuns.SingleAsync(x => x.Id == context.RunId, ct);
                run.ReferenceIds = JsonSerializer.Serialize(ids);
                run.LastCommittedReferenceId = context.ResumeAfter;
            }
            return new CatalogCounts();
        }, token);
        var refresher = new CatalogReferenceRefresher();
        foreach (var id in ids.Where(x => x > context.ResumeAfter))
        {
            var reference = await context.ReadAsync(db => db.SpeciesExternalReferences.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, token));
            context.CurrentExternalIdentifier = reference?.ExternalIdentifier;
            CatalogTaxon? taxon = null;
            var invalid = reference is null || reference.Provider != Name || reference.DatasetIdentifier != options!.DatasetIdentifier ||
                !int.TryParse(reference.ExternalIdentifier, out var parsed) || parsed <= 0;
            if (!invalid)
            {
                GbifTaxon? raw = null;
                try { raw = await api.TaxonAsync(int.Parse(reference!.ExternalIdentifier), token); }
                catch (GbifProviderException error) when (error.Reason == "Http404") { }
                if (raw is not null)
                {
                    IReadOnlyList<GbifVernacularName> names = [];
                    if (raw.Key?.ToString() == reference!.ExternalIdentifier && raw.DatasetKey == options!.DatasetIdentifier &&
                        raw.Rank == "SPECIES" && raw.Deleted is null)
                        names = await api.NamesAsync(raw.Key!.Value, token);
                    taxon = Normalize(raw, names);
                }
            }
            context.HttpRequests = api.Attempts; context.HttpRetries = api.Retries;
            await context.BatchAsync(async (db, ct) => {
                var current = await db.SpeciesExternalReferences.Include(x => x.Species).ThenInclude(x => x.CommonNames)
                    .Include(x => x.Species).ThenInclude(x => x.Aliases).Include(x => x.Species).ThenInclude(x => x.ExternalReferences)
                    .SingleOrDefaultAsync(x => x.Id == id, ct);
                if (invalid || current is null)
                {
                    CatalogIssueRecorder.Add(db, context.RunId, Name, options!.DatasetIdentifier, reference?.ExternalIdentifier,
                        "InvalidExternalReference", "Reference missing, incompatible, or lacks a valid dataset/numeric identifier; no lookup inferred.", id.ToString(), current);
                    return new CatalogCounts(Processed: 1, Skipped: 1);
                }
                return await refresher.ApplyAsync(db, context, current, taxon, ct);
            }, token, checkpoint: id);
        }
    }

    public async Task ExecuteAsync(CatalogSyncOperation operation, CatalogExecutionContext context, CancellationToken token)
    {
        var settings = options ?? throw new InvalidOperationException("Provider configuration was not validated.");

        using var http = clients.CreateClient("gbif");
        using var api = new GbifApiClient(http, settings, logger);
        var reconciler = new GbifSpeciesReconciler(logger);
        var limit = context.Command.Limit ?? settings.DefaultImportLimit;
        await context.BatchAsync(async (db, ct) => {
            if (!context.DryRun) (await db.CatalogSyncRuns.SingleAsync(x => x.Id == context.RunId, ct)).DatasetIdentifier = settings.DatasetIdentifier;
            return new CatalogCounts();
        }, token);
        string? currentIdentifier = context.Command.Taxon?.ToString();
        try
        {
            if (operation == CatalogSyncOperation.Synchronization)
            {
                await RefreshAsync(api, context, token);
                return;
            }
            var offset = 0;
            HashSet<int> seen = [];
            while (offset < limit)
            {
                token.ThrowIfCancellationRequested();
                GbifPage<GbifTaxon> page;
                if (context.Command.Taxon is int key)
                {
                    var taxon = await api.TaxonAsync(key, token);
                    if (taxon.Key != key) throw new GbifProviderException("MismatchedUsageKey");
                    page = new(0, 1, true, [taxon]);
                }
                else
                {
                    currentIdentifier = null;
                    var size = Math.Min(settings.PageSize, limit - offset);
                    page = await api.SearchAsync(context.Command.Query!, offset, size, token);
                    GbifApiClient.ValidatePage(page, offset, size);
                }
                foreach (var result in page.Results!)
                {
                    token.ThrowIfCancellationRequested();
                    if (result is null) throw new GbifProviderException("MalformedTaxon");
                    currentIdentifier = result.Key?.ToString();
                    // Search projections do not guarantee canonicalName/deleted. Fetch the authoritative usage.
                    var taxon = context.Command.Taxon is null && result.Key is > 0 && result.Rank == "SPECIES"
                        ? await api.TaxonAsync(result.Key.Value, token) : result;
                    if (taxon.Key != result.Key) throw new GbifProviderException("MismatchedUsageKey");
                    if (taxon.Key is int usage && !seen.Add(usage)) throw new GbifProviderException("RepeatedUsageKey");
                    if (Guid.TryParse(taxon.DatasetKey, out var dataset) && dataset.ToString() != settings.DatasetIdentifier)
                        throw new GbifProviderException("UnexpectedDataset");
                    IReadOnlyList<GbifVernacularName> names = [];
                    if (taxon.Key is > 0 && taxon.Rank == "SPECIES" && taxon.TaxonomicStatus == "ACCEPTED" && taxon.Deleted is null)
                        names = await api.NamesAsync(taxon.Key.Value, token);
                    context.HttpRequests = api.Attempts; context.HttpRetries = api.Retries;
                    await context.BatchAsync((db, ct) => reconciler.ApplyAsync(db, context, taxon, names, ct), token);
                    offset++;
                }
                if (page.EndOfRecords == true) break;
            }
            if (api.ThrottledResponses > 0)
                await context.RecordIssueAsync("ProviderThrottled", $"GBIF returned {api.ThrottledResponses} throttled responses; bounded retries recovered.", CatalogIssueSeverity.Information, token, settings.DatasetIdentifier);
            logger.LogInformation("GBIF scope evaluated: {Processed} taxa, {Attempts} HTTP attempts, dry run {DryRun}. Limit is a scope boundary, not a complete taxonomy claim.", context.Counts.Processed, api.Attempts, context.DryRun);
        }
        catch (GbifProviderException error)
        {
            context.HttpRequests = api.Attempts; context.HttpRetries = api.Retries;
            await context.RecordIssueAsync(error.Reason, $"GBIF operation stopped after {api.Attempts} HTTP attempts ({api.ThrottledResponses} throttled responses); committed records remain. Retry the stored bounded scope; sync resumes committed references and import safely replays its scope.", CatalogIssueSeverity.Error, token, settings.DatasetIdentifier, context.CurrentExternalIdentifier ?? currentIdentifier);
            if (error.Reason == "RequestBudgetExhausted") throw new CatalogPartialException(error.Reason);
            throw;
        }
        finally
        {
            context.HttpRequests = api.Attempts; context.HttpRetries = api.Retries;
            logger.LogInformation("GBIF used {Attempts} HTTP attempts; {ThrottledResponses} throttled responses", api.Attempts, api.ThrottledResponses);
        }
    }
}
