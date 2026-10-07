using Microsoft.EntityFrameworkCore;
using skipper_api.Data;
using skipper_api.Domain.Activity;
using skipper_api.Dtos.Activity;

namespace skipper_api.Services.Activity;

public class ActivitySearchService : IActivitySearchService
{
    private const int MaximumPageSize = 100;

    private readonly ProfessorDbContext _dbContext;

    public ActivitySearchService(ProfessorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ActivitySearchResponseDto> SearchAsync(
        ActivitySearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, 1, MaximumPageSize);
        var query = ApplyFilters(_dbContext.ActivityEvents.AsNoTracking(), request);
        var totalCount = await query.CountAsync(cancellationToken);
        var activityEvents = await query
            .AsSplitQuery()
            .Include(activityEvent => activityEvent.Animals)
                .ThenInclude(association => association.Animal)
            .Include(activityEvent => activityEvent.Enclosures)
                .ThenInclude(association => association.Enclosure)
            .Include(activityEvent => activityEvent.AnimalMovement)
                .ThenInclude(movement => movement!.FromEnclosure)
            .Include(activityEvent => activityEvent.AnimalMovement)
                .ThenInclude(movement => movement!.ToEnclosure)
            .Include(activityEvent => activityEvent.AnimalFeeding)
            .Include(activityEvent => activityEvent.AnimalDisposition)
            .Include(activityEvent => activityEvent.AnimalMedication)
            .Include(activityEvent => activityEvent.AnimalTreatment)
            .Include(activityEvent => activityEvent.EnclosureCleaning)
            .OrderByDescending(activityEvent => activityEvent.OccurredAt)
            .ThenByDescending(activityEvent => activityEvent.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new ActivitySearchResponseDto
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = activityEvents.Select(ToDto).ToList(),
        };
    }

    private static IQueryable<ActivityEvent> ApplyFilters(
        IQueryable<ActivityEvent> query,
        ActivitySearchRequestDto request)
    {
        if (request.EventType.HasValue)
        {
            query = query.Where(activityEvent => activityEvent.EventType == request.EventType.Value);
        }

        if (request.AnimalId.HasValue)
        {
            query = query.Where(activityEvent =>
                activityEvent.Animals.Any(association => association.AnimalId == request.AnimalId.Value));
        }

        if (request.EnclosureId.HasValue)
        {
            query = query.Where(activityEvent =>
                activityEvent.Enclosures.Any(association => association.EnclosureId == request.EnclosureId.Value));
        }

        if (!string.IsNullOrWhiteSpace(request.PerformedBy))
        {
            var performer = request.PerformedBy.Trim();
            query = query.Where(activityEvent =>
                activityEvent.PerformedBy != null &&
                EF.Functions.ILike(activityEvent.PerformedBy, $"%{performer}%"));
        }

        if (request.From.HasValue)
        {
            query = query.Where(activityEvent => activityEvent.OccurredAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(activityEvent => activityEvent.OccurredAt <= request.To.Value);
        }

        return query;
    }

    private static ActivitySearchResultDto ToDto(ActivityEvent activityEvent)
    {
        return new ActivitySearchResultDto
        {
            Id = activityEvent.Id,
            EventType = activityEvent.EventType,
            OccurredAt = activityEvent.OccurredAt,
            Title = activityEvent.Title,
            PerformedBy = activityEvent.PerformedBy,
            Notes = activityEvent.Notes,
            SourceReferenceId = activityEvent.SourceReferenceId,
            SourceType = activityEvent.SourceType,
            Metadata = activityEvent.Metadata?.RootElement.Clone(),
            CreatedAt = activityEvent.CreatedAt,
            UpdatedAt = activityEvent.UpdatedAt,
            Animals = activityEvent.Animals
                .OrderBy(association => association.RelationshipType == ActivityEventAnimalRelationshipType.Primary ? 0 : 1)
                .ThenBy(association => association.Animal.Name)
                .Select(association => new ActivitySearchAnimalDto
                {
                    AnimalId = association.AnimalId,
                    AnimalName = association.Animal.Name,
                    RelationshipType = association.RelationshipType,
                })
                .ToList(),
            Enclosures = activityEvent.Enclosures
                .OrderBy(association => association.RelationshipType == ActivityEventEnclosureRelationshipType.Primary ? 0 : 1)
                .ThenBy(association => association.Enclosure.Name)
                .Select(association => new ActivitySearchEnclosureDto
                {
                    EnclosureId = association.EnclosureId,
                    EnclosureName = association.Enclosure.Name,
                    RelationshipType = association.RelationshipType,
                })
                .ToList(),
            Details = ToDetailsDto(activityEvent),
        };
    }

    private static ActivitySearchDetailsDto ToDetailsDto(ActivityEvent activityEvent)
    {
        return new ActivitySearchDetailsDto
        {
            Movement = activityEvent.AnimalMovement is { } movement
                ? new ActivitySearchMovementDetailsDto
                {
                    FromEnclosureId = movement.FromEnclosureId,
                    FromEnclosureName = movement.FromEnclosure.Name,
                    ToEnclosureId = movement.ToEnclosureId,
                    ToEnclosureName = movement.ToEnclosure.Name,
                    Reason = movement.Reason,
                }
                : null,
            Feeding = activityEvent.AnimalFeeding is { } feeding
                ? new ActivitySearchFeedingDetailsDto
                {
                    Food = feeding.Food,
                    Quantity = feeding.Quantity,
                    Unit = feeding.Unit,
                    Result = feeding.Result,
                }
                : null,
            Disposition = activityEvent.AnimalDisposition is { } disposition
                ? new ActivitySearchDispositionDetailsDto
                {
                    DispositionType = disposition.DispositionType,
                    Reason = disposition.Reason,
                    RecipientOrDestination = disposition.RecipientOrDestination,
                }
                : null,
            Medication = activityEvent.AnimalMedication is { } medication
                ? new ActivitySearchMedicationDetailsDto
                {
                    MedicationName = medication.MedicationName,
                    Dose = medication.Dose,
                    DoseUnit = medication.DoseUnit,
                    Route = medication.Route,
                    Result = medication.Result,
                }
                : null,
            Treatment = activityEvent.AnimalTreatment is { } treatment
                ? new ActivitySearchTreatmentDetailsDto
                {
                    TreatmentType = treatment.TreatmentType,
                    TreatmentName = treatment.TreatmentName,
                    Result = treatment.Result,
                }
                : null,
            Cleaning = activityEvent.EnclosureCleaning is { } cleaning
                ? new ActivitySearchCleaningDetailsDto
                {
                    CleaningType = cleaning.CleaningType,
                    WaterChangePercent = cleaning.WaterChangePercent,
                    SubstrateChanged = cleaning.SubstrateChanged,
                    EquipmentCleaned = cleaning.EquipmentCleaned,
                }
                : null,
        };
    }
}
