namespace skipper_api.Dtos.Activity;

/// <summary>
/// Paged activity search response.
/// </summary>
public class ActivitySearchResponseDto
{
    /// <summary>The one-based page returned.</summary>
    public int Page { get; set; }

    /// <summary>The effective page size.</summary>
    public int PageSize { get; set; }

    /// <summary>Total matching records across all pages.</summary>
    public int TotalCount { get; set; }

    /// <summary>Total pages available for the current query.</summary>
    public int TotalPages { get; set; }

    /// <summary>Activity records for the requested page.</summary>
    public IReadOnlyList<ActivitySearchResultDto> Items { get; set; } = [];
}
