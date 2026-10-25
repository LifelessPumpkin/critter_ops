using HowellSync.Commands;
using HowellSync.Execution;
using HowellSync.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using skipper_api.Domain.Animals;

namespace MaryAnn.Howell;

// Test executable only. Never registered or shipped with Howell Sync.
internal static class HowellProcessHarness
{
    public static async Task<int> Main(string[] args)
    {
        if (args is not ["--howell-test-child", var signal]) return 2;
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables("HOWELL_TEST_").Build();
        var runner = new CatalogOperationRunner(new ProviderRegistry([new BlockingProvider(signal)]), configuration, NullLogger<CatalogOperationRunner>.Instance, new HowellSync.Persistence.CatalogSessionFactory(), new HowellSync.Persistence.CatalogRunRepository());
        return await runner.RunAsync(new CatalogCommand("import", "test"), default);
    }

    private sealed class BlockingProvider(string signal) : ICatalogProvider
    {
        public string Name => "test";
        public IReadOnlySet<CatalogSyncOperation> SupportedOperations { get; } = new HashSet<CatalogSyncOperation> { CatalogSyncOperation.Import };
        public void ValidateConfiguration(IConfigurationSection configuration) { }
        public async Task ExecuteAsync(CatalogSyncOperation operation, CatalogExecutionContext context, CancellationToken token)
        {
            await context.BatchAsync(async (db, ct) => {
                db.Species.Add(new Species { ScientificName = "Crash rollback specimen", AnimalGroupId = 1 });
                await db.SaveChangesAsync(ct);
                await File.WriteAllTextAsync(signal, "ready", ct);
                await Task.Delay(Timeout.Infinite, ct);
                return new CatalogCounts(1, 1);
            }, token);
        }
    }
}
