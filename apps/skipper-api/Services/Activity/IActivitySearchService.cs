using skipper_api.Dtos.Activity;

namespace skipper_api.Services.Activity;

public interface IActivitySearchService
{
    Task<ActivitySearchResponseDto> SearchAsync(
        ActivitySearchRequestDto request,
        CancellationToken cancellationToken = default);
}
