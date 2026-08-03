using Microsoft.AspNetCore.Mvc;
using skipper_api.Dtos.AnimalTimeline;
using skipper_api.Services.AnimalTimeline;

namespace skipper_api.Controllers;

[ApiController]
[Route("api/animals/{animalId:int}/timeline")]
public class AnimalTimelineController : ControllerBase
{
    private const string GetAnimalTimelineEventByIdRouteName = "GetAnimalTimelineEventById";

    private readonly IAnimalTimelineService _animalTimelineService;

    public AnimalTimelineController(IAnimalTimelineService animalTimelineService)
    {
        _animalTimelineService = animalTimelineService;
    }

    /// <summary>
    /// Returns timeline events for an animal, newest first.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<AnimalTimelineEventDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AnimalTimelineEventDto>>> GetByAnimalId(
        int animalId,
        CancellationToken cancellationToken)
    {
        var timelineEvents = await _animalTimelineService.GetByAnimalIdAsync(animalId, cancellationToken);

        return timelineEvents is null
            ? NotFound()
            : Ok(timelineEvents);
    }

    /// <summary>
    /// Returns a single timeline event for an animal.
    /// </summary>
    [HttpGet("{eventId:long}", Name = GetAnimalTimelineEventByIdRouteName)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AnimalTimelineEventDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnimalTimelineEventDto>> GetById(
        int animalId,
        long eventId,
        CancellationToken cancellationToken)
    {
        var timelineEvent = await _animalTimelineService.GetByIdAsync(animalId, eventId, cancellationToken);

        return timelineEvent is null
            ? NotFound()
            : Ok(timelineEvent);
    }

    /// <summary>
    /// Creates a new timeline event for an animal.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(AnimalTimelineEventDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnimalTimelineEventDto>> Create(
        int animalId,
        CreateAnimalTimelineEventDto request,
        CancellationToken cancellationToken)
    {
        var result = await _animalTimelineService.CreateAsync(animalId, request, cancellationToken);

        return result.Status switch
        {
            CreateAnimalTimelineEventResultStatus.Created => CreatedAtRoute(
                GetAnimalTimelineEventByIdRouteName,
                new { animalId, eventId = result.TimelineEvent!.Id },
                result.TimelineEvent),
            CreateAnimalTimelineEventResultStatus.AnimalNotFound => NotFound(new
            {
                message = "The provided animal does not exist.",
            }),
            CreateAnimalTimelineEventResultStatus.EnclosureNotFound => NotFound(new
            {
                message = "The provided enclosure does not exist.",
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>
    /// Updates an existing timeline event for an animal.
    /// </summary>
    [HttpPut("{eventId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AnimalTimelineEventDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnimalTimelineEventDto>> Update(
        int animalId,
        long eventId,
        UpdateAnimalTimelineEventDto request,
        CancellationToken cancellationToken)
    {
        var result = await _animalTimelineService.UpdateAsync(animalId, eventId, request, cancellationToken);

        return result.Status switch
        {
            UpdateAnimalTimelineEventResultStatus.Updated => Ok(result.TimelineEvent),
            UpdateAnimalTimelineEventResultStatus.NotFound => NotFound(),
            UpdateAnimalTimelineEventResultStatus.EnclosureNotFound => NotFound(new
            {
                message = "The provided enclosure does not exist.",
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>
    /// Deletes a timeline event for an animal.
    /// </summary>
    [HttpDelete("{eventId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        int animalId,
        long eventId,
        CancellationToken cancellationToken)
    {
        var result = await _animalTimelineService.DeleteAsync(animalId, eventId, cancellationToken);

        return result switch
        {
            DeleteAnimalTimelineEventResult.Deleted => NoContent(),
            DeleteAnimalTimelineEventResult.NotFound => NotFound(),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}
