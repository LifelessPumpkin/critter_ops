using skipper_api.Dtos.AnimalDispositions;

namespace skipper_api.Services.AnimalDispositions;

public enum CreateAnimalDispositionActivityResultStatus
{
    Created,
    AnimalNotFound,
    EnclosureNotFound,
    OccurredBeforeAcquisition,
    AnimalAlreadyDisposed,
}

public class CreateAnimalDispositionActivityResult
{
    public CreateAnimalDispositionActivityResultStatus Status { get; private init; }

    public AnimalDispositionActivityDto? Disposition { get; private init; }

    public static CreateAnimalDispositionActivityResult Created(AnimalDispositionActivityDto disposition)
    {
        return new CreateAnimalDispositionActivityResult
        {
            Status = CreateAnimalDispositionActivityResultStatus.Created,
            Disposition = disposition,
        };
    }

    public static CreateAnimalDispositionActivityResult AnimalNotFound()
    {
        return new CreateAnimalDispositionActivityResult
        {
            Status = CreateAnimalDispositionActivityResultStatus.AnimalNotFound,
        };
    }

    public static CreateAnimalDispositionActivityResult EnclosureNotFound()
    {
        return new CreateAnimalDispositionActivityResult
        {
            Status = CreateAnimalDispositionActivityResultStatus.EnclosureNotFound,
        };
    }

    public static CreateAnimalDispositionActivityResult OccurredBeforeAcquisition()
    {
        return new CreateAnimalDispositionActivityResult
        {
            Status = CreateAnimalDispositionActivityResultStatus.OccurredBeforeAcquisition,
        };
    }

    public static CreateAnimalDispositionActivityResult AnimalAlreadyDisposed()
    {
        return new CreateAnimalDispositionActivityResult
        {
            Status = CreateAnimalDispositionActivityResultStatus.AnimalAlreadyDisposed,
        };
    }
}
