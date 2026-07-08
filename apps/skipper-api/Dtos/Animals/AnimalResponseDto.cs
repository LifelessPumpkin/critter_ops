using skipper_api.Domain.Animals;

namespace skipper_api.Dtos.Animals;

/// <summary>
/// API response shape for animal records.
/// </summary>
public class AnimalResponseDto
{
    public int Id { get; set; }

    public int EnclosureId { get; set; }

    public required string Name { get; set; }

    public required string Species { get; set; }

    public string? SubspeciesOrMorph { get; set; }

    public AnimalType AnimalType { get; set; }

    public AnimalStatus Status { get; set; }

    public AnimalSex Sex { get; set; }

    public DateOnly? BirthDate { get; set; }

    public bool BirthDateIsEstimated { get; set; }

    public DateOnly AcquiredDate { get; set; }

    public DateOnly? DispositionDate { get; set; }

    public string? DispositionReason { get; set; }

    public string? MicrochipNumber { get; set; }

    public string? TagIdentifier { get; set; }

    public string? Source { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
