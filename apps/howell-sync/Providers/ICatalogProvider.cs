using HowellSync.Execution;
using Microsoft.Extensions.Configuration;
using skipper_api.Domain.Animals;

namespace HowellSync.Providers;

public interface ICatalogProvider
{
    string Name { get; }
    IReadOnlySet<CatalogSyncOperation> SupportedOperations { get; }
    // Validate only this provider's section. Never include configuration values in errors.
    void ValidateConfiguration(IConfigurationSection configuration);
    Task ExecuteAsync(CatalogSyncOperation operation, CatalogExecutionContext context, CancellationToken cancellationToken);
}

public sealed class ProviderRegistry
{
    private readonly Dictionary<string, ICatalogProvider> providers;

    public ProviderRegistry(IEnumerable<ICatalogProvider> providers)
    {
        this.providers = providers.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
    }

    public ICatalogProvider Resolve(string name, CatalogSyncOperation operation)
    {
        if (!providers.TryGetValue(name, out var provider))
            throw new ArgumentException("Unsupported provider. GBIF and other external integrations are not implemented yet.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(provider.Name, "^[a-z0-9][a-z0-9._-]{0,99}$"))
            throw new ArgumentException("Registered provider must have a canonical provider slug.");
        if (!provider.SupportedOperations.Contains(operation))
            throw new ArgumentException("The provider does not support this operation.");
        return provider;
    }
}
