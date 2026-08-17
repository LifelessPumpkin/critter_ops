namespace skipper_api.Domain.Activity;

/// <summary>
/// Structured details for an animal medication activity.
/// </summary>
public class AnimalMedicationActivity
{
    public long ActivityEventId { get; set; }

    public ActivityEvent ActivityEvent { get; set; } = null!;

    public required string MedicationName { get; set; }

    public decimal Dose { get; set; }

    public required string DoseUnit { get; set; }

    public required AnimalMedicationRoute Route { get; set; }

    public AnimalMedicationResult? Result { get; set; }
}
