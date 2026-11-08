using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using skipper_api.Domain.Animals;
namespace skipper_api.Dtos.Animals;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class VariantAssignmentRequestDto
{
    [Range(1, int.MaxValue)] public int? AssignmentId { get; set; }
    [Range(1, int.MaxValue)] public int? AnimalVariantId { get; set; }
    [MaxLength(100)] public string? CustomValue { get; set; }
    public VariantType? VariantType { get; set; }
}
public class VariantAssignmentDto
{
    public int AssignmentId { get; set; }
    public int? AnimalVariantId { get; set; }
    public int? SpeciesId { get; set; }
    public string DisplayName { get; set; } = "";
    public string? CustomValue { get; set; }
    public VariantType VariantType { get; set; }
    public bool IsCustom { get; set; }
    public int DisplayOrder { get; set; }
}
