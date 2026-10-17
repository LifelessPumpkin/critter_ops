using skipper_api.Dtos.Animals;

namespace skipper_api.Services.Animals;

public enum CreateAnimalResultStatus
{
    Created,
    EnclosureNotFound,
    InvalidReference,
}

public class CreateAnimalResult
{
    public string? Error { get; private init; }
    public static CreateAnimalResult InvalidReference(string error) => new() { Status = CreateAnimalResultStatus.InvalidReference, Error = error };

    public CreateAnimalResultStatus Status { get; private init; }

    public AnimalResponseDto? Animal { get; private init; }

    public static CreateAnimalResult Created(AnimalResponseDto animal)
    {
        return new CreateAnimalResult
        {
            Status = CreateAnimalResultStatus.Created,
            Animal = animal,
        };
    }

    public static CreateAnimalResult EnclosureNotFound()
    {
        return new CreateAnimalResult
        {
            Status = CreateAnimalResultStatus.EnclosureNotFound,
        };
    }
}
