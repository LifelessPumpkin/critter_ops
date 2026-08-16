namespace skipper_api.Domain.Activity;

/// <summary>
/// Structured details for an animal treatment or procedure activity.
/// </summary>
public class AnimalTreatmentActivity
{
    public long ActivityEventId { get; set; }

    public ActivityEvent ActivityEvent { get; set; } = null!;

    public string? TreatmentType { get; set; }

    public required string TreatmentName { get; set; }

    public AnimalTreatmentResult? Result { get; set; }
}
