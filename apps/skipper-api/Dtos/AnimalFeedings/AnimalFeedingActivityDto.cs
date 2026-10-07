using skipper_api.Domain.Activity;

namespace skipper_api.Dtos.AnimalFeedings;

/// <summary>
/// API response shape for an animal feeding activity.
/// </summary>
public class AnimalFeedingActivityDto
{
    public long ActivityEventId { get; set; }

    public int AnimalId { get; set; }

    public required string AnimalName { get; set; }

    public int EnclosureId { get; set; }

    public required string EnclosureName { get; set; }

    public required string Food { get; set; }

    public decimal Quantity { get; set; }

    public AnimalFeedingQuantityUnit Unit { get; set; }

    public AnimalFeedingResult Result { get; set; }

    public DateTime OccurredAt { get; set; }

    public string? Notes { get; set; }

    public string? PerformedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
