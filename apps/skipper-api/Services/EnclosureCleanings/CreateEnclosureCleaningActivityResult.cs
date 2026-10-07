using skipper_api.Dtos.EnclosureCleanings;

namespace skipper_api.Services.EnclosureCleanings;

public enum CreateEnclosureCleaningActivityResultStatus
{
    Created,
    EnclosureNotFound,
}

public class CreateEnclosureCleaningActivityResult
{
    public CreateEnclosureCleaningActivityResultStatus Status { get; private init; }

    public EnclosureCleaningActivityDto? Cleaning { get; private init; }

    public static CreateEnclosureCleaningActivityResult Created(EnclosureCleaningActivityDto cleaning)
    {
        return new CreateEnclosureCleaningActivityResult
        {
            Status = CreateEnclosureCleaningActivityResultStatus.Created,
            Cleaning = cleaning,
        };
    }

    public static CreateEnclosureCleaningActivityResult EnclosureNotFound()
    {
        return new CreateEnclosureCleaningActivityResult
        {
            Status = CreateEnclosureCleaningActivityResultStatus.EnclosureNotFound,
        };
    }
}
