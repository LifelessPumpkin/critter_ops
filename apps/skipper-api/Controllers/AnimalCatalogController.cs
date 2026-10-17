using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using skipper_api.Data;
using skipper_api.Dtos.Animals;

namespace skipper_api.Controllers;

[ApiController]
public class AnimalCatalogController(ProfessorDbContext db) : ControllerBase
{
    [HttpGet("api/animal-groups")]
    public async Task<ActionResult<IReadOnlyList<AnimalGroupDto>>> Groups(CancellationToken ct) =>
        Ok(await db.AnimalGroups.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => new AnimalGroupDto(x.Id, x.Name, x.SortOrder, x.LegacyAnimalType)).ToListAsync(ct));

    [HttpGet("api/species")]
    public async Task<ActionResult<CatalogPageDto<SpeciesDto>>> Species([FromQuery] CatalogSearchRequestDto request, CancellationToken ct)
    {
        var query = db.Species.AsNoTracking().Where(x => x.IsActive && x.AnimalGroup.IsActive);
        if (request.AnimalGroupId is {} group) query = query.Where(x => x.AnimalGroupId == group);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = Pattern(request.Search);
            query = query.Where(x => EF.Functions.ILike(x.CommonName!, pattern, "\\") || EF.Functions.ILike(x.ScientificName, pattern, "\\"));
        }
        var items = await query.OrderBy(x => x.CommonName ?? x.ScientificName).ThenBy(x => x.ScientificName).ThenBy(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize + 1)
            .Select(x => new SpeciesDto(x.Id, x.CommonName, x.ScientificName, x.AnimalGroupId, x.AnimalGroup.Name, x.PreferredVariantType)).ToListAsync(ct);
        return Ok(new CatalogPageDto<SpeciesDto>(items.Take(request.PageSize).ToList(), request.Page, request.PageSize, items.Count > request.PageSize));
    }

    [HttpGet("api/animal-variants")]
    public async Task<ActionResult<CatalogPageDto<AnimalVariantDto>>> Variants([FromQuery] CatalogSearchRequestDto request, CancellationToken ct)
    {
        var query = db.AnimalVariants.AsNoTracking().Where(x => x.IsActive && x.Species.IsActive && x.Species.AnimalGroup.IsActive);
        if (request.SpeciesId is {} species) query = query.Where(x => x.SpeciesId == species);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = Pattern(request.Search);
            query = query.Where(x => EF.Functions.ILike(x.Name, pattern, "\\"));
        }
        var items = await query.OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize + 1)
            .Select(x => new AnimalVariantDto(x.Id, x.SpeciesId, x.Name, x.VariantType)).ToListAsync(ct);
        return Ok(new CatalogPageDto<AnimalVariantDto>(items.Take(request.PageSize).ToList(), request.Page, request.PageSize, items.Count > request.PageSize));
    }

    private static string Pattern(string value) => "%" + value.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
}
