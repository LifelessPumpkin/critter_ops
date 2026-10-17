using System.ComponentModel.DataAnnotations;
using skipper_api.Domain.Animals;

namespace skipper_api.Dtos.Animals;

public class CatalogSearchRequestDto
{
    [MaxLength(200)] public string? Search { get; set; }
    [Range(1, int.MaxValue)] public int? AnimalGroupId { get; set; }
    [Range(1, int.MaxValue)] public int? SpeciesId { get; set; }
    [Range(1, 1000000)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
}
public record AnimalGroupDto(int Id, string Name, int SortOrder, AnimalType LegacyAnimalType);
public record SpeciesDto(int Id, string? CommonName, string ScientificName, int AnimalGroupId, string AnimalGroupName, VariantType? PreferredVariantType);
public record AnimalVariantDto(int Id, int SpeciesId, string Name, VariantType VariantType);
public record CatalogPageDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, bool HasMore);
