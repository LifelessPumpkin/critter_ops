using skipper_api.Dtos.EnclosureCleanings;

namespace skipper_api.Services.EnclosureCleanings;

public enum UpdateEnclosureCleaningActivityResultStatus
{
    Updated,
    NotFound,
}

public class UpdateEnclosureCleaningActivityResult
{
    public UpdateEnclosureCleaningActivityResultStatus Status { get; private init; }

    public EnclosureCleaningActivityDto? Cleaning { get; private init; }

    public static UpdateEnclosureCleaningActivityResult Updated(EnclosureCleaningActivityDto cleaning)
    {
        return new UpdateEnclosureCleaningActivityResult
        {
            Status = UpdateEnclosureCleaningActivityResultStatus.Updated,
            Cleaning = cleaning,
        };
    }

    public static UpdateEnclosureCleaningActivityResult NotFound()
    {
        return new UpdateEnclosureCleaningActivityResult
        {
            Status = UpdateEnclosureCleaningActivityResultStatus.NotFound,
        };
    }
}
