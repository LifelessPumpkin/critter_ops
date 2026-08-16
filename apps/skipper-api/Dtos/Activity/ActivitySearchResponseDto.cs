namespace skipper_api.Dtos.Activity;

/// <summary>
/// Paged activity search response.
/// </summary>
public class ActivitySearchResponseDto
{
    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public IReadOnlyList<ActivitySearchResultDto> Items { get; set; } = [];
}
