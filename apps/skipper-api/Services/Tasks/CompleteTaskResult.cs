using skipper_api.Dtos.Tasks;

namespace skipper_api.Services.Tasks;

public enum CompleteTaskResultStatus
{
    Completed,
    NotFound,
    AlreadyCompleted,
}

public sealed class CompleteTaskResult
{
    private CompleteTaskResult(CompleteTaskResultStatus status, TaskResponseDto? task = null)
    {
        Status = status;
        Task = task;
    }

    public CompleteTaskResultStatus Status { get; }

    public TaskResponseDto? Task { get; }

    public static CompleteTaskResult Completed(TaskResponseDto task) => new(CompleteTaskResultStatus.Completed, task);

    public static CompleteTaskResult NotFound() => new(CompleteTaskResultStatus.NotFound);

    public static CompleteTaskResult AlreadyCompleted() => new(CompleteTaskResultStatus.AlreadyCompleted);
}
