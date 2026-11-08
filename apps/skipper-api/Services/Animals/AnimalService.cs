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
                VariantAssignments = animal.VariantAssignments.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).Select(x => new VariantAssignmentDto {
                    AssignmentId = x.Id, AnimalVariantId = x.AnimalVariantId,
                    SpeciesId = x.AnimalVariant != null ? x.AnimalVariant.SpeciesId : null,
                    DisplayName = x.AnimalVariant != null ? x.AnimalVariant.Name : x.CustomValue!,
                    CustomValue = x.CustomValue, IsCustom = x.AnimalVariantId == null,
                    VariantType = x.AnimalVariant != null ? x.AnimalVariant.VariantType : x.VariantType!.Value,
                    DisplayOrder = x.DisplayOrder
                }).ToList(),
                Id = animal.Id,
                EnclosureId = animal.EnclosureId,
                EnclosureName = animal.Enclosure.Name,
                EnclosureLocation = animal.Enclosure.Location,
                Name = animal.Name,
                Species = animal.CatalogSpecies != null ? animal.CatalogSpecies.CommonName ?? animal.CatalogSpecies.ScientificName : animal.Species,
                AnimalGroupId = animal.AnimalGroupId,
                AnimalGroupName = animal.AnimalGroup != null ? animal.AnimalGroup.Name : animal.AnimalType.ToString(),
                CustomSpeciesName = animal.SpeciesId == null ? animal.Species : null,
                CustomVariantName = animal.VariantAssignments.OrderBy(x => x.DisplayOrder).Select(x => x.CustomValue).FirstOrDefault(),
                OriginalSpeciesText = animal.Species,
                OriginalVariantText = animal.SubspeciesOrMorph,
                SpeciesId = animal.SpeciesId,
                SpeciesScientificName = animal.CatalogSpecies != null ? animal.CatalogSpecies.ScientificName : null,
                AnimalVariantId = animal.VariantAssignments.OrderBy(x => x.DisplayOrder).Select(x => x.AnimalVariantId).FirstOrDefault(),
                VariantType = animal.VariantAssignments.OrderBy(x => x.DisplayOrder).Select(x => x.AnimalVariant != null ? (VariantType?)x.AnimalVariant.VariantType : x.VariantType).FirstOrDefault(),
                PreferredVariantType = animal.CatalogSpecies != null ? animal.CatalogSpecies.PreferredVariantType : null,
                SubspeciesOrMorph = animal.VariantAssignments.OrderBy(x => x.DisplayOrder).Select(x => x.AnimalVariant != null ? x.AnimalVariant.Name : x.CustomValue).FirstOrDefault(),
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

        if (request.VariantAssignments != null && (request.AnimalVariantId != null || request.CustomVariantName != null))
            return CreateAnimalResult.InvalidReference("Use variantAssignments without legacy variant fields.");
        if (request.SpeciesId.HasValue && request.CustomSpeciesName != null || request.AnimalVariantId.HasValue && request.CustomVariantName != null)
            return CreateAnimalResult.InvalidReference("Choose either structured or custom references.");
        var reference = await ResolveReferences(request.AnimalGroupId, request.SpeciesId, request.AnimalVariantId,
            request.CustomSpeciesName, request.CustomVariantName, request.AnimalType, cancellationToken);
        if (reference.Error is {} error) return CreateAnimalResult.InvalidReference(error);
        var assignments = await ResolveAssignments(request.VariantAssignments ?? LegacyAssignments(request.AnimalVariantId, request.CustomVariantName), reference.Species, null, cancellationToken);
        if (assignments.Error != null) return CreateAnimalResult.InvalidReference(assignments.Error);
        var now = DateTime.UtcNow;
        var animal = new Animal
        {
            EnclosureId = enclosureId,
            Enclosure = null!,
            Name = request.Name!,
            AnimalGroupId = reference.Group?.Id,
            SpeciesId = reference.Species?.Id,
            AnimalVariantId = null,
            Species = reference.Species != null ? "" : request.CustomSpeciesName!,
            SubspeciesOrMorph = null,
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

        ApplyAssignments(animal, assignments.Items!);
        _dbContext.Animals.Add(animal);
        try { await _dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation or PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.CheckViolation })
        {
            return CreateAnimalResult.InvalidReference("The selection conflicts with current database values (duplicate assignment or unavailable reference). Refresh and try again.");
        }

        return CreateAnimalResult.Created(ToResponseDto(animal, enclosureName));
    }

    public async Task<UpdateAnimalResult> UpdateAsync(
        int id,
        UpdateAnimalRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var animal = await _dbContext.Animals.Include(x => x.VariantAssignments).ThenInclude(x => x.AnimalVariant)
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

        var speciesId = request.SpeciesIdSpecified ? request.SpeciesId : animal.SpeciesId;
        var groupId = request.AnimalGroupIdSpecified ? request.AnimalGroupId : animal.AnimalGroupId;
        var customSpecies = request.CustomSpeciesNameSpecified ? request.CustomSpeciesName : (speciesId == null ? animal.Species : null);
        if (speciesId.HasValue && request.CustomSpeciesName != null)
            return UpdateAnimalResult.InvalidReference("Clear the structured species before providing a custom value.");
        var reference = await ResolveReferences(groupId, speciesId, null, customSpecies, null, request.AnimalType, cancellationToken, animal);
        if (reference.Error != null) return UpdateAnimalResult.InvalidReference(reference.Error);
        if (request.VariantAssignments != null && (request.AnimalVariantIdSpecified || request.CustomVariantNameSpecified))
            return UpdateAnimalResult.InvalidReference("Use variantAssignments without legacy variant fields.");
        var supplied = request.VariantAssignments ?? (request.AnimalVariantIdSpecified || request.CustomVariantNameSpecified
            ? LegacyAssignments(request.AnimalVariantId, request.CustomVariantName)
            : animal.VariantAssignments.Select(x => new VariantAssignmentRequestDto { AssignmentId = x.Id, AnimalVariantId = x.AnimalVariantId, CustomValue = x.CustomValue, VariantType = x.VariantType }).ToList());
        var assignments = await ResolveAssignments(supplied, reference.Species, animal, cancellationToken);
        if (assignments.Error != null) return UpdateAnimalResult.InvalidReference(assignments.Error);
        // All validation precedes mutations; SaveChanges commits the aggregate atomically.
        if (speciesId != animal.SpeciesId || speciesId == null) animal.Species = speciesId != null ? "" : customSpecies!;
        var resolvedGroupId = request.AnimalGroupIdSpecified || speciesId != animal.SpeciesId ? reference.Group?.Id : animal.AnimalGroupId;
        var groupChanged = animal.AnimalGroupId != resolvedGroupId;
        animal.AnimalGroupId = resolvedGroupId;
        animal.SpeciesId = speciesId;
        animal.AnimalType = request.AnimalType ?? (request.AnimalGroupIdSpecified && groupChanged ? reference.Group?.LegacyAnimalType : null) ?? animal.AnimalType;
        ApplyAssignments(animal, assignments.Items!);
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
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation or PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.CheckViolation })
        {
            return UpdateAnimalResult.InvalidReference("The selection conflicts with current database values (duplicate assignment or unavailable reference). Refresh and try again.");
        }

        return UpdateAnimalResult.Updated((await GetByIdAsync(animal.Id, cancellationToken))!);
    }

    public async Task<DeleteAnimalResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var animal = await _dbContext.Animals.Include(x => x.VariantAssignments).ThenInclude(x => x.AnimalVariant)
            .SingleOrDefaultAsync(animal => animal.Id == id, cancellationToken);

        if (animal is null)
        {
            return DeleteAnimalResult.NotFound;
        }

        _dbContext.Animals.Remove(animal);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return DeleteAnimalResult.Deleted;
    }

    private static List<VariantAssignmentRequestDto> LegacyAssignments(int? id, string? custom) =>
        id != null || custom != null ? [new() { AnimalVariantId = id, CustomValue = custom }] : [];

    private static string Normalize(string value) => System.Text.RegularExpressions.Regex.Replace(value.Trim(), @"\s+", " ").ToLowerInvariant();

    private async Task<(List<AnimalVariantAssignment>? Items, string? Error)> ResolveAssignments(
        List<VariantAssignmentRequestDto> requests, Species? species, Animal? existing, CancellationToken ct)
    {
        var ids = requests.Where(x => x != null && x.AnimalVariantId.HasValue).Select(x => x.AnimalVariantId!.Value).Distinct().ToList();
        var catalog = await _dbContext.AnimalVariants.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var result = new List<AnimalVariantAssignment>();
        var keys = new HashSet<string>();
        var assignmentIds = new HashSet<int>();
        foreach (var request in requests)
        {
            if (request == null) return (null, "Assignments cannot contain null entries.");
            if (request.AssignmentId is {} id && (id <= 0 || !assignmentIds.Add(id) || existing == null || !existing.VariantAssignments.Any(x => x.Id == id)))
                return (null, "Assignment ID is duplicated or does not belong to this animal.");
            if (request.AnimalVariantId.HasValue == (request.CustomValue != null))
                return (null, "Each assignment requires exactly one catalog variant or custom value.");
            AnimalVariant? variant = null;
            var type = request.VariantType ?? species?.PreferredVariantType ?? VariantType.Other;
            if (request.AnimalVariantId is {} variantId)
            {
                if (!catalog.TryGetValue(variantId, out variant) || (!variant.IsActive && !(existing?.VariantAssignments.Any(x => x.AnimalVariantId == variantId) ?? false)))
                    return (null, $"Variant {variantId} is unavailable for new selection.");
                if (species == null || variant.SpeciesId != species.Id)
                    return (null, $"Variant '{variant.Name}' ({variantId}) is incompatible with the selected species. Explicitly remove or replace it.");
                if (request.VariantType != null && request.VariantType != variant.VariantType)
                    return (null, "Structured variant type must match the catalog.");
            }
            else if (string.IsNullOrWhiteSpace(request.CustomValue) || request.CustomValue.Length > 100 || !Enum.IsDefined(type))
                return (null, "Custom variants require 1–100 characters and a valid variant type.");
            var key = variant != null ? $"id:{variant.Id}" : $"custom:{type}:{Normalize(request.CustomValue!)}";
            if (!keys.Add(key)) return (null, "Duplicate variant assignments are not allowed (custom values ignore case and repeated whitespace within a type).");
            var retained = request.AssignmentId.HasValue ? existing?.VariantAssignments.Single(x => x.Id == request.AssignmentId)
                : existing?.VariantAssignments.FirstOrDefault(x => variant != null ? x.AnimalVariantId == variant.Id : x.AnimalVariantId == null && x.VariantType == type && Normalize(x.CustomValue!) == Normalize(request.CustomValue!));
            if (retained != null && result.Any(x => x.Id == retained.Id))
                return (null, "An existing assignment cannot be used twice in a replacement collection.");
            result.Add(new() { Id = retained?.Id ?? 0, AnimalVariantId = variant?.Id, AnimalVariant = variant,
                CustomValue = request.CustomValue, VariantType = variant != null ? null : type, DisplayOrder = result.Count,
                CreatedAt = retained?.CreatedAt ?? DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        }
        return (result, null);
    }

    private void ApplyAssignments(Animal animal, List<AnimalVariantAssignment> desired)
    {
        foreach (var removed in animal.VariantAssignments.Where(x => !desired.Any(y => y.Id != 0 && y.Id == x.Id)).ToList())
        {
            animal.VariantAssignments.Remove(removed);
            _dbContext.AnimalVariantAssignments.Remove(removed);
        }
        foreach (var value in desired)
        {
            var current = value.Id == 0 ? null : animal.VariantAssignments.Single(x => x.Id == value.Id);
            if (current == null) animal.VariantAssignments.Add(value);
            else {
                current.AnimalVariantId = value.AnimalVariantId; current.AnimalVariant = value.AnimalVariant;
                current.CustomValue = value.CustomValue; current.VariantType = value.VariantType;
                current.DisplayOrder = value.DisplayOrder; current.UpdatedAt = value.UpdatedAt;
            }
        }
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
            VariantAssignments = animal.VariantAssignments.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).Select(x => new VariantAssignmentDto {
                    AssignmentId = x.Id, AnimalVariantId = x.AnimalVariantId,
                    SpeciesId = x.AnimalVariant != null ? x.AnimalVariant.SpeciesId : null,
                    DisplayName = x.AnimalVariant != null ? x.AnimalVariant.Name : x.CustomValue!,
                    CustomValue = x.CustomValue, IsCustom = x.AnimalVariantId == null,
                    VariantType = x.AnimalVariant != null ? x.AnimalVariant.VariantType : x.VariantType!.Value,
                    DisplayOrder = x.DisplayOrder
                }).ToList(),
                Id = animal.Id,
            EnclosureId = animal.EnclosureId,
            EnclosureName = animal.Enclosure.Name,
            Name = animal.Name,
            Species = animal.CatalogSpecies != null ? animal.CatalogSpecies.CommonName ?? animal.CatalogSpecies.ScientificName : animal.Species,
            AnimalGroupId = animal.AnimalGroupId,
            AnimalGroupName = animal.AnimalGroup != null ? animal.AnimalGroup.Name : animal.AnimalType.ToString(),
            CustomSpeciesName = animal.SpeciesId == null ? animal.Species : null,
            CustomVariantName = animal.VariantAssignments.OrderBy(x => x.DisplayOrder).Select(x => x.CustomValue).FirstOrDefault(),
            OriginalSpeciesText = animal.Species,
            OriginalVariantText = animal.SubspeciesOrMorph,
            SpeciesId = animal.SpeciesId,
            SpeciesScientificName = animal.CatalogSpecies != null ? animal.CatalogSpecies.ScientificName : null,
            AnimalVariantId = animal.VariantAssignments.OrderBy(x => x.DisplayOrder).Select(x => x.AnimalVariantId).FirstOrDefault(),
            VariantType = animal.VariantAssignments.OrderBy(x => x.DisplayOrder).Select(x => x.AnimalVariant != null ? (VariantType?)x.AnimalVariant.VariantType : x.VariantType).FirstOrDefault(),
            PreferredVariantType = animal.CatalogSpecies != null ? animal.CatalogSpecies.PreferredVariantType : null,
            SubspeciesOrMorph = animal.VariantAssignments.OrderBy(x => x.DisplayOrder).Select(x => x.AnimalVariant != null ? x.AnimalVariant.Name : x.CustomValue).FirstOrDefault(),
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
            VariantAssignments = animal.VariantAssignments.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).Select(x => new VariantAssignmentDto {
                    AssignmentId = x.Id, AnimalVariantId = x.AnimalVariantId,
                    SpeciesId = x.AnimalVariant != null ? x.AnimalVariant.SpeciesId : null,
                    DisplayName = x.AnimalVariant != null ? x.AnimalVariant.Name : x.CustomValue!,
                    CustomValue = x.CustomValue, IsCustom = x.AnimalVariantId == null,
                    VariantType = x.AnimalVariant != null ? x.AnimalVariant.VariantType : x.VariantType!.Value,
                    DisplayOrder = x.DisplayOrder
                }).ToList(),
                Id = animal.Id,
            EnclosureId = animal.EnclosureId,
            EnclosureName = enclosureName,
            Name = animal.Name,
            Species = animal.CatalogSpecies != null ? animal.CatalogSpecies.CommonName ?? animal.CatalogSpecies.ScientificName : animal.Species,
            AnimalGroupId = animal.AnimalGroupId,
            AnimalGroupName = animal.AnimalGroup != null ? animal.AnimalGroup.Name : animal.AnimalType.ToString(),
            CustomSpeciesName = animal.SpeciesId == null ? animal.Species : null,
            CustomVariantName = animal.VariantAssignments.OrderBy(x => x.DisplayOrder).Select(x => x.CustomValue).FirstOrDefault(),
            OriginalSpeciesText = animal.Species,
            OriginalVariantText = animal.SubspeciesOrMorph,
            SpeciesId = animal.SpeciesId,
            SpeciesScientificName = animal.CatalogSpecies != null ? animal.CatalogSpecies.ScientificName : null,
            AnimalVariantId = animal.VariantAssignments.OrderBy(x => x.DisplayOrder).Select(x => x.AnimalVariantId).FirstOrDefault(),
            VariantType = animal.VariantAssignments.OrderBy(x => x.DisplayOrder).Select(x => x.AnimalVariant != null ? (VariantType?)x.AnimalVariant.VariantType : x.VariantType).FirstOrDefault(),
            PreferredVariantType = animal.CatalogSpecies != null ? animal.CatalogSpecies.PreferredVariantType : null,
            SubspeciesOrMorph = animal.VariantAssignments.OrderBy(x => x.DisplayOrder).Select(x => x.AnimalVariant != null ? x.AnimalVariant.Name : x.CustomValue).FirstOrDefault(),
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
