using skipper_api.Dtos.Tasks;

namespace skipper_api.Services.Tasks;

public interface ITaskService
{
    System.Threading.Tasks.Task<IReadOnlyList<TaskResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);

    System.Threading.Tasks.Task<IReadOnlyList<TaskResponseDto>> GetTodayAsync(CancellationToken cancellationToken = default);

    System.Threading.Tasks.Task<TaskResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    System.Threading.Tasks.Task<CreateTaskResult> CreateAsync(
        CreateTaskRequestDto request,
        CancellationToken cancellationToken = default);

    System.Threading.Tasks.Task<UpdateTaskResult> UpdateAsync(
        int id,
        UpdateTaskRequestDto request,
        CancellationToken cancellationToken = default);

    System.Threading.Tasks.Task<CompleteTaskResult> CompleteAsync(
        int id,
        CompleteTaskRequestDto request,
        CancellationToken cancellationToken = default);

    System.Threading.Tasks.Task<DeleteTaskResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
