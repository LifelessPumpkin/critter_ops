using System.ComponentModel.DataAnnotations;
using skipper_api.Domain.Animals;

namespace skipper_api.Dtos.Animals;

public class CatalogSearchRequestDto : IValidatableObject
{
    [MaxLength(200)] public string? Search { get; set; }
    [Range(1, int.MaxValue)] public int? AnimalGroupId { get; set; }
    [Range(1, int.MaxValue)] public int? SpeciesId { get; set; }
    [Range(1, 1000000)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 25;
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if ((long)(Page - 1L) * PageSize > 100_000)
            yield return new ValidationResult("Offset cannot exceed 100000. Narrow the search.", [nameof(Page)]);
    }
}
public record AnimalGroupDto(int Id, string Name, int SortOrder, AnimalType LegacyAnimalType);
public record SpeciesDto(int Id, string? CommonName, string ScientificName, int AnimalGroupId, string AnimalGroupName, VariantType? PreferredVariantType);
public record AnimalVariantDto(int Id, int SpeciesId, string Name, VariantType VariantType);
public record CatalogPageDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, bool HasMore);
