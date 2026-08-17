using System.Text.Json;
using skipper_api.Domain.Activity;

namespace skipper_api.Dtos.Activity;

/// <summary>
/// Activity ledger search result.
/// </summary>
public class ActivitySearchResultDto
{
    public long Id { get; set; }

    public ActivityEventType EventType { get; set; }

    public DateTime OccurredAt { get; set; }

    public required string Title { get; set; }

    public string? PerformedBy { get; set; }

    public string? Notes { get; set; }

    public Guid? SourceReferenceId { get; set; }

    public string? SourceType { get; set; }

    public JsonElement? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public IReadOnlyList<ActivitySearchAnimalDto> Animals { get; set; } = [];

    public IReadOnlyList<ActivitySearchEnclosureDto> Enclosures { get; set; } = [];

    public ActivitySearchDetailsDto Details { get; set; } = new();
}
