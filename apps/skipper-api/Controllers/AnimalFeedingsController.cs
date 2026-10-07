using Microsoft.AspNetCore.Mvc;
using skipper_api.Dtos.AnimalFeedings;
using skipper_api.Services.AnimalFeedings;

namespace skipper_api.Controllers;

[ApiController]
[Route("api/animals/{animalId:int}/feedings")]
public class AnimalFeedingsController : ControllerBase
{
    private const string GetAnimalFeedingByIdRouteName = "GetAnimalFeedingById";

    private readonly IAnimalFeedingActivityService _feedingService;

    public AnimalFeedingsController(IAnimalFeedingActivityService feedingService)
    {
        _feedingService = feedingService;
    }

    /// <summary>
    /// Returns feeding activities for an animal, newest first.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<AnimalFeedingActivityDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AnimalFeedingActivityDto>>> GetByAnimalId(
        int animalId,
        CancellationToken cancellationToken)
    {
        var feedings = await _feedingService.GetByAnimalIdAsync(animalId, cancellationToken);

        return feedings is null
            ? NotFound()
            : Ok(feedings);
    }

    /// <summary>
    /// Returns a single feeding activity for an animal.
    /// </summary>
    [HttpGet("{activityId:long}", Name = GetAnimalFeedingByIdRouteName)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AnimalFeedingActivityDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnimalFeedingActivityDto>> GetById(
        int animalId,
        long activityId,
        CancellationToken cancellationToken)
    {
        var feeding = await _feedingService.GetByIdAsync(animalId, activityId, cancellationToken);

        return feeding is null
            ? NotFound()
            : Ok(feeding);
    }

    /// <summary>
    /// Creates a feeding activity for an animal in its current enclosure.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(AnimalFeedingActivityDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnimalFeedingActivityDto>> Create(
        int animalId,
        CreateAnimalFeedingActivityDto request,
        CancellationToken cancellationToken)
    {
        var result = await _feedingService.CreateAsync(animalId, request, cancellationToken);

        return result.Status switch
        {
            CreateAnimalFeedingActivityResultStatus.Created => CreatedAtRoute(
                GetAnimalFeedingByIdRouteName,
                new { animalId, activityId = result.Feeding!.ActivityEventId },
                result.Feeding),
            CreateAnimalFeedingActivityResultStatus.AnimalNotFound => NotFound(new
            {
                message = "The provided animal does not exist.",
            }),
            CreateAnimalFeedingActivityResultStatus.EnclosureNotFound => NotFound(new
            {
                message = "The animal's enclosure does not exist.",
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>
    /// Updates a feeding activity.
    /// </summary>
    [HttpPut("{activityId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AnimalFeedingActivityDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnimalFeedingActivityDto>> Update(
        int animalId,
        long activityId,
        UpdateAnimalFeedingActivityDto request,
        CancellationToken cancellationToken)
    {
        var result = await _feedingService.UpdateAsync(animalId, activityId, request, cancellationToken);

        return result.Status switch
        {
            UpdateAnimalFeedingActivityResultStatus.Updated => Ok(result.Feeding),
            UpdateAnimalFeedingActivityResultStatus.NotFound => NotFound(),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>
    /// Deletes a feeding activity.
    /// </summary>
    [HttpDelete("{activityId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        int animalId,
        long activityId,
        CancellationToken cancellationToken)
    {
        var result = await _feedingService.DeleteAsync(animalId, activityId, cancellationToken);

        return result switch
        {
            DeleteAnimalFeedingActivityResult.Deleted => NoContent(),
            DeleteAnimalFeedingActivityResult.NotFound => NotFound(),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}
