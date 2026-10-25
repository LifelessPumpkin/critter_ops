namespace skipper_api.Domain.Animals;

/// <summary>Operational grouping, not a biological taxonomic class.</summary>
public class AnimalGroup
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public AnimalType LegacyAnimalType { get; set; }
}
