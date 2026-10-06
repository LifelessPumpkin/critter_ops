using skipper_api.Domain.Activity;
using skipper_api.Domain.Tasks;

namespace skipper_api.Services.Tasks;

/// <summary>
/// Maps scheduled task categories to the semantic activity recorded when the task is completed.
/// </summary>
public static class CompletedTaskActivityTypeMapper
{
    public static ActivityEventType GetActivityEventType(TaskType taskType)
    {
        return taskType switch
        {
            TaskType.Feeding => ActivityEventType.Feeding,
            TaskType.Medication => ActivityEventType.Medication,
            TaskType.Cleaning => ActivityEventType.Cleaning,
            TaskType.WaterChange => ActivityEventType.WaterChange,
            TaskType.Inspection => ActivityEventType.Inspection,
            TaskType.Maintenance => ActivityEventType.Maintenance,
            TaskType.Note => ActivityEventType.Note,
            TaskType.General => ActivityEventType.General,
            TaskType.Other => ActivityEventType.Task,
            _ => ActivityEventType.Task,
        };
    }
}
