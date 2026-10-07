using skipper_api.Domain.Activity;
using skipper_api.Domain.Animals;

namespace skipper_api.Dtos.AnimalDispositions;

/// <summary>
/// API response shape for an animal disposition activity.
/// </summary>
public class AnimalDispositionActivityDto
{
    public long ActivityEventId { get; set; }

    public int AnimalId { get; set; }

    public required string AnimalName { get; set; }

    public int EnclosureId { get; set; }

    public required string EnclosureName { get; set; }

    public AnimalDispositionType DispositionType { get; set; }

    public AnimalStatus AnimalStatus { get; set; }

    public DateTime OccurredAt { get; set; }

    public string? Reason { get; set; }

    public string? RecipientOrDestination { get; set; }

    public string? Notes { get; set; }

    public string? PerformedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
