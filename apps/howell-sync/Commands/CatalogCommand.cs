using skipper_api.Domain.Animals;

namespace HowellSync.Commands;

public sealed record CatalogCommand(string Name, string? Provider = null, bool DryRun = false)
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
        string? provider = null;
        bool dryRun = false, help = false;
        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--help" when !help: help = true; break;
                case "--dry-run" when !dryRun: dryRun = true; break;
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
        return new(args[0], provider, dryRun);
    }

    public const string Help = """
        Howell Sync — catalog operations (no external providers registered yet)
        import --provider <name> [--dry-run]
        sync --provider <name> [--dry-run]
        validate [--dry-run]
        help | <command> --help
        Configure ConnectionStrings__ProfessorDb via environment or appsettings.json.
        GBIF is not implemented. Dry runs and validation write no history or catalog data.
        Exit codes: 0 success; 1 execution failure; 2 invalid arguments/configuration/provider;
                    3 catalog operation already running; 130 cancelled.
        """;
}
