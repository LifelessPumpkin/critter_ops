using skipper_api.Domain.Animals;

namespace skipper_api.Dtos.Animals;

/// <summary>Compact projection for the operational animals table.</summary>
public class AnimalListItemDto
{
    public string? CustomSpeciesName { get; set; }
    public string? CustomVariantName { get; set; }
    // Original text is exposed separately so structured identity never hides historical information.
    public string? OriginalSpeciesText { get; set; }
    public string? OriginalVariantText { get; set; }
    public int? AnimalGroupId { get; set; }
    public string? AnimalGroupName { get; set; }
    public int? SpeciesId { get; set; }
    public string? SpeciesName => Species;
    public string? VariantName => SubspeciesOrMorph;
    public string? SpeciesScientificName { get; set; }
    public int? AnimalVariantId { get; set; }
    public VariantType? VariantType { get; set; }
    public VariantType? PreferredVariantType { get; set; }
    public int Id { get; set; }
    public int EnclosureId { get; set; }
    public required string EnclosureName { get; set; }
    public required string EnclosureLocation { get; set; }
    public required string Name { get; set; }
    public required string Species { get; set; }
    public string? SubspeciesOrMorph { get; set; }
    public AnimalType AnimalType { get; set; }
    public AnimalStatus Status { get; set; }
    public AnimalSex Sex { get; set; }
    public DateOnly? BirthDate { get; set; }
    public bool BirthDateIsEstimated { get; set; }
}
