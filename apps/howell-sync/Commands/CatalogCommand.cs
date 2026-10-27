using skipper_api.Domain.Animals;

namespace HowellSync.Commands;

public sealed record CatalogCommand(string Name, string? Provider = null, bool DryRun = false, string? Query = null, int? Taxon = null, int? Limit = null)
{
    public CatalogSyncOperation Operation => Name switch
    {
        "import" => CatalogSyncOperation.Import,
        "sync" => CatalogSyncOperation.Synchronization,
        _ => CatalogSyncOperation.Validation
    };

    public static CatalogCommand Parse(string[] args)
    {
        if (args.Length == 0 || args is ["help"] or ["--help"]) return new("help");
        if (args[0] is not ("import" or "sync" or "validate"))
            throw new ArgumentException("Unknown command. Use help for usage.");
        string? provider = null, query = null;
        int? taxon = null, limit = null;
        bool dryRun = false, help = false;
        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--help" when !help: help = true; break;
                case "--dry-run" when !dryRun: dryRun = true; break;
                case "--query" when query is null:
                    if (++i == args.Length || string.IsNullOrWhiteSpace(args[i]) || args[i].StartsWith('-') || args[i].Length > 300)
                        throw new ArgumentException("--query requires text up to 300 characters.");
                    query = args[i]; break;
                case "--taxon" when taxon is null:
                    if (++i == args.Length || !int.TryParse(args[i], out var key) || key <= 0)
                        throw new ArgumentException("--taxon requires a positive GBIF usage key.");
                    taxon = key; break;
                case "--limit" when limit is null:
                    if (++i == args.Length || !int.TryParse(args[i], out var count) || count is < 1 or > 1000)
                        throw new ArgumentException("--limit must be between 1 and 1000.");
                    limit = count; break;
                case "--provider" when provider is null:
                    if (++i == args.Length || args[i].StartsWith('-') || string.IsNullOrWhiteSpace(args[i]))
                        throw new ArgumentException("--provider requires a provider name.");
                    provider = args[i].ToLowerInvariant();
                    break;
                default: throw new ArgumentException("Unknown or repeated option. Use help for usage.");
            }
        }
        if (help) return new("help");
        if (args[0] != "validate" && provider is null)
            throw new ArgumentException("import and sync require --provider <name>.");
        if (args[0] == "validate" && provider is not null)
            throw new ArgumentException("validate checks Professor; it does not accept --provider.");
        if ((query is not null && taxon is not null) || (args[0] == "validate" && (query is not null || taxon is not null || limit is not null)))
            throw new ArgumentException("Specify exactly one scope for imports; validate accepts no scope.");
        if (provider == "gbif" && (query is null) == (taxon is null))
            throw new ArgumentException("GBIF requires --query or --taxon.");
        return new(args[0], provider, dryRun, query, taxon, limit);
    }

    public const string Help = """
        Howell Sync — catalog operations 
        import --provider gbif (--query <text> | --taxon <usage-key>) [--limit 1..1000] [--dry-run]
        sync --provider gbif (--query <text> | --taxon <usage-key>) [--limit 1..1000] [--dry-run]
        validate [--dry-run]
        help | <command> --help
        Configure ConnectionStrings__ProfessorDb via environment or appsettings.json.
        Configure Providers__gbif__UserAgent with a real project URL/contact. Dry runs and validation write no history or catalog data.
        Exit codes: 0 success; 1 execution failure; 2 invalid arguments/configuration/provider;
                    3 catalog operation already running; 130 cancelled.
        """;
}
