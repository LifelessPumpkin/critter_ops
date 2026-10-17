using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using skipper_api.Data;
using skipper_api.Domain.Animals;
using skipper_api.Dtos.Animals;

namespace skipper_api.Services.Animals;

public class AnimalService : IAnimalService
{
    private const int MaximumPageSize = 100;
    private readonly ProfessorDbContext _dbContext;

    public AnimalService(ProfessorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AnimalResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Animals
            .AsNoTracking()
            .OrderBy(animal => animal.Name)
            .Select(ToResponseDtoProjection)
            .ToListAsync(cancellationToken);
    }

    public async Task<AnimalSearchResponseDto> SearchAsync(
        AnimalSearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, MaximumPageSize);
        var filteredQuery = ApplyNonLifecycleFilters(_dbContext.Animals.AsNoTracking(), request);
        var inCareStatuses = AnimalLifecycleRules.InCareStatuses;
        var outOfCareStatuses = AnimalLifecycleRules.OutOfCareStatuses;

        // A DbContext cannot execute concurrent operations. Await each aggregate while
        // keeping them server-side and scoped to the complete filtered query.
        var allCount = await filteredQuery.CountAsync(cancellationToken);
        var inCareCount = await filteredQuery.CountAsync(
            animal => inCareStatuses.Contains(animal.Status), cancellationToken);
        var outOfCareCount = await filteredQuery.CountAsync(
            animal => outOfCareStatuses.Contains(animal.Status), cancellationToken);

        var lifecycleQuery = request.Lifecycle switch
        {
            AnimalLifecycle.InCare => filteredQuery.Where(animal => inCareStatuses.Contains(animal.Status)),
            AnimalLifecycle.OutOfCare => filteredQuery.Where(animal => outOfCareStatuses.Contains(animal.Status)),
            _ => filteredQuery,
        };
        var totalCount = request.Lifecycle switch
        {
            AnimalLifecycle.InCare => inCareCount,
            AnimalLifecycle.OutOfCare => outOfCareCount,
            _ => allCount,
        };

