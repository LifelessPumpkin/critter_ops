using System.ComponentModel.DataAnnotations;
using skipper_api.Domain.Animals;

namespace skipper_api.Dtos.Animals;

/// <summary>Query parameters for the operational animals list.</summary>
public class AnimalSearchRequestDto
{
    [MaxLength(200)]
    public string? Search { get; set; }

    public AnimalLifecycle Lifecycle { get; set; } = AnimalLifecycle.InCare;

    public List<AnimalStatus> Statuses { get; set; } = [];

    public List<AnimalType> AnimalTypes { get; set; } = [];

    [Range(1, int.MaxValue)]
    public int? EnclosureId { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 40;
}
