using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace HowellSync.Providers.Gbif;

// One instance per execution: every endpoint and retry shares these gates and the attempt budget.
public sealed class GbifApiClient : IDisposable
{
    private readonly HttpClient http;
    private readonly GbifOptions options;
    private readonly ILogger logger;
    private readonly TimeProvider clock;
    private readonly Func<double> jitter;
    private readonly SemaphoreSlim concurrency;
    private readonly SemaphoreSlim pacing = new(1, 1);
    private DateTimeOffset nextRequest;
    private int attempts;
    public int Attempts => attempts;
    public int ThrottledResponses { get; private set; }
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public GbifApiClient(HttpClient http, GbifOptions options, ILogger logger, TimeProvider? clock = null, Func<double>? jitter = null)
    {
        options.Validate();
        this.http = http; this.options = options; this.logger = logger;
        this.clock = clock ?? TimeProvider.System; this.jitter = jitter ?? Random.Shared.NextDouble;
        concurrency = new(options.MaximumConcurrentRequests);
    }

    public Task<GbifTaxon> TaxonAsync(int key, CancellationToken token)
    {
        if (key <= 0) throw new ArgumentException("Invalid usage key.");
        return GetAsync<GbifTaxon>($"species/{key}", token);
    }

    public Task<GbifPage<GbifTaxon>> SearchAsync(string query, int offset, int limit, CancellationToken token) =>
        GetAsync<GbifPage<GbifTaxon>>($"species/search?q={Uri.EscapeDataString(query)}&datasetKey={options.DatasetIdentifier}&offset={offset}&limit={limit}", token);

    public async Task<IReadOnlyList<GbifVernacularName>> NamesAsync(int key, CancellationToken token)
    {
        if (key <= 0) throw new ArgumentException("Invalid usage key.");
        List<GbifVernacularName> names = [];
        var offset = 0;
        HashSet<string> pages = [];
        while (true)
        {
            var page = await GetAsync<GbifPage<GbifVernacularName>>($"species/{key}/vernacularNames?offset={offset}&limit={options.PageSize}", token);
            ValidatePage(page, offset, options.PageSize);
            var signature = JsonSerializer.Serialize(page.Results);
            if (page.Results!.Count > 0 && !pages.Add(signature)) throw new GbifProviderException("NonAdvancingNamesPage");
            names.AddRange(page.Results);
            if (page.EndOfRecords == true) return names;
            offset = checked(offset + page.Results.Count);
        }
    }

    public static void ValidatePage<T>(GbifPage<T> page, int offset, int requested)
    {
        if (page.Offset != offset || page.Limit is null or <= 0 || page.Limit > requested || page.EndOfRecords is null ||
            page.Results is null || page.Results.Count > page.Limit || (page.Results.Count == 0 && page.EndOfRecords != true))
            throw new GbifProviderException("MalformedPagination");
    }

    private async Task<T> GetAsync<T>(string relative, CancellationToken token)
    {
        await concurrency.WaitAsync(token);
        try
        {
            for (var retry = 0; ; retry++)
            {
                await pacing.WaitAsync(token);
                try
                {
                    if (attempts >= options.MaximumRequestsPerRun) throw new GbifProviderException("RequestBudgetExhausted");
                    var delay = nextRequest - clock.GetUtcNow();
                    if (delay > TimeSpan.Zero) await Task.Delay(delay, clock, token);
                    token.ThrowIfCancellationRequested();
                    attempts++;
                    nextRequest = clock.GetUtcNow() + options.MinimumRequestInterval;
                }
                finally { pacing.Release(); }

                TimeSpan? retryAfter = null;
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(options.BaseUrl), relative));
                    request.Headers.UserAgent.ParseAdd(options.UserAgent);
                    using var timeout = new CancellationTokenSource(options.RequestTimeout, clock);
                    using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, timeout.Token);
                    logger.LogInformation("GBIF HTTP attempt {Attempt}, retry {Retry}", attempts, retry);
                    // ResponseContentRead keeps both body download and parsing within the bounded request.
                    using var response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, linked.Token);
                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            return JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync(linked.Token), Json)
                                ?? throw new GbifProviderException("MalformedJson");
                        }
                        catch (JsonException) { throw new GbifProviderException("MalformedJson"); }
                    }
                    var code = (int)response.StatusCode;
                    if (code == 429)
                    {
                        ThrottledResponses++;
                        logger.LogWarning("GBIF throttled attempt {Attempt}", attempts);
                    }
                    if (code is not (429 or 500 or 502 or 503 or 504)) throw new GbifProviderException($"Http{code}");
                    var header = response.Headers.RetryAfter;
                    retryAfter = header?.Delta ?? (header?.Date - clock.GetUtcNow());
                    if (retryAfter > options.MaximumRetryDelay) throw new GbifProviderException("RetryAfterExceedsWaitPolicy");
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested) { }
                catch (HttpRequestException) { }
                token.ThrowIfCancellationRequested();
                if (retry >= options.MaximumRetries) throw new GbifProviderException("RetriesExhausted");
                var backoff = TimeSpan.FromMilliseconds(Math.Min(options.MaximumRetryDelay.TotalMilliseconds,
                    options.InitialRetryDelay.TotalMilliseconds * Math.Pow(2, retry) * (1 + jitter())));
                var wait = retryAfter > backoff ? retryAfter.Value : backoff;
                // Shared cooldown prevents other requests bypassing a throttled request's delay.
                await pacing.WaitAsync(token);
                try { if (nextRequest < clock.GetUtcNow() + wait) nextRequest = clock.GetUtcNow() + wait; }
                finally { pacing.Release(); }
            }
        }
        finally { concurrency.Release(); }
    }

    public void Dispose() { concurrency.Dispose(); pacing.Dispose(); }
}
