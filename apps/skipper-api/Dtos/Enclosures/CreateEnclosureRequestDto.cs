using System.ComponentModel.DataAnnotations;
using skipper_api.Domain.Enclosures;

namespace skipper_api.Dtos.Enclosures;

/// <summary>
/// API request shape for creating an enclosure.
/// </summary>
public class CreateEnclosureRequestDto
{
    [Required]
    [MaxLength(150)]
    public string? Name { get; set; }

    [Required]
    public EnclosureType? Type { get; set; }

    [Required]
    [MaxLength(150)]
    public string? Location { get; set; }

    [MaxLength(100)]
    public string? SizeLabel { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Length { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Width { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Height { get; set; }

    [MaxLength(30)]
    public string? DimensionUnit { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Volume { get; set; }

    [MaxLength(30)]
    public string? VolumeUnit { get; set; }

    [MaxLength(100)]
    public string? Material { get; set; }

    [Range(0, int.MaxValue)]
    public int? MaxAnimalCapacity { get; set; }

    [Required]
    public EnclosureMobility? Mobility { get; set; }

    [Required]
    public EnclosureStatus? Status { get; set; }

    [MaxLength(50)]
    public string? SafetyRating { get; set; }

    public string? Notes { get; set; }
}
