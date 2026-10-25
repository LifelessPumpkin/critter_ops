namespace skipper_api.Domain.Activity;

/// <summary>
/// Result recorded for an animal or enclosure inspection.
/// </summary>
public enum InspectionResult
{
    Satisfactory,
    AttentionRequired,
    Failed,
    NotObserved,
    Other,
}
