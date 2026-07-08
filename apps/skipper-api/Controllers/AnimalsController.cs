using Microsoft.AspNetCore.Mvc;
using skipper_api.Dtos.Animals;
using skipper_api.Services.Animals;

namespace skipper_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnimalsController : ControllerBase
{
    private readonly IAnimalService _animalService;

    public AnimalsController(IAnimalService animalService)
    {
        _animalService = animalService;
    }

    /// <summary>
    /// Returns all animals.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<AnimalResponseDto>))]
    public async Task<ActionResult<IReadOnlyList<AnimalResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var animals = await _animalService.GetAllAsync(cancellationToken);
        return Ok(animals);
    }

    /// <summary>
    /// Returns a single animal by ID.
    /// </summary>
    [HttpGet("{id:int}", Name = nameof(GetAnimalById))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AnimalResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnimalResponseDto>> GetAnimalById(int id, CancellationToken cancellationToken)
    {
        var animal = await _animalService.GetByIdAsync(id, cancellationToken);

        return animal is null
            ? NotFound()
            : Ok(animal);
    }

    /// <summary>
    /// Creates a new animal.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(AnimalResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AnimalResponseDto>> Create(
        CreateAnimalRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _animalService.CreateAsync(request, cancellationToken);

        return result.Status switch
        {
            CreateAnimalResultStatus.Created => CreatedAtRoute(
                nameof(GetAnimalById),
                new { id = result.Animal!.Id },
                result.Animal),
            CreateAnimalResultStatus.EnclosureNotFound => BadRequest(new
            {
                message = "The provided enclosure does not exist.",
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>
    /// Updates an existing animal.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AnimalResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnimalResponseDto>> Update(
        int id,
        UpdateAnimalRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _animalService.UpdateAsync(id, request, cancellationToken);

        return result.Status switch
        {
            UpdateAnimalResultStatus.Updated => Ok(result.Animal),
            UpdateAnimalResultStatus.AnimalNotFound => NotFound(),
            UpdateAnimalResultStatus.EnclosureNotFound => BadRequest(new
            {
                message = "The provided enclosure does not exist.",
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>
    /// Deletes an animal.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _animalService.DeleteAsync(id, cancellationToken);

        return result switch
        {
            DeleteAnimalResult.Deleted => NoContent(),
            DeleteAnimalResult.NotFound => NotFound(),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}
