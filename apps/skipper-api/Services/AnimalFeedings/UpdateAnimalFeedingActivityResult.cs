using skipper_api.Dtos.AnimalFeedings;

namespace skipper_api.Services.AnimalFeedings;

public enum UpdateAnimalFeedingActivityResultStatus
{
    Updated,
    NotFound,
}

public class UpdateAnimalFeedingActivityResult
{
    public UpdateAnimalFeedingActivityResultStatus Status { get; private init; }

    public AnimalFeedingActivityDto? Feeding { get; private init; }

    public static UpdateAnimalFeedingActivityResult Updated(AnimalFeedingActivityDto feeding)
    {
        return new UpdateAnimalFeedingActivityResult
        {
            Status = UpdateAnimalFeedingActivityResultStatus.Updated,
            Feeding = feeding,
        };
    }

    public static UpdateAnimalFeedingActivityResult NotFound()
    {
        return new UpdateAnimalFeedingActivityResult
        {
            Status = UpdateAnimalFeedingActivityResultStatus.NotFound,
        };
    }
}
