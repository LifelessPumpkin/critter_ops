namespace skipper_api.Dtos.Animals;

/// <summary>Paged animals response with complete lifecycle counts for the active filters.</summary>
public class AnimalSearchResponseDto
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int AllCount { get; set; }
    public int InCareCount { get; set; }
    public int OutOfCareCount { get; set; }
    public IReadOnlyList<AnimalListItemDto> Items { get; set; } = [];
}
