using skipper_api.Domain.Animals;

namespace skipper_api.Dtos.Animals;

/// <summary>Compact projection for the operational animals table.</summary>
public class AnimalListItemDto
{
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
