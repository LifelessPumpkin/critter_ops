using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using skipper_api.Data;
using skipper_api.Domain.Activity;
using skipper_api.Domain.Tasks;
using skipper_api.Dtos.Tasks;
using TaskEntity = skipper_api.Domain.Tasks.Task;

namespace skipper_api.Services.Tasks;

public class TaskService : ITaskService
{
    private readonly ProfessorDbContext _dbContext;

    public TaskService(ProfessorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async System.Threading.Tasks.Task<IReadOnlyList<TaskResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var tasks = await TaskQuery()
            .OrderBy(task => task.DueAt)
            .ThenBy(task => task.Id)
            .ToListAsync(cancellationToken);

        return tasks.Select(task => ToDto(task, now)).ToList();
    }

    public async System.Threading.Tasks.Task<IReadOnlyList<TaskResponseDto>> GetTodayAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var tomorrowStart = todayStart.AddDays(1);

        var tasks = await TaskQuery()
            .Where(task => !task.IsCompleted && task.DueAt < tomorrowStart)
            .OrderBy(task => task.DueAt >= todayStart)
            .ThenBy(task => task.DueAt)
            .ThenBy(task => task.Id)
            .ToListAsync(cancellationToken);

        return tasks.Select(task => ToDto(task, now)).ToList();
    }

    public async System.Threading.Tasks.Task<TaskResponseDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var task = await TaskQuery()
            .Where(task => task.Id == id)
            .SingleOrDefaultAsync(cancellationToken);

        return task is null
            ? null
            : ToDto(task, now);
    }

    public async System.Threading.Tasks.Task<CreateTaskResult> CreateAsync(
        CreateTaskRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var relationshipValidation = await ValidateRelationshipsAsync(
            request.AnimalId,
            request.EnclosureId,
            cancellationToken);

        if (relationshipValidation == RelationshipValidationResult.AnimalNotFound)
        {
            return CreateTaskResult.AnimalNotFound();
        }

        if (relationshipValidation == RelationshipValidationResult.EnclosureNotFound)
        {
            return CreateTaskResult.EnclosureNotFound();
        }

        var now = DateTime.UtcNow;
        var task = new TaskEntity
        {
            Title = request.Title!,
            Description = request.Description,
            TaskType = request.TaskType!.Value,
            DueAt = request.DueAt!.Value,
            RecurrenceType = request.RecurrenceType!.Value,
            RecurrenceInterval = NormalizeRecurrenceInterval(
                request.RecurrenceType.Value,
                request.RecurrenceInterval),
            IsCompleted = false,
            AnimalId = request.AnimalId,
            EnclosureId = request.EnclosureId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _dbContext.Tasks.Add(task);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var createdTask = await GetByIdAsync(task.Id, cancellationToken);

        return CreateTaskResult.Created(createdTask!);
    }

    public async System.Threading.Tasks.Task<UpdateTaskResult> UpdateAsync(
        int id,
        UpdateTaskRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var task = await _dbContext.Tasks
            .SingleOrDefaultAsync(task => task.Id == id, cancellationToken);

        if (task is null)
        {
            return UpdateTaskResult.TaskNotFound();
        }

        var relationshipValidation = await ValidateRelationshipsAsync(
            request.AnimalId,
            request.EnclosureId,
            cancellationToken);

        if (relationshipValidation == RelationshipValidationResult.AnimalNotFound)
        {
            return UpdateTaskResult.AnimalNotFound();
        }

        if (relationshipValidation == RelationshipValidationResult.EnclosureNotFound)
        {
            return UpdateTaskResult.EnclosureNotFound();
        }

        task.Title = request.Title!;
        task.Description = request.Description;
        task.TaskType = request.TaskType!.Value;
        task.DueAt = request.DueAt!.Value;
        task.RecurrenceType = request.RecurrenceType!.Value;
        task.RecurrenceInterval = NormalizeRecurrenceInterval(
            request.RecurrenceType.Value,
            request.RecurrenceInterval);
        task.AnimalId = request.AnimalId;
        task.EnclosureId = request.EnclosureId;
        task.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var updatedTask = await GetByIdAsync(id, cancellationToken);

        return UpdateTaskResult.Updated(updatedTask!);
    }

    public async System.Threading.Tasks.Task<CompleteTaskResult> CompleteAsync(
        int id,
        CompleteTaskRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var task = await _dbContext.Tasks
            .SingleOrDefaultAsync(task => task.Id == id, cancellationToken);

        if (task is null)
        {
            return CompleteTaskResult.NotFound();
        }

        if (task.IsCompleted)
        {
            return CompleteTaskResult.AlreadyCompleted();
        }

        var now = DateTime.UtcNow;
        task.IsCompleted = true;
        task.CompletedAt = now;
        task.CompletionNotes = request.CompletionNotes;
        task.CompletedBy = request.CompletedBy;
        task.UpdatedAt = now;

        _dbContext.ActivityEvents.Add(ToCompletionActivityEvent(task, now));

        if (CalculateNextDueAt(task) is { } nextDueAt)
        {
            _dbContext.Tasks.Add(new TaskEntity
            {
                Title = task.Title,
                Description = task.Description,
                TaskType = task.TaskType,
                DueAt = nextDueAt,
                RecurrenceType = task.RecurrenceType,
                RecurrenceInterval = task.RecurrenceInterval,
                IsCompleted = false,
                AnimalId = task.AnimalId,
                EnclosureId = task.EnclosureId,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var completedTask = await GetByIdAsync(id, cancellationToken);

        return CompleteTaskResult.Completed(completedTask!);
    }

    public async System.Threading.Tasks.Task<DeleteTaskResult> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var task = await _dbContext.Tasks
            .SingleOrDefaultAsync(task => task.Id == id, cancellationToken);

        if (task is null)
        {
            return DeleteTaskResult.NotFound;
        }

        _dbContext.Tasks.Remove(task);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return DeleteTaskResult.Deleted;
    }

    private IQueryable<TaskEntity> TaskQuery()
    {
        return _dbContext.Tasks
            .AsNoTracking()
            .Include(task => task.Animal)
            .Include(task => task.Enclosure);
    }

    private async System.Threading.Tasks.Task<RelationshipValidationResult> ValidateRelationshipsAsync(
        int? animalId,
        int? enclosureId,
        CancellationToken cancellationToken)
    {
        if (animalId is { } resolvedAnimalId)
        {
            var animalExists = await _dbContext.Animals
                .AsNoTracking()
                .AnyAsync(animal => animal.Id == resolvedAnimalId, cancellationToken);

            if (!animalExists)
            {
                return RelationshipValidationResult.AnimalNotFound;
            }
        }

        if (enclosureId is { } resolvedEnclosureId)
        {
            var enclosureExists = await _dbContext.Enclosures
                .AsNoTracking()
                .AnyAsync(enclosure => enclosure.Id == resolvedEnclosureId, cancellationToken);

            if (!enclosureExists)
            {
                return RelationshipValidationResult.EnclosureNotFound;
            }
        }

        return RelationshipValidationResult.Valid;
    }

    private static ActivityEvent ToCompletionActivityEvent(TaskEntity task, DateTime completedAt)
    {
        var activityEvent = new ActivityEvent
        {
            EventType = ActivityEventType.Task,
            OccurredAt = completedAt,
            Title = $"Task completed: {task.Title}",
            Notes = task.CompletionNotes,
            PerformedBy = task.CompletedBy,
            SourceType = "Task",
            Metadata = JsonSerializer.SerializeToDocument(new
            {
                taskId = task.Id,
                taskType = task.TaskType.ToString(),
                dueAt = task.DueAt,
                recurrenceType = task.RecurrenceType.ToString(),
                recurrenceInterval = task.RecurrenceInterval,
            }),
            CreatedAt = completedAt,
            UpdatedAt = completedAt,
        };

        if (task.AnimalId is { } animalId)
        {
            activityEvent.Animals.Add(new ActivityEventAnimal
            {
                AnimalId = animalId,
                RelationshipType = ActivityEventAnimalRelationshipType.Primary,
            });
        }

        if (task.EnclosureId is { } enclosureId)
        {
            activityEvent.Enclosures.Add(new ActivityEventEnclosure
            {
                EnclosureId = enclosureId,
                RelationshipType = ActivityEventEnclosureRelationshipType.Primary,
            });
        }

        return activityEvent;
    }

    private static DateTime? CalculateNextDueAt(TaskEntity task)
    {
        var interval = task.RecurrenceInterval ?? 1;

        return task.RecurrenceType switch
        {
            RecurrenceType.None => null,
            RecurrenceType.Daily => task.DueAt.AddDays(interval),
            RecurrenceType.Weekly => task.DueAt.AddDays(interval * 7),
            RecurrenceType.Monthly => task.DueAt.AddMonths(interval),
            RecurrenceType.Custom => task.DueAt.AddDays(interval),
            _ => null,
        };
    }

    private static int? NormalizeRecurrenceInterval(RecurrenceType recurrenceType, int? recurrenceInterval)
    {
        return recurrenceType == RecurrenceType.None
            ? null
            : recurrenceInterval ?? 1;
    }

    private static TaskResponseDto ToDto(TaskEntity task, DateTime now)
    {
        var todayStart = now.Date;
        var tomorrowStart = todayStart.AddDays(1);

        return new TaskResponseDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            TaskType = task.TaskType,
            DueAt = task.DueAt,
            RecurrenceType = task.RecurrenceType,
            RecurrenceInterval = task.RecurrenceInterval,
            IsCompleted = task.IsCompleted,
            CompletedAt = task.CompletedAt,
            CompletionNotes = task.CompletionNotes,
            CompletedBy = task.CompletedBy,
            AnimalId = task.AnimalId,
            AnimalName = task.Animal?.Name,
            EnclosureId = task.EnclosureId,
            EnclosureName = task.Enclosure?.Name,
            IsOverdue = !task.IsCompleted && task.DueAt < todayStart,
            IsDueToday = !task.IsCompleted && task.DueAt >= todayStart && task.DueAt < tomorrowStart,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
        };
    }

    private enum RelationshipValidationResult
    {
        Valid,
        AnimalNotFound,
        EnclosureNotFound,
    }
}
