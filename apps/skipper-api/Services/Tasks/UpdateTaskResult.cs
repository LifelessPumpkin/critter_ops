using skipper_api.Dtos.Tasks;

namespace skipper_api.Services.Tasks;

public enum UpdateTaskResultStatus
{
    Updated,
    TaskNotFound,
    AnimalNotFound,
    EnclosureNotFound,
}

public sealed class UpdateTaskResult
{
    private UpdateTaskResult(UpdateTaskResultStatus status, TaskResponseDto? task = null)
    {
        Status = status;
        Task = task;
    }

    public UpdateTaskResultStatus Status { get; }

    public TaskResponseDto? Task { get; }

    public static UpdateTaskResult Updated(TaskResponseDto task) => new(UpdateTaskResultStatus.Updated, task);

    public static UpdateTaskResult TaskNotFound() => new(UpdateTaskResultStatus.TaskNotFound);

    public static UpdateTaskResult AnimalNotFound() => new(UpdateTaskResultStatus.AnimalNotFound);

    public static UpdateTaskResult EnclosureNotFound() => new(UpdateTaskResultStatus.EnclosureNotFound);
}
