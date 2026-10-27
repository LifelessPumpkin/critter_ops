namespace HowellSync.Providers.Gbif;

public sealed record GbifTaxon
{
    public int? Key { get; init; }
    public string? DatasetKey { get; init; }
    public string? ScientificName { get; init; }
    public string? CanonicalName { get; init; }
    public string? Rank { get; init; }
    public string? TaxonomicStatus { get; init; }
    public int? AcceptedKey { get; init; }
    public int? ParentKey { get; init; }
    public string? Kingdom { get; init; }
    public string? Phylum { get; init; }
    public string? Class { get; init; }
    public string? Deleted { get; init; }
}

public sealed record GbifVernacularName(string? VernacularName, string? Language, string? Source);
public sealed record GbifPage<T>(int? Offset, int? Limit, bool? EndOfRecords, List<T>? Results);
public sealed class GbifProviderException(string reason) : Exception(reason)
{
    public string Reason { get; } = reason;
}
