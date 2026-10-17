using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using skipper_api.Domain.Animals;

namespace skipper_api.Dtos.Animals;

/// <summary>
/// API request shape for updating an animal.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class UpdateAnimalRequestDto : IValidatableObject
{
    [Required]
    public int? EnclosureId { get; set; }

    [Required]
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(100)]
    public string? Species { get; set; }

    [MaxLength(100)]
    public string? SubspeciesOrMorph { get; set; }

    public AnimalType? AnimalType { get; set; }

    private int? _AnimalGroupId;
    [JsonIgnore] public bool AnimalGroupIdSpecified { get; private set; }
    [Range(1, int.MaxValue)] public int? AnimalGroupId { get => _AnimalGroupId; set { _AnimalGroupId = value; AnimalGroupIdSpecified = true; } }
    private int? _SpeciesId;
    [JsonIgnore] public bool SpeciesIdSpecified { get; private set; }
    [Range(1, int.MaxValue)] public int? SpeciesId { get => _SpeciesId; set { _SpeciesId = value; SpeciesIdSpecified = true; } }
    private int? _AnimalVariantId;
    [JsonIgnore] public bool AnimalVariantIdSpecified { get; private set; }
    [Range(1, int.MaxValue)] public int? AnimalVariantId { get => _AnimalVariantId; set { _AnimalVariantId = value; AnimalVariantIdSpecified = true; } }

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
        if (!SpeciesId.HasValue && string.IsNullOrWhiteSpace(Species))
            yield return new ValidationResult("Select a species or provide a custom species.", [nameof(Species)]);
        if (SubspeciesOrMorph != null && string.IsNullOrWhiteSpace(SubspeciesOrMorph))
            yield return new ValidationResult("Provide a nonblank custom variant or omit it.", [nameof(SubspeciesOrMorph)]);
    }
}
