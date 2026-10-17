namespace skipper_api.Domain.Animals;

public class Species
{
    public int Id { get; set; }
    public string? CommonName { get; set; }
    public required string ScientificName { get; set; }
    public int AnimalGroupId { get; set; }
    public AnimalGroup AnimalGroup { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public VariantType? PreferredVariantType { get; set; }
}
