using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
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
        if (!Valid(request)) return ValidationProblem(statusCode: 400, modelStateDictionary: ModelState);
        var term = Normalize(request.Search);
        var exact = Escape(term);
        var prefix = exact + "%";
        var query = db.Species.AsNoTracking().Where(x => x.IsActive && x.AnimalGroup.IsActive);
        if (request.AnimalGroupId is {} group) query = query.Where(x => x.AnimalGroupId == group);
        if (term.Length > 0)
        {
            var pattern = "%" + exact + "%";
            // A union of matching IDs lets PostgreSQL use each name index independently.
            // Filtering the outer Species query keeps one canonical row even if many names match.
            var matches = db.Species.Where(x => EF.Functions.ILike(x.CommonName!, pattern, "\\") || EF.Functions.ILike(x.ScientificName, pattern, "\\")).Select(x => x.Id)
                .Union(db.SpeciesCommonNames.Where(n => EF.Functions.ILike(n.Name, pattern, "\\")).Select(n => n.SpeciesId))
                .Union(db.SpeciesAliases.Where(n => EF.Functions.ILike(n.Name, pattern, "\\")).Select(n => n.SpeciesId));
            query = query.Where(x => matches.Contains(x.Id));
        }
        var items = await query.OrderBy(x => term == "" ? 0 :
                EF.Functions.ILike(x.ScientificName, exact, "\\") ? 0 :
                EF.Functions.ILike(x.CommonName!, exact, "\\") ? 1 :
                x.CommonNames.Any(n => EF.Functions.ILike(n.Name, exact, "\\")) || x.Aliases.Any(n => EF.Functions.ILike(n.Name, exact, "\\")) ? 2 :
                EF.Functions.ILike(x.ScientificName, prefix, "\\") || EF.Functions.ILike(x.CommonName!, prefix, "\\") ||
                x.CommonNames.Any(n => EF.Functions.ILike(n.Name, prefix, "\\")) || x.Aliases.Any(n => EF.Functions.ILike(n.Name, prefix, "\\")) ? 3 : 4)
            .ThenBy(x => x.CommonName ?? x.ScientificName).ThenBy(x => x.ScientificName).ThenBy(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize + 1)
            .Select(x => new SpeciesDto(x.Id, x.CommonName ?? x.CommonNames.OrderByDescending(n => n.LanguageCode == "eng" || n.LanguageCode == "en").ThenBy(n => n.Id).Select(n => n.Name).FirstOrDefault(), x.ScientificName, x.AnimalGroupId, x.AnimalGroup.Name, x.PreferredVariantType)).ToListAsync(ct);
        return Ok(new CatalogPageDto<SpeciesDto>(items.Take(request.PageSize).ToList(), request.Page, request.PageSize, items.Count > request.PageSize));
    }

    [HttpGet("api/animal-variants")]
    public async Task<ActionResult<CatalogPageDto<AnimalVariantDto>>> Variants([FromQuery] CatalogSearchRequestDto request, CancellationToken ct)
    {
        if (!Valid(request)) return ValidationProblem(statusCode: 400, modelStateDictionary: ModelState);
        if (request.SpeciesId is null) return BadRequest("SpeciesId is required for variant search.");
        var query = db.AnimalVariants.AsNoTracking().Where(x => x.IsActive && x.Species.IsActive && x.Species.AnimalGroup.IsActive);
        if (request.SpeciesId is {} species) query = query.Where(x => x.SpeciesId == species);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = "%" + Escape(Normalize(request.Search)) + "%";
            query = query.Where(x => EF.Functions.ILike(x.Name, pattern, "\\"));
        }
        var items = await query.OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize + 1)
            .Select(x => new AnimalVariantDto(x.Id, x.SpeciesId, x.Name, x.VariantType)).ToListAsync(ct);
        return Ok(new CatalogPageDto<AnimalVariantDto>(items.Take(request.PageSize).ToList(), request.Page, request.PageSize, items.Count > request.PageSize));
    }

    private bool Valid(CatalogSearchRequestDto request)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), errors, true);
        foreach (var error in errors) ModelState.AddModelError(error.MemberNames.FirstOrDefault() ?? "pagination", error.ErrorMessage!);
        return ModelState.IsValid;
    }
    private static string Normalize(string? value) => Regex.Replace(value?.Trim() ?? "", @"\s+", " ");
    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
