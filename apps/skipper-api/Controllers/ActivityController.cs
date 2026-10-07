using Microsoft.AspNetCore.Mvc;
using skipper_api.Dtos.Activity;
using skipper_api.Services.Activity;

namespace skipper_api.Controllers;

[ApiController]
[Route("api/activity")]
public class ActivityController : ControllerBase
{
    private readonly IActivitySearchService _activitySearchService;

    public ActivityController(IActivitySearchService activitySearchService)
    {
        _activitySearchService = activitySearchService;
    }

    /// <summary>
    /// Searches the shared activity ledger across animals, enclosures, event types, performers, and date ranges.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ActivitySearchResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ActivitySearchResponseDto>> Search(
        [FromQuery] ActivitySearchRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.From.HasValue &&
            request.To.HasValue &&
            request.From.Value > request.To.Value)
        {
            return BadRequest(new
            {
                message = "The from filter must be earlier than or equal to the to filter.",
            });
        }

        var response = await _activitySearchService.SearchAsync(request, cancellationToken);
        return Ok(response);
    }
}
