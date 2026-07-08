using skipper_api.Dtos.Animals;

namespace skipper_api.Services.Animals;

public enum UpdateAnimalResultStatus
{
    Updated,
    AnimalNotFound,
    EnclosureNotFound,
}

public class UpdateAnimalResult
{
    public UpdateAnimalResultStatus Status { get; private init; }

    public AnimalResponseDto? Animal { get; private init; }

    public static UpdateAnimalResult Updated(AnimalResponseDto animal)
    {
        return new UpdateAnimalResult
        {
            Status = UpdateAnimalResultStatus.Updated,
            Animal = animal,
        };
    }

    public static UpdateAnimalResult AnimalNotFound()
    {
        return new UpdateAnimalResult
        {
            Status = UpdateAnimalResultStatus.AnimalNotFound,
        };
    }

    public static UpdateAnimalResult EnclosureNotFound()
    {
        return new UpdateAnimalResult
        {
            Status = UpdateAnimalResultStatus.EnclosureNotFound,
        };
    }
}
