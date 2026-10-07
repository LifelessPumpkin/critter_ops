using skipper_api.Dtos.AnimalMedications;

namespace skipper_api.Services.AnimalMedications;

public enum UpdateAnimalMedicationActivityResultStatus
{
    Updated,
    NotFound,
}

public class UpdateAnimalMedicationActivityResult
{
    public UpdateAnimalMedicationActivityResultStatus Status { get; private init; }

    public AnimalMedicationActivityDto? Medication { get; private init; }

    public static UpdateAnimalMedicationActivityResult Updated(AnimalMedicationActivityDto medication)
    {
        return new UpdateAnimalMedicationActivityResult
        {
            Status = UpdateAnimalMedicationActivityResultStatus.Updated,
            Medication = medication,
        };
    }

    public static UpdateAnimalMedicationActivityResult NotFound()
    {
        return new UpdateAnimalMedicationActivityResult
        {
            Status = UpdateAnimalMedicationActivityResultStatus.NotFound,
        };
    }
}
