namespace skipper_api.Domain.Animals;

public enum VariantType { Breed, Morph, Variety, Strain, Mutation, Subspecies, Other }

public class AnimalVariant
{
    public int Id { get; set; }
    public int SpeciesId { get; set; }
    public Species Species { get; set; } = null!;
    public required string Name { get; set; }
    public VariantType VariantType { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
