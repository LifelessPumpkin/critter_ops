using skipper_api.Dtos.AnimalMovements;

namespace skipper_api.Services.AnimalMovements;

public enum UpdateAnimalMovementActivityResultStatus
{
    Updated,
    NotFound,
}

public class UpdateAnimalMovementActivityResult
{
    public UpdateAnimalMovementActivityResultStatus Status { get; private init; }

    public AnimalMovementActivityDto? Movement { get; private init; }

    public static UpdateAnimalMovementActivityResult Updated(AnimalMovementActivityDto movement)
    {
        return new UpdateAnimalMovementActivityResult
        {
            Status = UpdateAnimalMovementActivityResultStatus.Updated,
            Movement = movement,
        };
    }

    public static UpdateAnimalMovementActivityResult NotFound()
    {
        return new UpdateAnimalMovementActivityResult
        {
            Status = UpdateAnimalMovementActivityResultStatus.NotFound,
        };
    }
}
