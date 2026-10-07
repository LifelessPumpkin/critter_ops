using skipper_api.Dtos.AnimalFeedings;

namespace skipper_api.Services.AnimalFeedings;

public enum CreateAnimalFeedingActivityResultStatus
{
    Created,
    AnimalNotFound,
    EnclosureNotFound,
}

public class CreateAnimalFeedingActivityResult
{
    public CreateAnimalFeedingActivityResultStatus Status { get; private init; }

    public AnimalFeedingActivityDto? Feeding { get; private init; }

    public static CreateAnimalFeedingActivityResult Created(AnimalFeedingActivityDto feeding)
    {
        return new CreateAnimalFeedingActivityResult
        {
            Status = CreateAnimalFeedingActivityResultStatus.Created,
            Feeding = feeding,
        };
    }

    public static CreateAnimalFeedingActivityResult AnimalNotFound()
    {
        return new CreateAnimalFeedingActivityResult
        {
            Status = CreateAnimalFeedingActivityResultStatus.AnimalNotFound,
        };
    }

    public static CreateAnimalFeedingActivityResult EnclosureNotFound()
    {
        return new CreateAnimalFeedingActivityResult
        {
            Status = CreateAnimalFeedingActivityResultStatus.EnclosureNotFound,
        };
    }
}
