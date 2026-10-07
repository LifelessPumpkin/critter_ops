using skipper_api.Dtos.Tasks;

namespace skipper_api.Services.Tasks;

public enum CreateTaskResultStatus
{
    Created,
    AnimalNotFound,
    EnclosureNotFound,
}

public sealed class CreateTaskResult
{
    private CreateTaskResult(CreateTaskResultStatus status, TaskResponseDto? task = null)
    {
        Status = status;
        Task = task;
    }

    public CreateTaskResultStatus Status { get; }

    public TaskResponseDto? Task { get; }

    public static CreateTaskResult Created(TaskResponseDto task) => new(CreateTaskResultStatus.Created, task);

    public static CreateTaskResult AnimalNotFound() => new(CreateTaskResultStatus.AnimalNotFound);

    public static CreateTaskResult EnclosureNotFound() => new(CreateTaskResultStatus.EnclosureNotFound);
}
