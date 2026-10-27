using System.Net;
using System.Net.Http.Headers;
using System.Text;
using HowellSync.Commands;
using HowellSync.Providers.Gbif;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MaryAnn.Howell;

public class GbifClientTests
{
    private static GbifOptions Options() => new() { UserAgent = "CritterOps/1.0 (+https://critterops.test/contact)", MinimumRequestInterval = TimeSpan.Zero,
        InitialRetryDelay = TimeSpan.FromSeconds(1), MaximumRetryDelay = TimeSpan.FromSeconds(60) };
    private static HttpResponseMessage Json(string body, int status = 200) => new((HttpStatusCode)status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => send(request, token);
    }

    [Fact]
    public async Task TypedLookupUsesHeaderAndToleratesOptionalFields()
    {
        using var http = new HttpClient(new Handler((request, _) => {
            Assert.Equal("https://api.gbif.org/v1/species/42", request.RequestUri!.AbsoluteUri);
            Assert.Contains("CritterOps/1.0", request.Headers.UserAgent.ToString());
            return Task.FromResult(Json("{\"key\":42}"));
        }));
        using var client = new GbifApiClient(http, Options(), NullLogger.Instance);
        var taxon = await client.TaxonAsync(42, default);
        Assert.Equal(42, taxon.Key); Assert.Null(taxon.CanonicalName); Assert.Equal(1, client.Attempts);
        await Assert.ThrowsAsync<ArgumentException>(() => client.TaxonAsync(-1, default));
    }

