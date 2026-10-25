using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using skipper_api.Domain.Animals;

namespace skipper_api.Dtos.Animals;

/// <summary>
/// API request shape for creating an animal.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class CreateAnimalRequestDto : IValidatableObject
{
    [Required]
    public int? EnclosureId { get; set; }

    [Required]
    [MaxLength(100)]
    public string? Name { get; set; }

    /// <summary>Permanent custom species; use instead of SpeciesId.</summary>
    [MaxLength(100)]
    public string? CustomSpeciesName { get; set; }

    /// <summary>Permanent custom variant; use instead of AnimalVariantId.</summary>
    [MaxLength(100)]
    public string? CustomVariantName { get; set; }

    public AnimalType? AnimalType { get; set; }

    [Range(1, int.MaxValue)] public int? AnimalGroupId { get; set; }
    [Range(1, int.MaxValue)] public int? SpeciesId { get; set; }
    [Range(1, int.MaxValue)] public int? AnimalVariantId { get; set; }

    [Required]
    public AnimalStatus? Status { get; set; }

    [Required]
    public AnimalSex? Sex { get; set; }

    public DateOnly? BirthDate { get; set; }

    public bool BirthDateIsEstimated { get; set; }

    [Required]
    public DateOnly? AcquiredDate { get; set; }

    public DateOnly? DispositionDate { get; set; }

    [MaxLength(100)]
    public string? DispositionReason { get; set; }

    [MaxLength(50)]
    public string? MicrochipNumber { get; set; }

    [MaxLength(50)]
    public string? TagIdentifier { get; set; }

    [MaxLength(100)]
    public string? Source { get; set; }

    public string? Notes { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!SpeciesId.HasValue && string.IsNullOrWhiteSpace(CustomSpeciesName))
            yield return new ValidationResult("Select a species or provide a custom species.", [nameof(CustomSpeciesName)]);
        if (SpeciesId.HasValue && !string.IsNullOrWhiteSpace(CustomSpeciesName))
            yield return new ValidationResult("Choose structured or custom species, not both.", [nameof(CustomSpeciesName)]);
        if (AnimalVariantId.HasValue && !string.IsNullOrWhiteSpace(CustomVariantName))
            yield return new ValidationResult("Choose structured or custom variant, not both.", [nameof(CustomVariantName)]);
        if (CustomVariantName != null && string.IsNullOrWhiteSpace(CustomVariantName))
            yield return new ValidationResult("Provide a nonblank custom variant or omit it.", [nameof(CustomVariantName)]);
    }
}
