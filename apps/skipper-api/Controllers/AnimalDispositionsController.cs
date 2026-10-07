using Microsoft.AspNetCore.Mvc;
using skipper_api.Dtos.AnimalDispositions;
using skipper_api.Services.AnimalDispositions;

namespace skipper_api.Controllers;

[ApiController]
[Route("api/animals/{animalId:int}/dispositions")]
public class AnimalDispositionsController : ControllerBase
{
    private const string GetAnimalDispositionByIdRouteName = "GetAnimalDispositionById";

    private readonly IAnimalDispositionActivityService _dispositionService;

    public AnimalDispositionsController(IAnimalDispositionActivityService dispositionService)
    {
        _dispositionService = dispositionService;
    }

    /// <summary>
    /// Returns disposition activities for an animal, newest first.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<AnimalDispositionActivityDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AnimalDispositionActivityDto>>> GetByAnimalId(
        int animalId,
        CancellationToken cancellationToken)
    {
        var dispositions = await _dispositionService.GetByAnimalIdAsync(animalId, cancellationToken);

        return dispositions is null
            ? NotFound()
            : Ok(dispositions);
    }

    /// <summary>
    /// Returns a single disposition activity for an animal.
    /// </summary>
    [HttpGet("{activityId:long}", Name = GetAnimalDispositionByIdRouteName)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AnimalDispositionActivityDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnimalDispositionActivityDto>> GetById(
        int animalId,
        long activityId,
        CancellationToken cancellationToken)
    {
        var disposition = await _dispositionService.GetByIdAsync(animalId, activityId, cancellationToken);

        return disposition is null
            ? NotFound()
            : Ok(disposition);
    }

    /// <summary>
    /// Creates a disposition activity and updates the animal's current status.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(AnimalDispositionActivityDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AnimalDispositionActivityDto>> Create(
        int animalId,
        CreateAnimalDispositionActivityDto request,
        CancellationToken cancellationToken)
    {
        var result = await _dispositionService.CreateAsync(animalId, request, cancellationToken);

        return result.Status switch
        {
            CreateAnimalDispositionActivityResultStatus.Created => CreatedAtRoute(
                GetAnimalDispositionByIdRouteName,
                new { animalId, activityId = result.Disposition!.ActivityEventId },
                result.Disposition),
            CreateAnimalDispositionActivityResultStatus.AnimalNotFound => NotFound(new
            {
                message = "The provided animal does not exist.",
            }),
            CreateAnimalDispositionActivityResultStatus.EnclosureNotFound => NotFound(new
            {
                message = "The animal's enclosure does not exist.",
            }),
            CreateAnimalDispositionActivityResultStatus.OccurredBeforeAcquisition => BadRequest(new
            {
                message = "Disposition cannot occur before the animal's acquisition date.",
            }),
            CreateAnimalDispositionActivityResultStatus.AnimalAlreadyDisposed => Conflict(new
            {
                message = "The animal is already in a terminal disposition state.",
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>
    /// Updates a disposition activity and keeps the animal's disposition state in sync.
    /// </summary>
    [HttpPut("{activityId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AnimalDispositionActivityDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnimalDispositionActivityDto>> Update(
        int animalId,
        long activityId,
        UpdateAnimalDispositionActivityDto request,
        CancellationToken cancellationToken)
    {
        var result = await _dispositionService.UpdateAsync(animalId, activityId, request, cancellationToken);

        return result.Status switch
        {
            UpdateAnimalDispositionActivityResultStatus.Updated => Ok(result.Disposition),
            UpdateAnimalDispositionActivityResultStatus.NotFound => NotFound(),
            UpdateAnimalDispositionActivityResultStatus.OccurredBeforeAcquisition => BadRequest(new
            {
                message = "Disposition cannot occur before the animal's acquisition date.",
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>
    /// Deletes the latest disposition activity and restores the animal to active status.
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
        var result = await _dispositionService.DeleteAsync(animalId, activityId, cancellationToken);

        return result switch
        {
            DeleteAnimalDispositionActivityResult.Deleted => NoContent(),
            DeleteAnimalDispositionActivityResult.NotFound => NotFound(),
            DeleteAnimalDispositionActivityResult.NotLatestDisposition => Conflict(new
            {
                message = "Only the latest disposition can be deleted.",
            }),
            DeleteAnimalDispositionActivityResult.AnimalStateMismatch => Conflict(new
            {
                message = "The animal's current status no longer matches this disposition.",
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}
