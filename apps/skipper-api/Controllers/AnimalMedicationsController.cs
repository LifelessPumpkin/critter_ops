using Microsoft.AspNetCore.Mvc;
using skipper_api.Dtos.AnimalMedications;
using skipper_api.Services.AnimalMedications;

namespace skipper_api.Controllers;

[ApiController]
[Route("api/animals/{animalId:int}/medications")]
public class AnimalMedicationsController : ControllerBase
{
    private const string GetAnimalMedicationByIdRouteName = "GetAnimalMedicationById";

    private readonly IAnimalMedicationActivityService _medicationService;

    public AnimalMedicationsController(IAnimalMedicationActivityService medicationService)
    {
        _medicationService = medicationService;
    }

    /// <summary>Returns medication activities for an animal, newest first.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<AnimalMedicationActivityDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AnimalMedicationActivityDto>>> GetByAnimalId(int animalId, CancellationToken cancellationToken)
    {
        var medications = await _medicationService.GetByAnimalIdAsync(animalId, cancellationToken);
        return medications is null ? NotFound() : Ok(medications);
    }

    /// <summary>Returns a single medication activity for an animal.</summary>
    [HttpGet("{activityId:long}", Name = GetAnimalMedicationByIdRouteName)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AnimalMedicationActivityDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnimalMedicationActivityDto>> GetById(int animalId, long activityId, CancellationToken cancellationToken)
    {
        var medication = await _medicationService.GetByIdAsync(animalId, activityId, cancellationToken);
        return medication is null ? NotFound() : Ok(medication);
    }

    /// <summary>Creates a medication activity for an animal in its current enclosure.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(AnimalMedicationActivityDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnimalMedicationActivityDto>> Create(
        int animalId,
        CreateAnimalMedicationActivityDto request,
        CancellationToken cancellationToken)
    {
        var result = await _medicationService.CreateAsync(animalId, request, cancellationToken);

        return result.Status switch
        {
            CreateAnimalMedicationActivityResultStatus.Created => CreatedAtRoute(
                GetAnimalMedicationByIdRouteName,
                new { animalId, activityId = result.Medication!.ActivityEventId },
                result.Medication),
            CreateAnimalMedicationActivityResultStatus.AnimalNotFound => NotFound(new
            {
                message = "The provided animal does not exist.",
            }),
            CreateAnimalMedicationActivityResultStatus.EnclosureNotFound => NotFound(new
            {
                message = "The animal's enclosure does not exist.",
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>Updates a medication activity.</summary>
    [HttpPut("{activityId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AnimalMedicationActivityDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnimalMedicationActivityDto>> Update(
        int animalId,
        long activityId,
        UpdateAnimalMedicationActivityDto request,
        CancellationToken cancellationToken)
    {
        var result = await _medicationService.UpdateAsync(animalId, activityId, request, cancellationToken);

        return result.Status switch
        {
            UpdateAnimalMedicationActivityResultStatus.Updated => Ok(result.Medication),
            UpdateAnimalMedicationActivityResultStatus.NotFound => NotFound(),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>Deletes a medication activity.</summary>
    [HttpDelete("{activityId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int animalId, long activityId, CancellationToken cancellationToken)
    {
        var result = await _medicationService.DeleteAsync(animalId, activityId, cancellationToken);

        return result switch
        {
            DeleteAnimalMedicationActivityResult.Deleted => NoContent(),
            DeleteAnimalMedicationActivityResult.NotFound => NotFound(),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}
