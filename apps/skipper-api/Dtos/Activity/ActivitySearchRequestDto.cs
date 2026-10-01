using System.ComponentModel.DataAnnotations;
using skipper_api.Domain.Activity;

namespace skipper_api.Dtos.Activity;

/// <summary>
/// Query parameters for searching the shared activity ledger.
/// </summary>
public class ActivitySearchRequestDto
{
    /// <summary>
    /// Free-text search across activity titles, notes, performers, animals, and enclosures.
    /// </summary>
    [MaxLength(200)]
    public string? Search { get; set; }

    /// <summary>
    /// Legacy single-value event type filter.
    /// </summary>
    public ActivityEventType? EventType { get; set; }

    /// <summary>
    /// Event types to include. When supplied, this takes precedence over EventType.
    /// </summary>
    public List<ActivityEventType> EventTypes { get; set; } = [];

    [Range(1, int.MaxValue)]
    public int? AnimalId { get; set; }

    [Range(1, int.MaxValue)]
    public int? EnclosureId { get; set; }

    [MaxLength(100)]
    public string? PerformedBy { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    public ActivitySortDirection Sort { get; set; } = ActivitySortDirection.Newest;

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 50;
}
