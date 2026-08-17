using skipper_api.Dtos.AnimalDispositions;

namespace skipper_api.Services.AnimalDispositions;

public enum UpdateAnimalDispositionActivityResultStatus
{
    Updated,
    NotFound,
    OccurredBeforeAcquisition,
}

public class UpdateAnimalDispositionActivityResult
{
    public UpdateAnimalDispositionActivityResultStatus Status { get; private init; }

    public AnimalDispositionActivityDto? Disposition { get; private init; }

    public static UpdateAnimalDispositionActivityResult Updated(AnimalDispositionActivityDto disposition)
    {
        return new UpdateAnimalDispositionActivityResult
        {
            Status = UpdateAnimalDispositionActivityResultStatus.Updated,
            Disposition = disposition,
        };
    }

    public static UpdateAnimalDispositionActivityResult NotFound()
    {
        return new UpdateAnimalDispositionActivityResult
        {
            Status = UpdateAnimalDispositionActivityResultStatus.NotFound,
        };
    }

    public static UpdateAnimalDispositionActivityResult OccurredBeforeAcquisition()
    {
        return new UpdateAnimalDispositionActivityResult
        {
            Status = UpdateAnimalDispositionActivityResultStatus.OccurredBeforeAcquisition,
        };
    }
}
