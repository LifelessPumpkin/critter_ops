using skipper_api.Domain.Enclosures;

namespace skipper_api.Dtos.Enclosures;

/// <summary>
/// API response shape for enclosure records.
/// </summary>
public class EnclosureResponseDto
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public EnclosureType Type { get; set; }

    public required string Location { get; set; }

    public string? SizeLabel { get; set; }

    public decimal? Length { get; set; }

    public decimal? Width { get; set; }

    public decimal? Height { get; set; }

    public string? DimensionUnit { get; set; }

    public decimal? Volume { get; set; }

    public string? VolumeUnit { get; set; }

    public string? Material { get; set; }

    public int? MaxAnimalCapacity { get; set; }

    public EnclosureMobility Mobility { get; set; }

    public EnclosureStatus Status { get; set; }

    public string? SafetyRating { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime UpdatedDate { get; set; }
}
