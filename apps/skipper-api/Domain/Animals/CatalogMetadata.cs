namespace skipper_api.Domain.Animals;

public enum TaxonomicStatus { Unresolved, Accepted, Synonym, Deprecated, Doubtful }
public enum SpeciesAliasType { Alternate, Historical, Synonym, Deprecated }
public enum CatalogSyncOperation { Import, Synchronization, Validation, CatalogExpansion }
public enum CatalogSyncStatus { Pending, Running, Completed, CompletedWithIssues, Failed, Cancelled }
public enum CatalogIssueStatus { Open, Investigating, Resolved, Ignored }
public enum CatalogIssueSeverity { Information, Warning, Error }

public class SpeciesCommonName
{
    public int Id { get; set; }
    public int SpeciesId { get; set; }
    public Species Species { get; set; } = null!;
    public required string Name { get; set; }
    public string? LanguageCode { get; set; }
    public string? Source { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SpeciesAlias
{
    public int Id { get; set; }
    public int SpeciesId { get; set; }
    public Species Species { get; set; } = null!;
    public required string Name { get; set; }
    public SpeciesAliasType AliasType { get; set; }
    public string? Source { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SpeciesExternalReference
{
    public int Id { get; set; }
    public int SpeciesId { get; set; }
    public Species Species { get; set; } = null!;
    public required string Provider { get; set; }
    public string? DatasetIdentifier { get; set; }
    public required string ExternalIdentifier { get; set; }
    public string? ExternalScientificName { get; set; }
    public string? ExternalTaxonomicStatus { get; set; }
    public string? ExternalAcceptedIdentifier { get; set; }
    public DateTime? LastCheckedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CatalogSyncRun
{
    public int Id { get; set; }
    public required string Provider { get; set; }
    public string? DatasetIdentifier { get; set; }
    public CatalogSyncOperation OperationType { get; set; }
    public CatalogSyncStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int RecordsProcessed { get; set; }
    public int RecordsCreated { get; set; }
    public int RecordsUpdated { get; set; }
    public int RecordsSkipped { get; set; }
    public int RecordsFailed { get; set; }
    public string? ErrorMessage { get; set; }
    public ICollection<CatalogSyncIssue> Issues { get; set; } = [];
}

public class CatalogSyncIssue
{
    public int Id { get; set; }
    public int? CatalogSyncRunId { get; set; }
    public CatalogSyncRun? CatalogSyncRun { get; set; }
    public int? SpeciesId { get; set; }
    public Species? Species { get; set; }
    public int? SpeciesExternalReferenceId { get; set; }
    public SpeciesExternalReference? SpeciesExternalReference { get; set; }
    public required string Provider { get; set; }
    public string? DatasetIdentifier { get; set; }
    public string? ExternalIdentifier { get; set; }
    public required string IssueType { get; set; }
    public CatalogIssueSeverity Severity { get; set; } = CatalogIssueSeverity.Warning;
    public CatalogIssueStatus Status { get; set; }
    public required string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
