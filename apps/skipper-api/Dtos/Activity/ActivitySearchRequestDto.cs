using System.ComponentModel.DataAnnotations;
using skipper_api.Domain.Activity;

namespace skipper_api.Dtos.Activity;

/// <summary>
/// Query parameters for searching the shared activity ledger.
/// </summary>
public class ActivitySearchRequestDto
{
    public ActivityEventType? EventType { get; set; }

    [Range(1, int.MaxValue)]
    public int? AnimalId { get; set; }

    [Range(1, int.MaxValue)]
    public int? EnclosureId { get; set; }

    [MaxLength(100)]
    public string? PerformedBy { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 50;
}
