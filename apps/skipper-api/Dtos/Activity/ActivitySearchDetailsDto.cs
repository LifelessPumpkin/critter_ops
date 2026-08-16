namespace skipper_api.Dtos.Activity;

/// <summary>
/// Structured activity detail payloads for supported activity types.
/// </summary>
public class ActivitySearchDetailsDto
{
    public ActivitySearchMovementDetailsDto? Movement { get; set; }

    public ActivitySearchFeedingDetailsDto? Feeding { get; set; }

    public ActivitySearchDispositionDetailsDto? Disposition { get; set; }

    public ActivitySearchMedicationDetailsDto? Medication { get; set; }

    public ActivitySearchTreatmentDetailsDto? Treatment { get; set; }

    public ActivitySearchCleaningDetailsDto? Cleaning { get; set; }
}