    [Theory]
    [InlineData(429, "seconds")]
    [InlineData(429, "date")]
    [InlineData(429, "missing")]
    [InlineData(500, "missing")]
    [InlineData(502, "missing")]
    [InlineData(503, "seconds")]
    [InlineData(504, "missing")]
    public async Task RetryAfterAndFallbackRespectVirtualTime(int status, string header)
    {
        var clock = new TestClock();
        List<DateTimeOffset> starts = [];
        using var http = new HttpClient(new Handler((_, _) => {
            starts.Add(clock.GetUtcNow());
            var response = Json(starts.Count == 1 ? "{}" : "{\"key\":42}", starts.Count == 1 ? status : 200);
            if (starts.Count == 1 && header == "seconds") response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(10));
            if (starts.Count == 1 && header == "date") response.Headers.RetryAfter = new RetryConditionHeaderValue(clock.GetUtcNow().AddSeconds(10));
            return Task.FromResult(response);
        }));
        using var client = new GbifApiClient(http, Options(), NullLogger.Instance, clock, () => 0);
        var work = client.TaxonAsync(42, default);
        Assert.False(work.IsCompleted);
        await clock.Drive(work);
        Assert.Equal(2, client.Attempts);
        Assert.True(starts[1] - starts[0] >= TimeSpan.FromSeconds(header == "missing" ? 1 : 10));
    }

    [Fact]
    public async Task RepeatedThrottlingUsesBoundedExponentialBackoffAndJitter()
    {
        var clock = new TestClock();
        List<DateTimeOffset> starts = [];
        using var http = new HttpClient(new Handler((_, _) => { starts.Add(clock.GetUtcNow()); return Task.FromResult(Json("{}", 429)); }));
        using var client = new GbifApiClient(http, Options(), NullLogger.Instance, clock, () => 0.5);
        var work = client.TaxonAsync(42, default);
        await clock.Drive(work, allowFailure: true);
        Assert.Equal("RetriesExhausted", (await Assert.ThrowsAsync<GbifProviderException>(() => work)).Reason);
        Assert.Equal(4, client.Attempts); Assert.Equal(4, client.ThrottledResponses);
        Assert.Equal(new[] { 1.5, 3.0, 6.0 }, starts.Zip(starts.Skip(1), (a, b) => (b - a).TotalSeconds));
    }

    [Fact]
    public async Task ExcessiveRetryAfterFailsWithoutRetryingEarly()
    {
        using var http = new HttpClient(new Handler((_, _) => {
            var response = Json("{}", 429); response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromHours(1)); return Task.FromResult(response);
        }));
        using var client = new GbifApiClient(http, Options(), NullLogger.Instance);
        Assert.Equal("RetryAfterExceedsWaitPolicy", (await Assert.ThrowsAsync<GbifProviderException>(() => client.TaxonAsync(42, default))).Reason);
        Assert.Equal(1, client.Attempts);
    }

    [Theory]
    [InlineData(400)] [InlineData(401)] [InlineData(403)] [InlineData(404)]
    public async Task PermanentErrorsAreNotRetried(int status)
    {
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(Json("{}", status))));
        using var client = new GbifApiClient(http, Options(), NullLogger.Instance);
        Assert.Equal($"Http{status}", (await Assert.ThrowsAsync<GbifProviderException>(() => client.TaxonAsync(42, default))).Reason);
        Assert.Equal(1, client.Attempts);
    }

    [Theory]
    [InlineData("not json")] [InlineData("null")] [InlineData("{\"key\":\"invalid\"}")]
    public async Task MalformedResponsesAreNotRetried(string body)
    {
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(Json(body))));
        using var client = new GbifApiClient(http, Options(), NullLogger.Instance);
        Assert.Equal("MalformedJson", (await Assert.ThrowsAsync<GbifProviderException>(() => client.TaxonAsync(42, default))).Reason);
        Assert.Equal(1, client.Attempts);
    }

    [Fact]
    public async Task EveryRetryConsumesRequestBudget()
    {
        var options = Options(); options.MaximumRequestsPerRun = 1;
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(Json("{}", 503))));
        using var client = new GbifApiClient(http, options, NullLogger.Instance);
        Assert.Equal("RequestBudgetExhausted", (await Assert.ThrowsAsync<GbifProviderException>(() => client.TaxonAsync(42, default))).Reason);
        Assert.Equal(1, client.Attempts);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NetworkAndTimeoutFailuresAreBounded(bool timeout)
    {
        var clock = new TestClock(); var options = Options(); options.MaximumRetries = 1;
        using var http = new HttpClient(new Handler(async (_, token) => {
            if (timeout) await Task.Delay(Timeout.InfiniteTimeSpan, token);
            throw new HttpRequestException("controlled network failure");
        }));
        using var client = new GbifApiClient(http, options, NullLogger.Instance, clock, () => 0);
        var work = client.TaxonAsync(42, default);
        await clock.Drive(work, allowFailure: true);
        Assert.Equal("RetriesExhausted", (await Assert.ThrowsAsync<GbifProviderException>(() => work)).Reason);
        Assert.Equal(2, client.Attempts);
        if (timeout) Assert.True(clock.GetUtcNow() >= TestClock.Epoch.Add(options.RequestTimeout * 2));
    }

    [Fact]
    public async Task CancellationDoesNotRetryAndCancelsPacing()
    {
        var clock = new TestClock(); var options = Options(); options.MinimumRequestInterval = TimeSpan.FromSeconds(10);
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(Json("{\"key\":42}"))));
        using var client = new GbifApiClient(http, options, NullLogger.Instance, clock);
        await client.TaxonAsync(42, default);
        using var cancel = new CancellationTokenSource();
        var work = client.TaxonAsync(43, cancel.Token); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => work);
        Assert.Equal(1, client.Attempts);
    }

    [Fact]
    public async Task ConcurrencyAndPacingApplyAcrossEndpoints()
    {
        var clock = new TestClock(); var options = Options(); options.MinimumRequestInterval = TimeSpan.FromSeconds(3);
        var entered = new TaskCompletionSource(); var release = new TaskCompletionSource();
        List<DateTimeOffset> starts = [];
        using var http = new HttpClient(new Handler(async (request, token) => {
            starts.Add(clock.GetUtcNow());
            if (starts.Count == 1) { entered.SetResult(); await release.Task.WaitAsync(token); }
            return Json(request.RequestUri!.AbsolutePath.EndsWith("vernacularNames") ? "{\"offset\":0,\"limit\":20,\"endOfRecords\":true,\"results\":[]}" : "{\"key\":42}");
        }));
        using var client = new GbifApiClient(http, options, NullLogger.Instance, clock);
        var one = client.TaxonAsync(42, default); await entered.Task;
        var two = client.NamesAsync(42, default);
        Assert.Single(starts); release.SetResult();
        await clock.Drive(Task.WhenAll(one, two));
        Assert.Equal(2, starts.Count); Assert.True(starts[1] - starts[0] >= TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task VernacularPaginationAndRepeatDetection()
    {
        var calls = 0;
        using var http = new HttpClient(new Handler((request, _) => {
            var offset = calls++;
            Assert.Contains("offset=" + offset, request.RequestUri!.Query);
            return Task.FromResult(Json($$"""{"offset":{{offset}},"limit":1,"endOfRecords":{{(offset == 1 ? "true" : "false")}},"results":[{"vernacularName":"Name {{offset}}","language":"eng"}]}"""));
        }));
        var options = Options(); options.PageSize = 1;
        using var client = new GbifApiClient(http, options, NullLogger.Instance);
        Assert.Equal(2, (await client.NamesAsync(42, default)).Count);
        Assert.Equal(2, client.Attempts);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CancellationDuringHttpOrRetryNeverRetries(bool duringRetry)
    {
        var clock = new TestClock();
        using var cancel = new CancellationTokenSource();
        var entered = new TaskCompletionSource();
        using var http = new HttpClient(new Handler(async (_, token) => {
            entered.TrySetResult();
            if (!duringRetry) await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return Json("{}", 429);
        }));
        using var client = new GbifApiClient(http, Options(), NullLogger.Instance, clock);
        var work = client.TaxonAsync(42, cancel.Token); await entered.Task;
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => work);
        Assert.Equal(1, client.Attempts);
    }

    [Fact]
    public async Task RepeatedVernacularPagesFailBoundedly()
    {
        var calls = 0; var options = Options(); options.PageSize = 1;
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(Json(
            "{\"offset\":" + calls++ + ",\"limit\":1,\"endOfRecords\":false,\"results\":[{\"vernacularName\":\"same\"}]}"))));
        using var client = new GbifApiClient(http, options, NullLogger.Instance);
        Assert.Equal("NonAdvancingNamesPage", (await Assert.ThrowsAsync<GbifProviderException>(() => client.NamesAsync(42, default))).Reason);
        Assert.Equal(2, client.Attempts);
    }

    [Fact]
    public void PaginationRejectsMissingAndNonAdvancingFields()
    {
        foreach (var page in new GbifPage<GbifTaxon>[] {
            new(null, 1, true, []), new(0, null, true, []), new(0, 1, null, []), new(0, 1, false, []),
            new(0, 1, true, null), new(1, 1, true, []), new(0, 2, true, []) })
            Assert.Throws<GbifProviderException>(() => GbifApiClient.ValidatePage(page, 0, 1));
    }

    [Theory]
    [InlineData("import --provider gbif")]
    [InlineData("import --provider gbif --taxon -1")]
    [InlineData("import --provider gbif --query Python --taxon 1")]
    [InlineData("import --provider gbif --query Python --limit 0")]
    [InlineData("import --provider gbif --query Python --limit 1001")]
    [InlineData("validate --query Python")]
    public void UnsafeScopesRejected(string input) => Assert.Throws<ArgumentException>(() => CatalogCommand.Parse(input.Split(' ')));

    [Fact]
    public void ConfigurationAndNormalization()
    {
        Options().Validate();
        foreach (var alter in new Action<GbifOptions>[] { o => o.UserAgent = "", o => o.UserAgent = "CritterOps/1.0 (+https://)", o => o.UserAgent = "CritterOps/1.0 (@)", o => o.BaseUrl = "http://api.gbif.org/", o => o.RequestTimeout = TimeSpan.Zero,
            o => o.MaximumConcurrentRequests = 0, o => o.MaximumRequestsPerRun = 0, o => o.PageSize = 0, o => o.MaximumRetries = -1,
            o => o.DatasetIdentifier = "invalid", o => o.MinimumRequestInterval = TimeSpan.FromSeconds(-1) })
        { var options = Options(); alter(options); Assert.Throws<ArgumentException>(options.Validate); }
        Assert.Equal("python regius", GbifTaxonMapper.Key(" \tPython  regius\n"));
        Assert.Equal("Python regius (Shaw, 1802)", GbifTaxonMapper.Clean("Python regius (Shaw, 1802)"));
        Assert.Null(GbifTaxonMapper.Group(new() { Kingdom = "Plantae", Class = "Mammalia" }));
        Assert.Null(GbifTaxonMapper.Group(new() { Kingdom = "Animalia", Phylum = "Chordata" }));
        Assert.Equal("Invertebrate", GbifTaxonMapper.Group(new() { Kingdom = "Animalia", Phylum = "Arthropoda" }));
    }

    // Deterministic timer scheduler: all retry, timeout and pacing waits run against virtual time.
    private sealed class TestClock : TimeProvider
    {
        public static readonly DateTimeOffset Epoch = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        private DateTimeOffset now = Epoch;
        private readonly List<ClockTimer> timers = [];
        public override DateTimeOffset GetUtcNow() { lock (timers) return now; }
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ClockTimer(this, callback, state); timer.Change(dueTime, period); return timer;
        }
        public async Task Drive(Task work, bool allowFailure = false)
        {
            for (var i = 0; !work.IsCompleted && i < 2000; i++)
            {
                await Task.Delay(1); // Let task continuations register timers before advancing virtual time.
                ClockTimer? next;
                lock (timers)
                {
                    next = timers.Where(t => t.Due is not null).OrderBy(t => t.Due).FirstOrDefault();
                    if (next is not null) { now = next.Due!.Value; next.Due = null; }
                }
                next?.Fire();
            }
            Assert.True(work.IsCompleted, "Virtual-time operation did not terminate.");
            if (!allowFailure) await work;
        }
        private sealed class ClockTimer(TestClock clock, TimerCallback callback, object? state) : ITimer
        {
            public DateTimeOffset? Due;
            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                lock (clock.timers) { if (!clock.timers.Contains(this)) clock.timers.Add(this); Due = dueTime == Timeout.InfiniteTimeSpan ? null : clock.now + dueTime; }
                return true;
            }
            public void Fire() => callback(state);
            public void Dispose() { lock (clock.timers) clock.timers.Remove(this); }
            public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        }
    }
}
