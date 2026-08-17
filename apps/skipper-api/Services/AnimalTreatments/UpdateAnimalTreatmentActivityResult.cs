using skipper_api.Dtos.AnimalTreatments;

namespace skipper_api.Services.AnimalTreatments;

public enum UpdateAnimalTreatmentActivityResultStatus
{
    Updated,
    NotFound,
}

public class UpdateAnimalTreatmentActivityResult
{
    public UpdateAnimalTreatmentActivityResultStatus Status { get; private init; }

    public AnimalTreatmentActivityDto? Treatment { get; private init; }

    public static UpdateAnimalTreatmentActivityResult Updated(AnimalTreatmentActivityDto treatment)
    {
        return new UpdateAnimalTreatmentActivityResult
        {
            Status = UpdateAnimalTreatmentActivityResultStatus.Updated,
            Treatment = treatment,
        };
    }

    public static UpdateAnimalTreatmentActivityResult NotFound()
    {
        return new UpdateAnimalTreatmentActivityResult
        {
            Status = UpdateAnimalTreatmentActivityResultStatus.NotFound,
        };
    }
}
