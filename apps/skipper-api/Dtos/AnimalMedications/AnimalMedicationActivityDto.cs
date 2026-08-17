using skipper_api.Domain.Activity;

namespace skipper_api.Dtos.AnimalMedications;

/// <summary>
/// API response shape for an animal medication activity.
/// </summary>
public class AnimalMedicationActivityDto
{
    public long ActivityEventId { get; set; }
    public int AnimalId { get; set; }
    public required string AnimalName { get; set; }
    public int EnclosureId { get; set; }
    public required string EnclosureName { get; set; }
    public required string MedicationName { get; set; }
    public decimal Dose { get; set; }
    public required string DoseUnit { get; set; }
    public AnimalMedicationRoute Route { get; set; }
    public AnimalMedicationResult? Result { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? Notes { get; set; }
    public string? PerformedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
