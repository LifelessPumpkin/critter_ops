using HowellSync.Commands;
using HowellSync.Execution;
using HowellSync.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using skipper_api.Domain.Animals;
using Xunit;

namespace MaryAnn.Howell;

public class HowellCommandTests
{
    [Theory]
    [InlineData("help")]
    [InlineData("--help")]
    [InlineData("import --help")]
    public void Help(string input) => Assert.Equal("help", CatalogCommand.Parse(input.Split(' ')).Name);

    [Theory]
    [InlineData("bogus")]
    [InlineData("help --bogus")]
    [InlineData("import")]
    [InlineData("import --provider")]
    [InlineData("import --provider --dry-run")]
    [InlineData("sync --provider test --oops")]
    [InlineData("validate --provider test")]
    [InlineData("validate --dry-run --dry-run")]
    public void InvalidArguments(string input) => Assert.Throws<ArgumentException>(() => CatalogCommand.Parse(input.Split(' ')));

    [Theory]
    [InlineData("import", CatalogSyncOperation.Import)]
    [InlineData("sync", CatalogSyncOperation.Synchronization)]
    public void Parse(string name, CatalogSyncOperation operation)
    {
        var command = CatalogCommand.Parse([name, "--provider", "TEST", "--dry-run"]);
        Assert.Equal(operation, command.Operation); Assert.True(command.DryRun); Assert.Equal("test", command.Provider);
    }

    [Fact]
    public async Task UnsupportedAndMissingConfigurationReturnTwo()
    {
        var runner = new CatalogOperationRunner(new ProviderRegistry([]), new ConfigurationBuilder().Build(), NullLogger<CatalogOperationRunner>.Instance, new HowellSync.Persistence.CatalogSessionFactory(), new HowellSync.Persistence.CatalogRunRepository());
        Assert.Equal(2, await runner.RunAsync(new("import", "gbif"), default));
        Assert.Equal(2, await runner.RunAsync(new("validate"), default));
        Assert.Equal(0, await runner.RunAsync(new("help"), default));
    }

    [Fact]
    public void ProviderRegistrationAndResolution()
    {
        var services = new ServiceCollection().AddSingleton<ICatalogProvider, ConfigProvider>().AddSingleton<ProviderRegistry>().BuildServiceProvider();
        var registry = services.GetRequiredService<ProviderRegistry>();
        Assert.IsType<ConfigProvider>(registry.Resolve("TEST", CatalogSyncOperation.Import));
        Assert.Throws<ArgumentException>(() => registry.Resolve("missing", CatalogSyncOperation.Import));
        Assert.Throws<ArgumentException>(() => registry.Resolve("test", CatalogSyncOperation.Synchronization));
    }

    [Fact]
    public async Task SelectedProviderConfigurationIsValidatedAndUnusedProvidersAreIgnored()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["ConnectionStrings:ProfessorDb"] = "Host=localhost;Database=test", ["Providers:unused:Budget"] = "bad"
        }).Build();
        var runner = new CatalogOperationRunner(new ProviderRegistry([new ConfigProvider()]), config, NullLogger<CatalogOperationRunner>.Instance, new HowellSync.Persistence.CatalogSessionFactory(), new HowellSync.Persistence.CatalogRunRepository());
        Assert.Equal(2, await runner.RunAsync(new("import", "test"), default));
        config["Providers:test:Budget"] = "2";
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Assert.Equal(130, await runner.RunAsync(new("import", "test"), cancelled.Token));
    }

    [Fact]
    public void EnvironmentOverridesProviderConfiguration()
    {
        const string key = "HOWELL_TEST_Providers__test__Budget";
        Environment.SetEnvironmentVariable(key, "3");
        try
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Providers:test:Budget"] = "1" })
                .AddEnvironmentVariables("HOWELL_TEST_").Build();
            Assert.Equal("3", config["Providers:test:Budget"]);
            new ConfigProvider().ValidateConfiguration(config.GetSection("Providers:test"));
        }
        finally { Environment.SetEnvironmentVariable(key, null); }
    }

    public sealed class ConfigProvider : ICatalogProvider
    {
        public string Name => "test";
        public IReadOnlySet<CatalogSyncOperation> SupportedOperations { get; } = new HashSet<CatalogSyncOperation> { CatalogSyncOperation.Import };
        public void ValidateConfiguration(IConfigurationSection configuration)
        {
            if (!int.TryParse(configuration["Budget"], out int budget) || budget < 1) throw new ArgumentException("Invalid budget");
        }
        public Task ExecuteAsync(CatalogSyncOperation operation, CatalogExecutionContext context, CancellationToken token) => Task.CompletedTask;
    }
}