        var items = await lifecycleQuery
            .OrderBy(animal => animal.Name)
            .ThenBy(animal => animal.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(animal => new AnimalListItemDto
            {
                Id = animal.Id,
                EnclosureId = animal.EnclosureId,
                EnclosureName = animal.Enclosure.Name,
                EnclosureLocation = animal.Enclosure.Location,
                Name = animal.Name,
                Species = animal.CatalogSpecies != null ? animal.CatalogSpecies.CommonName ?? animal.CatalogSpecies.ScientificName : animal.Species,
            AnimalGroupId = animal.AnimalGroupId,
            AnimalGroupName = animal.AnimalGroup != null ? animal.AnimalGroup.Name : animal.AnimalType.ToString(),
            SpeciesId = animal.SpeciesId,
            SpeciesScientificName = animal.CatalogSpecies != null ? animal.CatalogSpecies.ScientificName : null,
            AnimalVariantId = animal.AnimalVariantId,
            VariantType = animal.AnimalVariant != null ? animal.AnimalVariant.VariantType : null,
            PreferredVariantType = animal.CatalogSpecies != null ? animal.CatalogSpecies.PreferredVariantType : null,
                SubspeciesOrMorph = animal.AnimalVariant != null ? animal.AnimalVariant.Name : animal.SubspeciesOrMorph,
                AnimalType = animal.AnimalType,
                Status = animal.Status,
                Sex = animal.Sex,
                BirthDate = animal.BirthDate,
                BirthDateIsEstimated = animal.BirthDateIsEstimated,
            })
            .ToListAsync(cancellationToken);

        return new AnimalSearchResponseDto
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize),
            AllCount = allCount,
            InCareCount = inCareCount,
            OutOfCareCount = outOfCareCount,
            Items = items,
        };
    }

    private IQueryable<Animal> ApplyNonLifecycleFilters(
        IQueryable<Animal> query,
        AnimalSearchRequestDto request)
    {
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(animal => EF.Functions.ILike(animal.Name, $"%{search}%"));
        }

        if (request.Statuses.Count > 0)
        {
            query = query.Where(animal => request.Statuses.Contains(animal.Status));
        }

        if (request.AnimalTypes.Count > 0 || request.AnimalGroupIds.Count > 0)
        {
            var legacyGroupTypes = _dbContext.AnimalGroups.Where(group => request.AnimalGroupIds.Contains(group.Id))
                .Select(group => group.LegacyAnimalType);
            query = query.Where(animal => request.AnimalTypes.Contains(animal.AnimalType)
                || (animal.AnimalGroupId.HasValue && request.AnimalGroupIds.Contains(animal.AnimalGroupId.Value))
                || (!animal.AnimalGroupId.HasValue && legacyGroupTypes.Contains(animal.AnimalType)));
        }

        if (request.EnclosureId.HasValue)
        {
            query = query.Where(animal => animal.EnclosureId == request.EnclosureId.Value);
        }

        return query;
    }

    public async Task<AnimalResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Animals
            .AsNoTracking()
            .Where(animal => animal.Id == id)
            .Select(ToResponseDtoProjection)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CreateAnimalResult> CreateAsync(
        CreateAnimalRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.EnclosureId is not { } enclosureId)
        {
            return CreateAnimalResult.EnclosureNotFound();
        }

        var enclosureName = await _dbContext.Enclosures
            .Where(enclosure => enclosure.Id == enclosureId)
            .Select(enclosure => enclosure.Name)
            .SingleOrDefaultAsync(cancellationToken);

        if (enclosureName is null)
        {
            return CreateAnimalResult.EnclosureNotFound();
        }

        var reference = await ResolveReferences(request.AnimalGroupId, request.SpeciesId, request.AnimalVariantId,
            request.Species, request.SubspeciesOrMorph, request.AnimalType, cancellationToken);
        if (reference.Error is {} error) return CreateAnimalResult.InvalidReference(error);
        var now = DateTime.UtcNow;
        var animal = new Animal
        {
            EnclosureId = enclosureId,
            Enclosure = null!,
            Name = request.Name!,
            AnimalGroupId = reference.Group?.Id,
            SpeciesId = reference.Species?.Id,
            AnimalVariantId = reference.Variant?.Id,
            Species = reference.Species is {} species ? species.CommonName ?? species.ScientificName : request.Species!.Trim(),
            SubspeciesOrMorph = reference.Variant?.Name ?? request.SubspeciesOrMorph?.Trim(),
            AnimalType = reference.Group?.LegacyAnimalType ?? request.AnimalType ?? AnimalType.Other,
            Status = request.Status!.Value,
            Sex = request.Sex!.Value,
            BirthDate = request.BirthDate,
            BirthDateIsEstimated = request.BirthDateIsEstimated,
            AcquiredDate = request.AcquiredDate!.Value,
            DispositionDate = request.DispositionDate,
            DispositionReason = request.DispositionReason,
            MicrochipNumber = request.MicrochipNumber,
            TagIdentifier = request.TagIdentifier,
            Source = request.Source,
            Notes = request.Notes,
            CreatedAt = now,
            UpdatedAt = now,
        };

        animal.AnimalGroup = reference.Group;
        animal.CatalogSpecies = reference.Species;
        animal.AnimalVariant = reference.Variant;
        _dbContext.Animals.Add(animal);
        try { await _dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            return CreateAnimalResult.InvalidReference("A selected enclosure or catalog reference is no longer available. Refresh the selection and try again.");
        }

        return CreateAnimalResult.Created(ToResponseDto(animal, enclosureName));
    }

    public async Task<UpdateAnimalResult> UpdateAsync(
        int id,
        UpdateAnimalRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var animal = await _dbContext.Animals
            .SingleOrDefaultAsync(animal => animal.Id == id, cancellationToken);

        if (animal is null)
        {
            return UpdateAnimalResult.AnimalNotFound();
        }

        if (request.EnclosureId is not { } enclosureId)
        {
            return UpdateAnimalResult.EnclosureNotFound();
        }

        var enclosureName = await _dbContext.Enclosures
            .Where(enclosure => enclosure.Id == enclosureId)
            .Select(enclosure => enclosure.Name)
            .SingleOrDefaultAsync(cancellationToken);

        if (enclosureName is null)
        {
            return UpdateAnimalResult.EnclosureNotFound();
        }

        // Old consumers round-trip resolved text without IDs. Preserve references on unrelated edits.
        var existingSpeciesName = animal.SpeciesId.HasValue
            ? await _dbContext.Species.Where(x => x.Id == animal.SpeciesId).Select(x => x.CommonName ?? x.ScientificName).SingleAsync(cancellationToken)
            : animal.Species;
        var existingVariantName = animal.AnimalVariantId.HasValue
            ? await _dbContext.AnimalVariants.Where(x => x.Id == animal.AnimalVariantId).Select(x => x.Name).SingleAsync(cancellationToken)
            : animal.SubspeciesOrMorph;
        var speciesId = request.SpeciesIdSpecified ? request.SpeciesId : (request.Species == existingSpeciesName ? animal.SpeciesId : null);
        var variantId = request.AnimalVariantIdSpecified ? request.AnimalVariantId : (speciesId == animal.SpeciesId && request.SubspeciesOrMorph == existingVariantName ? animal.AnimalVariantId : null);
        var groupId = request.AnimalGroupIdSpecified ? request.AnimalGroupId : (request.AnimalType == animal.AnimalType ? animal.AnimalGroupId : null);
        var reference = await ResolveReferences(groupId, speciesId, variantId,
            request.Species, request.SubspeciesOrMorph, request.AnimalType, cancellationToken, animal);
        if (reference.Error is {} error) return UpdateAnimalResult.InvalidReference(error);
        animal.AnimalGroupId = reference.Group?.Id;
        animal.SpeciesId = reference.Species?.Id;
        animal.AnimalVariantId = reference.Variant?.Id;
        animal.EnclosureId = enclosureId;
        animal.Name = request.Name!;
        animal.Species = reference.Species is {} species ? species.CommonName ?? species.ScientificName : request.Species!.Trim();
        animal.SubspeciesOrMorph = reference.Variant?.Name ?? request.SubspeciesOrMorph?.Trim();
        animal.AnimalType = reference.Group?.LegacyAnimalType ?? request.AnimalType ?? animal.AnimalType;
        animal.Status = request.Status!.Value;
        animal.Sex = request.Sex!.Value;
        animal.BirthDate = request.BirthDate;
        animal.BirthDateIsEstimated = request.BirthDateIsEstimated;
        animal.AcquiredDate = request.AcquiredDate!.Value;
        animal.DispositionDate = request.DispositionDate;
        animal.DispositionReason = request.DispositionReason;
        animal.MicrochipNumber = request.MicrochipNumber;
        animal.TagIdentifier = request.TagIdentifier;
        animal.Source = request.Source;
        animal.Notes = request.Notes;
        animal.UpdatedAt = DateTime.UtcNow;
        animal.AnimalGroup = reference.Group;
        animal.CatalogSpecies = reference.Species;
        animal.AnimalVariant = reference.Variant;

        try { await _dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            return UpdateAnimalResult.InvalidReference("A selected enclosure or catalog reference is no longer available. Refresh the selection and try again.");
        }

        return UpdateAnimalResult.Updated(ToResponseDto(animal, enclosureName));
    }

    public async Task<DeleteAnimalResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var animal = await _dbContext.Animals
            .SingleOrDefaultAsync(animal => animal.Id == id, cancellationToken);

        if (animal is null)
        {
            return DeleteAnimalResult.NotFound;
        }

        _dbContext.Animals.Remove(animal);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return DeleteAnimalResult.Deleted;
    }

    private record References(AnimalGroup? Group = null, Species? Species = null, AnimalVariant? Variant = null, string? Error = null);

    private async Task<References> ResolveReferences(int? groupId, int? speciesId, int? variantId,
        string? customSpecies, string? customVariant, AnimalType? legacyType, CancellationToken ct, Animal? existing = null)
    {
        var species = speciesId.HasValue ? await _dbContext.Species.Include(x => x.AnimalGroup).SingleOrDefaultAsync(x => x.Id == speciesId, ct) : null;
        if (speciesId.HasValue && (species == null || ((!species.IsActive || !species.AnimalGroup.IsActive) && existing?.SpeciesId != speciesId)))
            return new(Error: "The selected species is unavailable.");
        groupId ??= species?.AnimalGroupId;
        var group = groupId.HasValue ? await _dbContext.AnimalGroups.SingleOrDefaultAsync(x => x.Id == groupId, ct) : null;
        if (groupId.HasValue && (group == null || (!group.IsActive && existing?.AnimalGroupId != groupId)))
            return new(Error: "The selected animal group is unavailable.");
        if (species != null && species.AnimalGroupId != groupId)
            return new(Error: "The species does not belong to the selected animal group.");
        var variant = variantId.HasValue ? await _dbContext.AnimalVariants.SingleOrDefaultAsync(x => x.Id == variantId, ct) : null;
        if (variantId.HasValue && (variant == null || (!variant.IsActive && existing?.AnimalVariantId != variantId)))
            return new(Error: "The selected variant is unavailable.");
        if (variant != null && (species == null || variant.SpeciesId != species.Id))
            return new(Error: "The variant does not belong to the selected species.");
        if (species == null && (string.IsNullOrWhiteSpace(customSpecies) || customSpecies.Trim().Length > 100))
            return new(Error: "Provide a species from the catalog or a custom species name (up to 100 characters).");
        if (variant == null && customVariant != null && (string.IsNullOrWhiteSpace(customVariant) || customVariant.Trim().Length > 100))
            return new(Error: "Custom variant must contain 1 to 100 characters, or be omitted.");
        if (legacyType.HasValue && !Enum.IsDefined(legacyType.Value)) return new(Error: "Invalid animal group.");
        return new(group, species, variant);
    }

    private static readonly Expression<Func<Animal, AnimalResponseDto>> ToResponseDtoProjection = animal =>
        new AnimalResponseDto
        {
            Id = animal.Id,
            EnclosureId = animal.EnclosureId,
            EnclosureName = animal.Enclosure.Name,
            Name = animal.Name,
            Species = animal.CatalogSpecies != null ? animal.CatalogSpecies.CommonName ?? animal.CatalogSpecies.ScientificName : animal.Species,
            AnimalGroupId = animal.AnimalGroupId,
            AnimalGroupName = animal.AnimalGroup != null ? animal.AnimalGroup.Name : animal.AnimalType.ToString(),
            SpeciesId = animal.SpeciesId,
            SpeciesScientificName = animal.CatalogSpecies != null ? animal.CatalogSpecies.ScientificName : null,
            AnimalVariantId = animal.AnimalVariantId,
            VariantType = animal.AnimalVariant != null ? animal.AnimalVariant.VariantType : null,
            PreferredVariantType = animal.CatalogSpecies != null ? animal.CatalogSpecies.PreferredVariantType : null,
            SubspeciesOrMorph = animal.AnimalVariant != null ? animal.AnimalVariant.Name : animal.SubspeciesOrMorph,
            AnimalType = animal.AnimalType,
            Status = animal.Status,
            Sex = animal.Sex,
            BirthDate = animal.BirthDate,
            BirthDateIsEstimated = animal.BirthDateIsEstimated,
            AcquiredDate = animal.AcquiredDate,
            DispositionDate = animal.DispositionDate,
            DispositionReason = animal.DispositionReason,
            MicrochipNumber = animal.MicrochipNumber,
            TagIdentifier = animal.TagIdentifier,
            Source = animal.Source,
            Notes = animal.Notes,
            CreatedAt = animal.CreatedAt,
            UpdatedAt = animal.UpdatedAt,
        };

    private static AnimalResponseDto ToResponseDto(Animal animal, string enclosureName)
    {
        return new AnimalResponseDto
        {
            Id = animal.Id,
            EnclosureId = animal.EnclosureId,
            EnclosureName = enclosureName,
            Name = animal.Name,
            Species = animal.CatalogSpecies != null ? animal.CatalogSpecies.CommonName ?? animal.CatalogSpecies.ScientificName : animal.Species,
            AnimalGroupId = animal.AnimalGroupId,
            AnimalGroupName = animal.AnimalGroup != null ? animal.AnimalGroup.Name : animal.AnimalType.ToString(),
            SpeciesId = animal.SpeciesId,
            SpeciesScientificName = animal.CatalogSpecies != null ? animal.CatalogSpecies.ScientificName : null,
            AnimalVariantId = animal.AnimalVariantId,
            VariantType = animal.AnimalVariant != null ? animal.AnimalVariant.VariantType : null,
            PreferredVariantType = animal.CatalogSpecies != null ? animal.CatalogSpecies.PreferredVariantType : null,
            SubspeciesOrMorph = animal.AnimalVariant != null ? animal.AnimalVariant.Name : animal.SubspeciesOrMorph,
            AnimalType = animal.AnimalType,
            Status = animal.Status,
            Sex = animal.Sex,
            BirthDate = animal.BirthDate,
            BirthDateIsEstimated = animal.BirthDateIsEstimated,
            AcquiredDate = animal.AcquiredDate,
            DispositionDate = animal.DispositionDate,
            DispositionReason = animal.DispositionReason,
            MicrochipNumber = animal.MicrochipNumber,
            TagIdentifier = animal.TagIdentifier,
            Source = animal.Source,
            Notes = animal.Notes,
            CreatedAt = animal.CreatedAt,
            UpdatedAt = animal.UpdatedAt,
        };
    }
}
