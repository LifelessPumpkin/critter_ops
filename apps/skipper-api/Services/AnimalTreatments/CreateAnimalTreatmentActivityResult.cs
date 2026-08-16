using skipper_api.Dtos.AnimalTreatments;

namespace skipper_api.Services.AnimalTreatments;

public enum CreateAnimalTreatmentActivityResultStatus
{
    Created,
    AnimalNotFound,
    EnclosureNotFound,
}

public class CreateAnimalTreatmentActivityResult
{
    public CreateAnimalTreatmentActivityResultStatus Status { get; private init; }

    public AnimalTreatmentActivityDto? Treatment { get; private init; }

    public static CreateAnimalTreatmentActivityResult Created(AnimalTreatmentActivityDto treatment)
    {
        return new CreateAnimalTreatmentActivityResult
        {
            Status = CreateAnimalTreatmentActivityResultStatus.Created,
            Treatment = treatment,
        };
    }

    public static CreateAnimalTreatmentActivityResult AnimalNotFound()
    {
        return new CreateAnimalTreatmentActivityResult
        {
            Status = CreateAnimalTreatmentActivityResultStatus.AnimalNotFound,
        };
    }

    public static CreateAnimalTreatmentActivityResult EnclosureNotFound()
    {
        return new CreateAnimalTreatmentActivityResult
        {
            Status = CreateAnimalTreatmentActivityResultStatus.EnclosureNotFound,
        };
    }
}
