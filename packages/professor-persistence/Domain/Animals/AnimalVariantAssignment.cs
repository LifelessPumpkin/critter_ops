namespace skipper_api.Domain.Animals;

/// <summary>An individual animal characteristic; custom values are permanently supported.</summary>
public class AnimalVariantAssignment
{
    public int Id { get; set; }
    public int AnimalId { get; set; }
    public Animal Animal { get; set; } = null!;
    public int? AnimalVariantId { get; set; }
    public AnimalVariant? AnimalVariant { get; set; }
    public string? CustomValue { get; set; }
    public string? NormalizedCustomValue { get; private set; }
    public VariantType? VariantType { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
