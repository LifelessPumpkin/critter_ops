using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using skipper_api.Domain.Activity;

namespace skipper_api.Dtos.EnclosureCleanings;

/// <summary>
/// Request shape for updating an enclosure cleaning activity.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class UpdateEnclosureCleaningActivityDto
{
    [Required]
    public EnclosureCleaningType? CleaningType { get; set; }

    [Range(typeof(decimal), "0", "100")]
    public decimal? WaterChangePercent { get; set; }

    public bool SubstrateChanged { get; set; }

    [MaxLength(250)]
    public string? EquipmentCleaned { get; set; }

    [Required]
    public DateTime? OccurredAt { get; set; }

    public string? Notes { get; set; }

    [MaxLength(100)]
    public string? PerformedBy { get; set; }
}
