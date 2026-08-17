using skipper_api.Domain.Activity;

namespace skipper_api.Dtos.Activity;

public class ActivitySearchMovementDetailsDto
{
    public int FromEnclosureId { get; set; }

    public required string FromEnclosureName { get; set; }

    public int ToEnclosureId { get; set; }

    public required string ToEnclosureName { get; set; }

    public string? Reason { get; set; }
}

public class ActivitySearchFeedingDetailsDto
{
    public required string Food { get; set; }

    public decimal Quantity { get; set; }

    public AnimalFeedingQuantityUnit Unit { get; set; }

    public AnimalFeedingResult Result { get; set; }
}

public class ActivitySearchDispositionDetailsDto
{
    public AnimalDispositionType DispositionType { get; set; }

    public string? Reason { get; set; }

    public string? RecipientOrDestination { get; set; }
}

public class ActivitySearchMedicationDetailsDto
{
    public required string MedicationName { get; set; }

    public decimal Dose { get; set; }

    public required string DoseUnit { get; set; }

    public AnimalMedicationRoute Route { get; set; }

    public AnimalMedicationResult? Result { get; set; }
}

public class ActivitySearchTreatmentDetailsDto
{
    public string? TreatmentType { get; set; }

    public required string TreatmentName { get; set; }

    public AnimalTreatmentResult? Result { get; set; }
}

public class ActivitySearchCleaningDetailsDto
{
    public EnclosureCleaningType CleaningType { get; set; }

    public decimal? WaterChangePercent { get; set; }

    public bool SubstrateChanged { get; set; }

    public string? EquipmentCleaned { get; set; }
}
