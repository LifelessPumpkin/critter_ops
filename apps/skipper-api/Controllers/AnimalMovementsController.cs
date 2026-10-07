using Microsoft.AspNetCore.Mvc;
using skipper_api.Dtos.AnimalMovements;
using skipper_api.Services.AnimalMovements;

namespace skipper_api.Controllers;

[ApiController]
[Route("api/animals/{animalId:int}/movements")]
public class AnimalMovementsController : ControllerBase
{
    private const string GetAnimalMovementByIdRouteName = "GetAnimalMovementById";

    private readonly IAnimalMovementActivityService _movementService;

    public AnimalMovementsController(IAnimalMovementActivityService movementService)
    {
        _movementService = movementService;
    }

    /// <summary>
    /// Returns movement activities for an animal, newest first.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<AnimalMovementActivityDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AnimalMovementActivityDto>>> GetByAnimalId(
        int animalId,
        CancellationToken cancellationToken)
    {
        var movements = await _movementService.GetByAnimalIdAsync(animalId, cancellationToken);

        return movements is null
            ? NotFound()
            : Ok(movements);
    }

    /// <summary>
    /// Returns a single movement activity for an animal.
    /// </summary>
    [HttpGet("{activityId:long}", Name = GetAnimalMovementByIdRouteName)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AnimalMovementActivityDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnimalMovementActivityDto>> GetById(
        int animalId,
        long activityId,
        CancellationToken cancellationToken)
    {
        var movement = await _movementService.GetByIdAsync(animalId, activityId, cancellationToken);

        return movement is null
            ? NotFound()
            : Ok(movement);
    }

    /// <summary>
    /// Creates a movement activity and updates the animal's current enclosure.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(AnimalMovementActivityDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AnimalMovementActivityDto>> Create(
        int animalId,
        CreateAnimalMovementActivityDto request,
        CancellationToken cancellationToken)
    {
        var result = await _movementService.CreateAsync(animalId, request, cancellationToken);

        return result.Status switch
        {
            CreateAnimalMovementActivityResultStatus.Created => CreatedAtRoute(
                GetAnimalMovementByIdRouteName,
                new { animalId, activityId = result.Movement!.ActivityEventId },
                result.Movement),
            CreateAnimalMovementActivityResultStatus.AnimalNotFound => NotFound(new
            {
                message = "The provided animal does not exist.",
            }),
            CreateAnimalMovementActivityResultStatus.SourceEnclosureNotFound => NotFound(new
            {
                message = "The provided source enclosure does not exist.",
            }),
            CreateAnimalMovementActivityResultStatus.DestinationEnclosureNotFound => NotFound(new
            {
                message = "The provided destination enclosure does not exist.",
            }),
            CreateAnimalMovementActivityResultStatus.SourceDoesNotMatchCurrentEnclosure => Conflict(new
            {
                message = "The source enclosure must match the animal's current enclosure.",
            }),
            CreateAnimalMovementActivityResultStatus.DestinationMatchesCurrentEnclosure => Conflict(new
            {
                message = "The destination enclosure must differ from the animal's current enclosure.",
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>
    /// Updates descriptive fields for a movement activity.
    /// </summary>
    [HttpPut("{activityId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AnimalMovementActivityDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnimalMovementActivityDto>> Update(
        int animalId,
        long activityId,
        UpdateAnimalMovementActivityDto request,
        CancellationToken cancellationToken)
    {
        var result = await _movementService.UpdateAsync(animalId, activityId, request, cancellationToken);

        return result.Status switch
        {
            UpdateAnimalMovementActivityResultStatus.Updated => Ok(result.Movement),
            UpdateAnimalMovementActivityResultStatus.NotFound => NotFound(),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>
    /// Deletes the latest movement activity and restores the animal's previous enclosure.
    /// </summary>
    [HttpDelete("{activityId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        int animalId,
        long activityId,
        CancellationToken cancellationToken)
    {
        var result = await _movementService.DeleteAsync(animalId, activityId, cancellationToken);

        return result switch
        {
            DeleteAnimalMovementActivityResult.Deleted => NoContent(),
            DeleteAnimalMovementActivityResult.NotFound => NotFound(),
            DeleteAnimalMovementActivityResult.NotLatestMovement => Conflict(new
            {
                message = "Only the latest movement can be deleted.",
            }),
            DeleteAnimalMovementActivityResult.AnimalLocationMismatch => Conflict(new
            {
                message = "The animal's current enclosure no longer matches this movement's destination.",
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}
