using Microsoft.AspNetCore.Mvc;
using skipper_api.Dtos.Tasks;
using skipper_api.Services.Tasks;

namespace skipper_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>
    /// Returns all husbandry tasks sorted by due date.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<TaskResponseDto>))]
    public async Task<ActionResult<IReadOnlyList<TaskResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var tasks = await _taskService.GetAllAsync(cancellationToken);
        return Ok(tasks);
    }

    /// <summary>
    /// Returns incomplete tasks due today and overdue tasks, sorted by urgency and due date.
    /// </summary>
    [HttpGet("today")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<TaskResponseDto>))]
    public async Task<ActionResult<IReadOnlyList<TaskResponseDto>>> GetToday(CancellationToken cancellationToken)
    {
        var tasks = await _taskService.GetTodayAsync(cancellationToken);
        return Ok(tasks);
    }

    /// <summary>
    /// Returns a single husbandry task by ID.
    /// </summary>
    [HttpGet("{id:int}", Name = nameof(GetTaskById))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TaskResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponseDto>> GetTaskById(
        int id,
        CancellationToken cancellationToken)
    {
        var task = await _taskService.GetByIdAsync(id, cancellationToken);

        return task is null
            ? NotFound()
            : Ok(task);
    }

    /// <summary>
    /// Creates a new husbandry task.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TaskResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskResponseDto>> Create(
        CreateTaskRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.CreateAsync(request, cancellationToken);

        return result.Status switch
        {
            CreateTaskResultStatus.Created => CreatedAtRoute(
                nameof(GetTaskById),
                new { id = result.Task!.Id },
                result.Task),
            CreateTaskResultStatus.AnimalNotFound => BadRequest(new
            {
                message = "The provided animal does not exist.",
            }),
            CreateTaskResultStatus.EnclosureNotFound => BadRequest(new
            {
                message = "The provided enclosure does not exist.",
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>
    /// Updates an existing husbandry task.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TaskResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponseDto>> Update(
        int id,
        UpdateTaskRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.UpdateAsync(id, request, cancellationToken);

        return result.Status switch
        {
            UpdateTaskResultStatus.Updated => Ok(result.Task),
            UpdateTaskResultStatus.TaskNotFound => NotFound(),
            UpdateTaskResultStatus.AnimalNotFound => BadRequest(new
            {
                message = "The provided animal does not exist.",
            }),
            UpdateTaskResultStatus.EnclosureNotFound => BadRequest(new
            {
                message = "The provided enclosure does not exist.",
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>
    /// Completes a task occurrence, records activity history, and creates the next recurring occurrence when needed.
    /// </summary>
    [HttpPost("{id:int}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TaskResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TaskResponseDto>> Complete(
        int id,
        CompleteTaskRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.CompleteAsync(id, request, cancellationToken);

        return result.Status switch
        {
            CompleteTaskResultStatus.Completed => Ok(result.Task),
            CompleteTaskResultStatus.NotFound => NotFound(),
            CompleteTaskResultStatus.AlreadyCompleted => Conflict(new
            {
                message = "The task has already been completed.",
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>
    /// Deletes a husbandry task.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _taskService.DeleteAsync(id, cancellationToken);

        return result switch
        {
            DeleteTaskResult.Deleted => NoContent(),
            DeleteTaskResult.NotFound => NotFound(),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}
