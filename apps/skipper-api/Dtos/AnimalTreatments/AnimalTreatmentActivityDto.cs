using skipper_api.Domain.Activity;

namespace skipper_api.Dtos.AnimalTreatments;

/// <summary>
/// API response shape for an animal treatment activity.
/// </summary>
public class AnimalTreatmentActivityDto
{
    public long ActivityEventId { get; set; }
    public int AnimalId { get; set; }
    public required string AnimalName { get; set; }
    public int EnclosureId { get; set; }
    public required string EnclosureName { get; set; }
    public string? TreatmentType { get; set; }
    public required string TreatmentName { get; set; }
    public AnimalTreatmentResult? Result { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? Notes { get; set; }
    public string? PerformedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
