using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using skipper_api.Domain.Animals;

namespace skipper_api.Dtos.Animals;

/// <summary>
/// API request shape for updating an animal.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class UpdateAnimalRequestDto
{
    [Required]
    public int? EnclosureId { get; set; }

    [Required]
    [MaxLength(100)]
    public string? Name { get; set; }

    [Required]
    [MaxLength(100)]
    public string? Species { get; set; }

    [MaxLength(100)]
    public string? SubspeciesOrMorph { get; set; }

    [Required]
    public AnimalType? AnimalType { get; set; }

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
}
