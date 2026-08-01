using Microsoft.AspNetCore.Mvc;
using skipper_api.Dtos.EnclosureTimeline;
using skipper_api.Services.EnclosureTimeline;

namespace skipper_api.Controllers;

[ApiController]
[Route("api/enclosures/{enclosureId:int}/timeline")]
public class EnclosureTimelineController : ControllerBase
{
    private const string GetTimelineEventByIdRouteName = "GetEnclosureTimelineEventById";

    private readonly IEnclosureTimelineService _enclosureTimelineService;

    public EnclosureTimelineController(IEnclosureTimelineService enclosureTimelineService)
    {
        _enclosureTimelineService = enclosureTimelineService;
    }

    /// <summary>
    /// Returns timeline events for an enclosure, newest first.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<EnclosureTimelineEventDto>))]
    public async Task<ActionResult<IReadOnlyList<EnclosureTimelineEventDto>>> GetByEnclosureId(
        int enclosureId,
        CancellationToken cancellationToken)
    {
        var timelineEvents = await _enclosureTimelineService.GetByEnclosureIdAsync(enclosureId, cancellationToken);
        return Ok(timelineEvents);
    }

    /// <summary>
    /// Returns a single timeline event for an enclosure.
    /// </summary>
    [HttpGet("{eventId:long}", Name = GetTimelineEventByIdRouteName)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EnclosureTimelineEventDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnclosureTimelineEventDto>> GetById(
        int enclosureId,
        long eventId,
        CancellationToken cancellationToken)
    {
        var timelineEvent = await _enclosureTimelineService.GetByIdAsync(
            enclosureId,
            eventId,
            cancellationToken);

        return timelineEvent is null
            ? NotFound()
            : Ok(timelineEvent);
    }

    /// <summary>
    /// Creates a new timeline event for an enclosure.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(EnclosureTimelineEventDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EnclosureTimelineEventDto>> Create(
        int enclosureId,
        CreateEnclosureTimelineEventDto request,
        CancellationToken cancellationToken)
    {
        var result = await _enclosureTimelineService.CreateAsync(enclosureId, request, cancellationToken);

        return result.Status switch
        {
            CreateTimelineEventResultStatus.Created => CreatedAtRoute(
                GetTimelineEventByIdRouteName,
                new { enclosureId, eventId = result.TimelineEvent!.Id },
                result.TimelineEvent),
            CreateTimelineEventResultStatus.EnclosureNotFound => BadRequest(new
            {
                message = "The provided enclosure does not exist.",
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>
    /// Updates an existing timeline event for an enclosure.
    /// </summary>
    [HttpPut("{eventId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EnclosureTimelineEventDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnclosureTimelineEventDto>> Update(
        int enclosureId,
        long eventId,
        UpdateEnclosureTimelineEventDto request,
        CancellationToken cancellationToken)
    {
        var result = await _enclosureTimelineService.UpdateAsync(
            enclosureId,
            eventId,
            request,
            cancellationToken);

        return result.Status switch
        {
            UpdateTimelineEventResultStatus.Updated => Ok(result.TimelineEvent),
            UpdateTimelineEventResultStatus.NotFound => NotFound(),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>
    /// Deletes a timeline event for an enclosure.
    /// </summary>
    [HttpDelete("{eventId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        int enclosureId,
        long eventId,
        CancellationToken cancellationToken)
    {
        var result = await _enclosureTimelineService.DeleteAsync(enclosureId, eventId, cancellationToken);

        return result switch
        {
            DeleteTimelineEventResult.Deleted => NoContent(),
            DeleteTimelineEventResult.NotFound => NotFound(),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}
