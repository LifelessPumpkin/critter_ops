using skipper_api.Dtos.AnimalMedications;

namespace skipper_api.Services.AnimalMedications;

public enum CreateAnimalMedicationActivityResultStatus
{
    Created,
    AnimalNotFound,
    EnclosureNotFound,
}

public class CreateAnimalMedicationActivityResult
{
    public CreateAnimalMedicationActivityResultStatus Status { get; private init; }

    public AnimalMedicationActivityDto? Medication { get; private init; }

    public static CreateAnimalMedicationActivityResult Created(AnimalMedicationActivityDto medication)
    {
        return new CreateAnimalMedicationActivityResult
        {
            Status = CreateAnimalMedicationActivityResultStatus.Created,
            Medication = medication,
        };
    }

    public static CreateAnimalMedicationActivityResult AnimalNotFound()
    {
        return new CreateAnimalMedicationActivityResult
        {
            Status = CreateAnimalMedicationActivityResultStatus.AnimalNotFound,
        };
    }

    public static CreateAnimalMedicationActivityResult EnclosureNotFound()
    {
        return new CreateAnimalMedicationActivityResult
        {
            Status = CreateAnimalMedicationActivityResultStatus.EnclosureNotFound,
        };
    }
}
