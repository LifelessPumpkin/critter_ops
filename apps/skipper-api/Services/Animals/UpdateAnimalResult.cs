using skipper_api.Dtos.Animals;

namespace skipper_api.Services.Animals;

public enum UpdateAnimalResultStatus
{
    Updated,
    AnimalNotFound,
    EnclosureNotFound,
    InvalidReference,
}

public class UpdateAnimalResult
{
    public string? Error { get; private init; }
    public static UpdateAnimalResult InvalidReference(string error) => new() { Status = UpdateAnimalResultStatus.InvalidReference, Error = error };

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
