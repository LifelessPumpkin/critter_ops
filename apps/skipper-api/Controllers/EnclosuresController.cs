using Microsoft.AspNetCore.Mvc;
using skipper_api.Dtos.Enclosures;
using skipper_api.Services.Enclosures;

namespace skipper_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnclosuresController : ControllerBase
{
    private readonly IEnclosureService _enclosureService;

    public EnclosuresController(IEnclosureService enclosureService)
    {
        _enclosureService = enclosureService;
    }

    /// <summary>
    /// Returns all enclosures.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<EnclosureResponseDto>))]
    public async Task<ActionResult<IReadOnlyList<EnclosureResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var enclosures = await _enclosureService.GetAllAsync(cancellationToken);
        return Ok(enclosures);
    }

    /// <summary>
    /// Returns a single enclosure by ID.
    /// </summary>
    [HttpGet("{id:int}", Name = nameof(GetById))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EnclosureResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnclosureResponseDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var enclosure = await _enclosureService.GetByIdAsync(id, cancellationToken);

        return enclosure is null
            ? NotFound()
            : Ok(enclosure);
    }

    /// <summary>
    /// Creates a new enclosure.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(EnclosureResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EnclosureResponseDto>> Create(
        CreateEnclosureRequestDto request,
        CancellationToken cancellationToken)
    {
        var enclosure = await _enclosureService.CreateAsync(request, cancellationToken);

        return CreatedAtRoute(
            nameof(GetById),
            new { id = enclosure.Id },
            enclosure);
    }

    /// <summary>
    /// Updates an existing enclosure.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EnclosureResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnclosureResponseDto>> Update(
        int id,
        UpdateEnclosureRequestDto request,
        CancellationToken cancellationToken)
    {
        var enclosure = await _enclosureService.UpdateAsync(id, request, cancellationToken);

        return enclosure is null
            ? NotFound()
            : Ok(enclosure);
    }

    /// <summary>
    /// Deletes an enclosure when no animals are assigned to it.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _enclosureService.DeleteAsync(id, cancellationToken);

        return result switch
        {
            DeleteEnclosureResult.Deleted => NoContent(),
            DeleteEnclosureResult.NotFound => NotFound(),
            DeleteEnclosureResult.HasAssignedAnimals => Conflict(new
            {
                message = "Cannot delete an enclosure while animals are assigned to it.",
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}
