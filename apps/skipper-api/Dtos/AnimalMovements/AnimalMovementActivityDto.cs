namespace skipper_api.Dtos.AnimalMovements;

/// <summary>
/// API response shape for an animal movement activity.
/// </summary>
public class AnimalMovementActivityDto
{
    public long ActivityEventId { get; set; }

    public int AnimalId { get; set; }

    public required string AnimalName { get; set; }

    public int FromEnclosureId { get; set; }

    public required string FromEnclosureName { get; set; }

    public int ToEnclosureId { get; set; }

    public required string ToEnclosureName { get; set; }

    public DateTime OccurredAt { get; set; }

    public string? Reason { get; set; }

    public string? Notes { get; set; }

    public string? PerformedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
