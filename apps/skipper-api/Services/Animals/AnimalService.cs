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
                CustomSpeciesName = animal.SpeciesId == null ? animal.Species : null,
                CustomVariantName = animal.AnimalVariantId == null ? animal.SubspeciesOrMorph : null,
                OriginalSpeciesText = animal.Species,
                OriginalVariantText = animal.SubspeciesOrMorph,
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

        if (request.SpeciesId.HasValue && request.CustomSpeciesName != null || request.AnimalVariantId.HasValue && request.CustomVariantName != null)
            return CreateAnimalResult.InvalidReference("Choose either structured or custom references.");
        var reference = await ResolveReferences(request.AnimalGroupId, request.SpeciesId, request.AnimalVariantId,
            request.CustomSpeciesName, request.CustomVariantName, request.AnimalType, cancellationToken);
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
            Species = reference.Species != null ? "" : request.CustomSpeciesName!,
            SubspeciesOrMorph = reference.Variant != null ? null : request.CustomVariantName,
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

        var referencesSpecified = request.AnimalGroupIdSpecified || request.SpeciesIdSpecified || request.AnimalVariantIdSpecified
            || request.CustomSpeciesNameSpecified || request.CustomVariantNameSpecified || request.AnimalType.HasValue;
        if (referencesSpecified)
        {
            var speciesId = request.SpeciesIdSpecified ? request.SpeciesId : animal.SpeciesId;
            var variantId = request.AnimalVariantIdSpecified ? request.AnimalVariantId : animal.AnimalVariantId;
            if (speciesId.HasValue && request.CustomSpeciesName != null || variantId.HasValue && request.CustomVariantName != null)
                return UpdateAnimalResult.InvalidReference("Clear the structured reference before providing a custom value.");
            var groupId = request.AnimalGroupIdSpecified ? request.AnimalGroupId : animal.AnimalGroupId;
            var customSpecies = request.CustomSpeciesNameSpecified ? request.CustomSpeciesName : (speciesId == null ? animal.Species : null);
            var customVariant = request.CustomVariantNameSpecified ? request.CustomVariantName : (variantId == null ? animal.SubspeciesOrMorph : null);
            var reference = await ResolveReferences(groupId, speciesId, variantId,
                customSpecies, customVariant, request.AnimalType, cancellationToken, animal);
            if (reference.Error is {} error) return UpdateAnimalResult.InvalidReference(error);
            // An unchanged structured selection must not overwrite preserved historical text.
            if (speciesId != animal.SpeciesId || speciesId == null) animal.Species = speciesId != null ? "" : customSpecies!;
            if (variantId != animal.AnimalVariantId || variantId == null) animal.SubspeciesOrMorph = variantId != null ? null : customVariant;
            // A variant-only edit must not reclassify an ambiguous operational group.
            var resolvedGroupId = request.AnimalGroupIdSpecified || speciesId != animal.SpeciesId
                ? reference.Group?.Id : animal.AnimalGroupId;
            var groupChanged = animal.AnimalGroupId != resolvedGroupId;
            animal.AnimalGroupId = resolvedGroupId;
            animal.SpeciesId = reference.Species?.Id;
            animal.AnimalVariantId = reference.Variant?.Id;
            animal.AnimalType = request.AnimalType ?? (request.AnimalGroupIdSpecified && groupChanged ? reference.Group?.LegacyAnimalType : null) ?? animal.AnimalType;
        }
        animal.EnclosureId = enclosureId;
        animal.Name = request.Name!;
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

        try { await _dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            return UpdateAnimalResult.InvalidReference("A selected enclosure or catalog reference is no longer available. Refresh the selection and try again.");
        }

        return UpdateAnimalResult.Updated((await GetByIdAsync(animal.Id, cancellationToken))!);
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
        if (group != null && legacyType.HasValue && group.LegacyAnimalType != legacyType
            && !(existing?.AnimalGroupId == group.Id && existing.AnimalType == legacyType))
            return new(Error: "The animal group conflicts with the supplied operational type.");
        if (species != null && species.AnimalGroupId != groupId)
            return new(Error: "The species does not belong to the selected animal group.");
        var variant = variantId.HasValue ? await _dbContext.AnimalVariants.SingleOrDefaultAsync(x => x.Id == variantId, ct) : null;
        if (variantId.HasValue && (variant == null || (!variant.IsActive && existing?.AnimalVariantId != variantId)))
            return new(Error: "The selected variant is unavailable.");
        if (variant != null && (species == null || variant.SpeciesId != species.Id))
            return new(Error: "The variant does not belong to the selected species.");
        if (species == null && (string.IsNullOrWhiteSpace(customSpecies) || customSpecies.Length > 100)
            && !(existing != null && existing.SpeciesId == null && customSpecies == existing.Species))
            return new(Error: "Provide a species from the catalog or a custom species name (up to 100 characters).");
        if (variant == null && customVariant != null && (string.IsNullOrWhiteSpace(customVariant) || customVariant.Length > 100))
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
            CustomSpeciesName = animal.SpeciesId == null ? animal.Species : null,
            CustomVariantName = animal.AnimalVariantId == null ? animal.SubspeciesOrMorph : null,
            OriginalSpeciesText = animal.Species,
            OriginalVariantText = animal.SubspeciesOrMorph,
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
            CustomSpeciesName = animal.SpeciesId == null ? animal.Species : null,
            CustomVariantName = animal.AnimalVariantId == null ? animal.SubspeciesOrMorph : null,
            OriginalSpeciesText = animal.Species,
            OriginalVariantText = animal.SubspeciesOrMorph,
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
