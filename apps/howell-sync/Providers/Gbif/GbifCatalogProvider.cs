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
        if (command.Name == "dev-import")
        {
            if (command.DevelopmentProfile is null || command.Profile is null || command.Query is not null || command.Taxon is not null) throw new ArgumentException("Resolved profile required.");
            command.DevelopmentProfile.Validate(command.Profile);
            if (command.Limit != command.DevelopmentProfile.MaximumExaminedRecords) throw new ArgumentException("Profile bounds must match.");
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
            logger.LogInformation(new EventId(104, "PROGRESS"), "Checked GBIF {Identifier}: processed={Processed}/{Selected} updated={Updated} unchanged={Unchanged} skipped={Skipped} new-issues={Issues}",
                reference?.ExternalIdentifier ?? "missing reference", context.Counts.Processed, ids.Count(x => x > context.ResumeAfter),
                context.Counts.Updated, context.Counts.Unchanged, context.Counts.Skipped, context.Counts.Issues);
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
        var profile = context.Command.DevelopmentProfile;
        var existing = profile is null ? 0 : await context.ReadAsync(db => profile.SpeciesIds(db, settings.DatasetIdentifier).CountAsync(token));
        var qualifying = existing;
        var proposedNames = new HashSet<string>(StringComparer.Ordinal);
        string? currentIdentifier = context.Command.Taxon?.ToString();
        try
        {
            if (operation == CatalogSyncOperation.Synchronization)
            {
                await RefreshAsync(api, context, token);
                return;
            }
            HashSet<int> seen = [];
            string?[] scopes = profile?.Genera.Select(g => (string?)g).ToArray() ?? [context.Command.Query];
            var offsets = new int[scopes.Length];
            var completed = new bool[scopes.Length];
            var examined = 0;
            // Round-robin pages give small profiles category coverage and large profiles bounded expansion.
            while (examined < limit && completed.Any(done => !done) && (profile is null || qualifying < profile.TargetSpecies))
            {
                for (var scopeIndex = 0; scopeIndex < scopes.Length && examined < limit; scopeIndex++)
                {
                    if (completed[scopeIndex] || profile is not null && qualifying >= profile.TargetSpecies) continue;
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
                        // Spread the first target across scopes rather than filling it from one genus.
                        var size = Math.Min(settings.PageSize, limit - examined);
                        if (profile is not null) size = Math.Min(size, Math.Max(1, profile.TargetSpecies / scopes.Length));
                        page = await api.SearchAsync(scopes[scopeIndex]!, offsets[scopeIndex], size, token, profile is not null);
                        GbifApiClient.ValidatePage(page, offsets[scopeIndex], size);
                    }
                    foreach (var result in page.Results!)
                    {
                        token.ThrowIfCancellationRequested();
                        if (profile is not null && qualifying >= Math.Min(profile.TargetSpecies, profile.MaximumSpecies)) break;
                        if (result is null) throw new GbifProviderException("MalformedTaxon");
                        offsets[scopeIndex]++; examined++;
                        currentIdentifier = result.Key?.ToString();
                        context.HttpRequests = api.Attempts; context.HttpRetries = api.Retries;
                        if (result.Key is int usage && !seen.Add(usage))
                        {
                            if (profile is null) throw new GbifProviderException("RepeatedUsageKey");
                            await context.BatchAsync((db, ct) => {
                                CatalogIssueRecorder.Add(db, context.RunId, Name, settings.DatasetIdentifier, usage.ToString(),
                                    "RepeatedUsageKey", "Duplicate provider taxon skipped within the bounded scope.", usage.ToString());
                                return Task.FromResult(new CatalogCounts(Processed: 1, Skipped: 1));
                            }, token);
                            continue;
                        }
                        // Replay/resume skips already persisted eligible references. Refreshing is the separate sync command.
                        if (profile is not null && await context.ReadAsync(db => {
                            var eligibleIds = profile.SpeciesIds(db, settings.DatasetIdentifier);
                            return db.SpeciesExternalReferences.AnyAsync(x =>
                            x.Provider == Name && x.DatasetIdentifier == settings.DatasetIdentifier && x.ExternalIdentifier == currentIdentifier &&
                            x.ExternalTaxonomicStatus == "ACCEPTED" && eligibleIds.Contains(x.SpeciesId), token); }))
                        {
                            await context.BatchAsync((db, ct) => Task.FromResult(new CatalogCounts(Processed: 1, Unchanged: 1)), token);
                            continue;
                        }
                        var taxon = context.Command.Taxon is null && result.Key is > 0 && result.Rank == "SPECIES"
                            ? await api.TaxonAsync(result.Key.Value, token) : result;
                        if (taxon.Key != result.Key) throw new GbifProviderException("MismatchedUsageKey");
                        if (Guid.TryParse(taxon.DatasetKey, out var dataset) && dataset.ToString() != settings.DatasetIdentifier)
                            throw new GbifProviderException("UnexpectedDataset");
                        if (profile is not null && (!profile.ContainsCanonicalName(taxon.CanonicalName) || taxon.Rank != "SPECIES" || taxon.TaxonomicStatus != "ACCEPTED" || taxon.Deleted is not null))
                        {
                            await context.BatchAsync((db, ct) => Task.FromResult(new CatalogCounts(Processed: 1, Skipped: 1)), token);
                            continue;
                        }
                        IReadOnlyList<GbifVernacularName> names = [];
                        if (taxon.Key is > 0 && taxon.Rank == "SPECIES" && taxon.TaxonomicStatus == "ACCEPTED" && taxon.Deleted is null)
                            names = await api.NamesAsync(taxon.Key.Value, token);
                        context.HttpRequests = api.Attempts; context.HttpRetries = api.Retries;
                        var previousCreated = context.Counts.Created;
                        var previousUpdated = context.Counts.Updated;
                        await context.BatchAsync((db, ct) => reconciler.ApplyAsync(db, context, taxon, names, ct), token);
                        if (profile is not null)
                        {
                            if (!context.DryRun) qualifying = await context.ReadAsync(db => profile.SpeciesIds(db, settings.DatasetIdentifier).CountAsync(token));
                            else if ((context.Counts.Created > previousCreated || context.Counts.Updated > previousUpdated) && proposedNames.Add(taxon.CanonicalName!)) qualifying++;
                        }
                        logger.LogInformation(new EventId(104, "PROGRESS"), "Examined GBIF {Identifier} ({Name}): processed={Processed} created={Created} updated={Updated} unchanged={Unchanged} skipped={Skipped} eligible={Eligible}",
                            currentIdentifier, taxon.CanonicalName ?? taxon.ScientificName, context.Counts.Processed, context.Counts.Created,
                            context.Counts.Updated, context.Counts.Unchanged, context.Counts.Skipped, qualifying);
                    }
                    completed[scopeIndex] = page.EndOfRecords == true;
                }
            }
            if (profile is not null && qualifying < profile.MinimumSpecies)
            {
                await context.RecordIssueAsync("ProfileBelowMinimum", "Configured profile scopes did not supply the minimum distinct eligible Species within the examined-record bound.", CatalogIssueSeverity.Error, token, settings.DatasetIdentifier);
                throw new CatalogMinimumException();
            }
            if (api.ThrottledResponses > 0)
                await context.RecordIssueAsync("ProviderThrottled", $"GBIF returned {api.ThrottledResponses} throttled responses; bounded retries recovered.", CatalogIssueSeverity.Information, token, settings.DatasetIdentifier);
            logger.LogDebug("GBIF scope evaluated: {Processed} taxa, {Attempts} HTTP attempts, dry run {DryRun}. Limit is a scope boundary, not a complete taxonomy claim.", context.Counts.Processed, api.Attempts, context.DryRun);
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
            if (profile is not null)
            {
                using var summaryTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                try
                {
                    var persisted = await context.ReadAsync(db => profile.SpeciesIds(db, settings.DatasetIdentifier).CountAsync(summaryTimeout.Token));
                    var total = await context.ReadAsync(db => db.Species.CountAsync(summaryTimeout.Token));
                    logger.LogInformation("Profile={Profile}; existing matching Species={Existing}; {Mode} new Species={Created}; updated={Updated}; unchanged examined={Unchanged}; total distinct eligible={Eligible}; total Professor Species={Total}; HTTP requests={Requests}; retries={Retries}; issues={Issues}; proposed eligible={Proposed}. Final status follows in FINISH.",
                        profile.Name, existing, context.DryRun ? "proposed" : "committed", context.Counts.Created, context.Counts.Updated, context.Counts.Unchanged,
                        persisted, total, api.Attempts, api.Retries, context.Counts.Issues, context.DryRun ? qualifying : persisted);
                }
                catch (Exception error) { logger.LogWarning("Profile summary unavailable ({Type}); inspect persisted history after connectivity is restored.", error.GetType().Name); }
            }
            logger.LogDebug("GBIF used {Attempts} HTTP attempts; {ThrottledResponses} throttled responses", api.Attempts, api.ThrottledResponses);
        }
    }
}
