using skipper_api.Dtos.AnimalMovements;

namespace skipper_api.Services.AnimalMovements;

public enum CreateAnimalMovementActivityResultStatus
{
    Created,
    AnimalNotFound,
    SourceEnclosureNotFound,
    DestinationEnclosureNotFound,
    SourceDoesNotMatchCurrentEnclosure,
    DestinationMatchesCurrentEnclosure,
}

public class CreateAnimalMovementActivityResult
{
    public CreateAnimalMovementActivityResultStatus Status { get; private init; }

    public AnimalMovementActivityDto? Movement { get; private init; }

    public static CreateAnimalMovementActivityResult Created(AnimalMovementActivityDto movement)
    {
        return new CreateAnimalMovementActivityResult
        {
            Status = CreateAnimalMovementActivityResultStatus.Created,
            Movement = movement,
        };
    }

    public static CreateAnimalMovementActivityResult AnimalNotFound()
    {
        return new CreateAnimalMovementActivityResult
        {
            Status = CreateAnimalMovementActivityResultStatus.AnimalNotFound,
        };
    }

    public static CreateAnimalMovementActivityResult SourceEnclosureNotFound()
    {
        return new CreateAnimalMovementActivityResult
        {
            Status = CreateAnimalMovementActivityResultStatus.SourceEnclosureNotFound,
        };
    }

    public static CreateAnimalMovementActivityResult DestinationEnclosureNotFound()
    {
        return new CreateAnimalMovementActivityResult
        {
            Status = CreateAnimalMovementActivityResultStatus.DestinationEnclosureNotFound,
        };
    }

    public static CreateAnimalMovementActivityResult SourceDoesNotMatchCurrentEnclosure()
    {
        return new CreateAnimalMovementActivityResult
        {
            Status = CreateAnimalMovementActivityResultStatus.SourceDoesNotMatchCurrentEnclosure,
        };
    }

    public static CreateAnimalMovementActivityResult DestinationMatchesCurrentEnclosure()
    {
        return new CreateAnimalMovementActivityResult
        {
            Status = CreateAnimalMovementActivityResultStatus.DestinationMatchesCurrentEnclosure,
        };
    }
}
