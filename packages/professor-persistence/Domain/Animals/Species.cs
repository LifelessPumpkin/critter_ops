namespace skipper_api.Domain.Animals;

public class Species
{
    public int Id { get; set; }
    public string? CommonName { get; set; }
    public required string ScientificName { get; set; }
    public int AnimalGroupId { get; set; }
    public AnimalGroup AnimalGroup { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public TaxonomicStatus TaxonomicStatus { get; set; } = TaxonomicStatus.Unresolved;
    public bool IsManuallyManaged { get; set; } = true;
    // Null for historical records whose creation/modification time is unknown.
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public ICollection<SpeciesCommonName> CommonNames { get; set; } = [];
    public ICollection<SpeciesAlias> Aliases { get; set; } = [];
    public ICollection<SpeciesExternalReference> ExternalReferences { get; set; } = [];
    public VariantType? PreferredVariantType { get; set; }
}
