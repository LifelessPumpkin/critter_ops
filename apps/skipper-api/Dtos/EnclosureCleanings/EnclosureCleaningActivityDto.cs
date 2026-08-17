using skipper_api.Domain.Activity;

namespace skipper_api.Dtos.EnclosureCleanings;

/// <summary>
/// API response shape for an enclosure cleaning activity.
/// </summary>
public class EnclosureCleaningActivityDto
{
    public long ActivityEventId { get; set; }

    public int EnclosureId { get; set; }

    public required string EnclosureName { get; set; }

    public EnclosureCleaningType CleaningType { get; set; }

    public decimal? WaterChangePercent { get; set; }

    public bool SubstrateChanged { get; set; }

    public string? EquipmentCleaned { get; set; }

    public DateTime OccurredAt { get; set; }

    public string? Notes { get; set; }

    public string? PerformedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
