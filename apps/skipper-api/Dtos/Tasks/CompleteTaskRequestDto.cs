using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace skipper_api.Dtos.Tasks;

/// <summary>
/// API request shape for completing a husbandry task.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class CompleteTaskRequestDto
{
    public string? CompletionNotes { get; set; }

    [MaxLength(100)]
    public string? CompletedBy { get; set; }
}
