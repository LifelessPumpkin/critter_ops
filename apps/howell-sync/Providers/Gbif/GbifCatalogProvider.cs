using HowellSync.Commands;
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
    public void ValidateCommand(CatalogCommand command)
    {
        if ((command.Query is null) == (command.Taxon is null) || command.Taxon <= 0 || command.Limit is < 1 or > 1000 ||
            command.Query is not null && (string.IsNullOrWhiteSpace(command.Query) || command.Query.Length > 300))
            throw new ArgumentException("GBIF requires one bounded --query or --taxon scope.");
    }

    public async Task ExecuteAsync(CatalogSyncOperation operation, CatalogExecutionContext context, CancellationToken token)
    {
        var settings = options ?? throw new InvalidOperationException("Provider configuration was not validated.");
        ValidateCommand(context.Command);
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
            await context.RecordIssueAsync(error.Reason, $"GBIF import stopped after {api.Attempts} HTTP attempts ({api.ThrottledResponses} throttled responses); committed records remain. Restart the same bounded scope; no durable cursor is claimed.", CatalogIssueSeverity.Error, token, settings.DatasetIdentifier, currentIdentifier);
            throw;
        }
        finally
        {
            logger.LogInformation("GBIF used {Attempts} HTTP attempts; {ThrottledResponses} throttled responses", api.Attempts, api.ThrottledResponses);
        }
    }
}
