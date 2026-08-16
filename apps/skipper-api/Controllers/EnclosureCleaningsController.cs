using Microsoft.AspNetCore.Mvc;
using skipper_api.Dtos.EnclosureCleanings;
using skipper_api.Services.EnclosureCleanings;

namespace skipper_api.Controllers;

[ApiController]
[Route("api/enclosures/{enclosureId:int}/cleanings")]
public class EnclosureCleaningsController : ControllerBase
{
    private const string GetEnclosureCleaningByIdRouteName = "GetEnclosureCleaningById";

    private readonly IEnclosureCleaningActivityService _cleaningService;

    public EnclosureCleaningsController(IEnclosureCleaningActivityService cleaningService)
    {
        _cleaningService = cleaningService;
    }

    /// <summary>
    /// Returns cleaning activities for an enclosure, newest first.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<EnclosureCleaningActivityDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<EnclosureCleaningActivityDto>>> GetByEnclosureId(
        int enclosureId,
        CancellationToken cancellationToken)
    {
        var cleanings = await _cleaningService.GetByEnclosureIdAsync(enclosureId, cancellationToken);

        return cleanings is null
            ? NotFound()
            : Ok(cleanings);
    }

    /// <summary>
    /// Returns a single cleaning activity for an enclosure.
    /// </summary>
    [HttpGet("{activityId:long}", Name = GetEnclosureCleaningByIdRouteName)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EnclosureCleaningActivityDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnclosureCleaningActivityDto>> GetById(
        int enclosureId,
        long activityId,
        CancellationToken cancellationToken)
    {
        var cleaning = await _cleaningService.GetByIdAsync(enclosureId, activityId, cancellationToken);

        return cleaning is null
            ? NotFound()
            : Ok(cleaning);
    }

    /// <summary>
    /// Creates a cleaning activity for an enclosure.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(EnclosureCleaningActivityDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnclosureCleaningActivityDto>> Create(
        int enclosureId,
        CreateEnclosureCleaningActivityDto request,
        CancellationToken cancellationToken)
    {
        var result = await _cleaningService.CreateAsync(enclosureId, request, cancellationToken);

        return result.Status switch
        {
            CreateEnclosureCleaningActivityResultStatus.Created => CreatedAtRoute(
                GetEnclosureCleaningByIdRouteName,
                new { enclosureId, activityId = result.Cleaning!.ActivityEventId },
                result.Cleaning),
            CreateEnclosureCleaningActivityResultStatus.EnclosureNotFound => NotFound(new
            {
                message = "The provided enclosure does not exist.",
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>
    /// Updates a cleaning activity.
    /// </summary>
    [HttpPut("{activityId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EnclosureCleaningActivityDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnclosureCleaningActivityDto>> Update(
        int enclosureId,
        long activityId,
        UpdateEnclosureCleaningActivityDto request,
        CancellationToken cancellationToken)
    {
        var result = await _cleaningService.UpdateAsync(enclosureId, activityId, request, cancellationToken);

        return result.Status switch
        {
            UpdateEnclosureCleaningActivityResultStatus.Updated => Ok(result.Cleaning),
            UpdateEnclosureCleaningActivityResultStatus.NotFound => NotFound(),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>
    /// Deletes a cleaning activity.
    /// </summary>
    [HttpDelete("{activityId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        int enclosureId,
        long activityId,
        CancellationToken cancellationToken)
    {
        var result = await _cleaningService.DeleteAsync(enclosureId, activityId, cancellationToken);

        return result switch
        {
            DeleteEnclosureCleaningActivityResult.Deleted => NoContent(),
            DeleteEnclosureCleaningActivityResult.NotFound => NotFound(),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}
