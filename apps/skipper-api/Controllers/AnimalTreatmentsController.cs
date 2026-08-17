using Microsoft.AspNetCore.Mvc;
using skipper_api.Dtos.AnimalTreatments;
using skipper_api.Services.AnimalTreatments;

namespace skipper_api.Controllers;

[ApiController]
[Route("api/animals/{animalId:int}/treatments")]
public class AnimalTreatmentsController : ControllerBase
{
    private const string GetAnimalTreatmentByIdRouteName = "GetAnimalTreatmentById";

    private readonly IAnimalTreatmentActivityService _treatmentService;

    public AnimalTreatmentsController(IAnimalTreatmentActivityService treatmentService)
    {
        _treatmentService = treatmentService;
    }

    /// <summary>Returns treatment activities for an animal, newest first.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<AnimalTreatmentActivityDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AnimalTreatmentActivityDto>>> GetByAnimalId(int animalId, CancellationToken cancellationToken)
    {
        var treatments = await _treatmentService.GetByAnimalIdAsync(animalId, cancellationToken);
        return treatments is null ? NotFound() : Ok(treatments);
    }

    /// <summary>Returns a single treatment activity for an animal.</summary>
    [HttpGet("{activityId:long}", Name = GetAnimalTreatmentByIdRouteName)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AnimalTreatmentActivityDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnimalTreatmentActivityDto>> GetById(int animalId, long activityId, CancellationToken cancellationToken)
    {
        var treatment = await _treatmentService.GetByIdAsync(animalId, activityId, cancellationToken);
        return treatment is null ? NotFound() : Ok(treatment);
    }

    /// <summary>Creates a treatment activity for an animal in its current enclosure.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(AnimalTreatmentActivityDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnimalTreatmentActivityDto>> Create(
        int animalId,
        CreateAnimalTreatmentActivityDto request,
        CancellationToken cancellationToken)
    {
        var result = await _treatmentService.CreateAsync(animalId, request, cancellationToken);

        return result.Status switch
        {
            CreateAnimalTreatmentActivityResultStatus.Created => CreatedAtRoute(
                GetAnimalTreatmentByIdRouteName,
                new { animalId, activityId = result.Treatment!.ActivityEventId },
                result.Treatment),
            CreateAnimalTreatmentActivityResultStatus.AnimalNotFound => NotFound(new
            {
                message = "The provided animal does not exist.",
            }),
            CreateAnimalTreatmentActivityResultStatus.EnclosureNotFound => NotFound(new
            {
                message = "The animal's enclosure does not exist.",
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>Updates a treatment activity.</summary>
    [HttpPut("{activityId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AnimalTreatmentActivityDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnimalTreatmentActivityDto>> Update(
        int animalId,
        long activityId,
        UpdateAnimalTreatmentActivityDto request,
        CancellationToken cancellationToken)
    {
        var result = await _treatmentService.UpdateAsync(animalId, activityId, request, cancellationToken);

        return result.Status switch
        {
            UpdateAnimalTreatmentActivityResultStatus.Updated => Ok(result.Treatment),
            UpdateAnimalTreatmentActivityResultStatus.NotFound => NotFound(),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>Deletes a treatment activity.</summary>
    [HttpDelete("{activityId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int animalId, long activityId, CancellationToken cancellationToken)
    {
        var result = await _treatmentService.DeleteAsync(animalId, activityId, cancellationToken);

        return result switch
        {
            DeleteAnimalTreatmentActivityResult.Deleted => NoContent(),
            DeleteAnimalTreatmentActivityResult.NotFound => NotFound(),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}
