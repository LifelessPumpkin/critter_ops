using Microsoft.EntityFrameworkCore;
using skipper_api.Data;
using skipper_api.Domain.Activity;
using skipper_api.Dtos.EnclosureCleanings;

namespace skipper_api.Services.EnclosureCleanings;

public class EnclosureCleaningActivityService : IEnclosureCleaningActivityService
{
    private readonly ProfessorDbContext _dbContext;

    public EnclosureCleaningActivityService(ProfessorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<EnclosureCleaningActivityDto>?> GetByEnclosureIdAsync(
        int enclosureId,
        CancellationToken cancellationToken = default)
    {
        var enclosureExists = await _dbContext.Enclosures
            .AsNoTracking()
            .AnyAsync(enclosure => enclosure.Id == enclosureId, cancellationToken);

        if (!enclosureExists)
        {
            return null;
        }

        var cleaningEvents = await CleaningEvents()
            .Where(activityEvent => activityEvent.Enclosures.Any(association => association.EnclosureId == enclosureId))
            .OrderByDescending(activityEvent => activityEvent.OccurredAt)
            .ThenByDescending(activityEvent => activityEvent.Id)
            .ToListAsync(cancellationToken);

        return cleaningEvents.Select(activityEvent => ToDto(activityEvent, enclosureId)).ToList();
    }

    public async Task<EnclosureCleaningActivityDto?> GetByIdAsync(
        int enclosureId,
        long activityId,
        CancellationToken cancellationToken = default)
    {
        var cleaningEvent = await CleaningEvents()
            .Where(activityEvent =>
                activityEvent.Id == activityId &&
                activityEvent.Enclosures.Any(association => association.EnclosureId == enclosureId))
            .SingleOrDefaultAsync(cancellationToken);

        return cleaningEvent is null
            ? null
            : ToDto(cleaningEvent, enclosureId);
    }

    public async Task<CreateEnclosureCleaningActivityResult> CreateAsync(
        int enclosureId,
        CreateEnclosureCleaningActivityDto request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var enclosure = await _dbContext.Enclosures
            .SingleOrDefaultAsync(enclosure => enclosure.Id == enclosureId, cancellationToken);

        if (enclosure is null)
        {
            return CreateEnclosureCleaningActivityResult.EnclosureNotFound();
        }

        var now = DateTime.UtcNow;
        var cleaning = new EnclosureCleaningActivity
        {
            CleaningType = request.CleaningType!.Value,
            WaterChangePercent = request.WaterChangePercent,
            SubstrateChanged = request.SubstrateChanged,
            EquipmentCleaned = request.EquipmentCleaned,
        };
        var activityEvent = new ActivityEvent
        {
            EventType = request.CleaningType == EnclosureCleaningType.WaterChange
                ? ActivityEventType.WaterChange
                : ActivityEventType.Cleaning,
            OccurredAt = request.OccurredAt!.Value,
            Title = ToTimelineTitle(cleaning),
            Notes = request.Notes,
            PerformedBy = request.PerformedBy,
            CreatedAt = now,
            UpdatedAt = now,
            Enclosures =
            [
                new ActivityEventEnclosure
                {
                    EnclosureId = enclosureId,
                    RelationshipType = ActivityEventEnclosureRelationshipType.Primary,
                },
            ],
            EnclosureCleaning = request.CleaningType == EnclosureCleaningType.WaterChange
                ? null
                : cleaning,
            EnclosureWaterChange = request.CleaningType == EnclosureCleaningType.WaterChange
                ? new EnclosureWaterChangeActivity
                {
                    WaterChangePercent = request.WaterChangePercent,
                }
                : null,
        };

        _dbContext.ActivityEvents.Add(activityEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return CreateEnclosureCleaningActivityResult.Created(ToDto(
            activityEvent,
            enclosureId,
            enclosure.Name));
    }

    public async Task<UpdateEnclosureCleaningActivityResult> UpdateAsync(
        int enclosureId,
        long activityId,
        UpdateEnclosureCleaningActivityDto request,
        CancellationToken cancellationToken = default)
    {
        var cleaningEvent = await _dbContext.ActivityEvents
            .Include(activityEvent => activityEvent.EnclosureCleaning)
            .Include(activityEvent => activityEvent.EnclosureWaterChange)
            .Include(activityEvent => activityEvent.Enclosures)
                .ThenInclude(association => association.Enclosure)
            .SingleOrDefaultAsync(
                activityEvent =>
                    activityEvent.Id == activityId &&
                    (activityEvent.EventType == ActivityEventType.Cleaning ||
                        activityEvent.EventType == ActivityEventType.WaterChange) &&
                    activityEvent.Enclosures.Any(association => association.EnclosureId == enclosureId),
                cancellationToken);

        if (cleaningEvent is null ||
            (cleaningEvent.EnclosureCleaning is null && cleaningEvent.EnclosureWaterChange is null))
        {
            return UpdateEnclosureCleaningActivityResult.NotFound();
        }

        var cleaning = new EnclosureCleaningActivity
        {
            CleaningType = request.CleaningType!.Value,
            WaterChangePercent = request.WaterChangePercent,
            SubstrateChanged = request.SubstrateChanged,
            EquipmentCleaned = request.EquipmentCleaned,
        };
        cleaningEvent.EventType = request.CleaningType == EnclosureCleaningType.WaterChange
            ? ActivityEventType.WaterChange
            : ActivityEventType.Cleaning;
        cleaningEvent.EnclosureCleaning = request.CleaningType == EnclosureCleaningType.WaterChange
            ? null
            : cleaning;
        cleaningEvent.EnclosureWaterChange = request.CleaningType == EnclosureCleaningType.WaterChange
            ? new EnclosureWaterChangeActivity
            {
                WaterChangePercent = request.WaterChangePercent,
            }
            : null;
        cleaningEvent.OccurredAt = request.OccurredAt!.Value;
        cleaningEvent.Title = ToTimelineTitle(cleaning);
        cleaningEvent.Notes = request.Notes;
        cleaningEvent.PerformedBy = request.PerformedBy;
        cleaningEvent.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return UpdateEnclosureCleaningActivityResult.Updated(ToDto(cleaningEvent, enclosureId));
    }

    public async Task<DeleteEnclosureCleaningActivityResult> DeleteAsync(
        int enclosureId,
        long activityId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var cleaningEvent = await _dbContext.ActivityEvents
            .Include(activityEvent => activityEvent.EnclosureCleaning)
            .Include(activityEvent => activityEvent.EnclosureWaterChange)
            .Include(activityEvent => activityEvent.Enclosures)
            .SingleOrDefaultAsync(
                activityEvent =>
                    activityEvent.Id == activityId &&
                    (activityEvent.EventType == ActivityEventType.Cleaning ||
                        activityEvent.EventType == ActivityEventType.WaterChange) &&
                    activityEvent.Enclosures.Any(association => association.EnclosureId == enclosureId),
                cancellationToken);

        if (cleaningEvent is null ||
            (cleaningEvent.EnclosureCleaning is null && cleaningEvent.EnclosureWaterChange is null))
        {
            return DeleteEnclosureCleaningActivityResult.NotFound;
        }

        _dbContext.ActivityEvents.Remove(cleaningEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return DeleteEnclosureCleaningActivityResult.Deleted;
    }

    private IQueryable<ActivityEvent> CleaningEvents()
    {
        return _dbContext.ActivityEvents
            .AsNoTracking()
            .Include(activityEvent => activityEvent.EnclosureCleaning)
            .Include(activityEvent => activityEvent.EnclosureWaterChange)
            .Include(activityEvent => activityEvent.Enclosures)
                .ThenInclude(association => association.Enclosure)
            .Where(activityEvent =>
                (activityEvent.EventType == ActivityEventType.Cleaning && activityEvent.EnclosureCleaning != null) ||
                (activityEvent.EventType == ActivityEventType.WaterChange && activityEvent.EnclosureWaterChange != null));
    }

    private static EnclosureCleaningActivityDto ToDto(ActivityEvent activityEvent, int enclosureId)
    {
        var enclosureAssociation = activityEvent.Enclosures.Single(association => association.EnclosureId == enclosureId);

        return ToDto(
            activityEvent,
            enclosureId,
            enclosureAssociation.Enclosure.Name);
    }

    private static EnclosureCleaningActivityDto ToDto(
        ActivityEvent activityEvent,
        int enclosureId,
        string enclosureName)
    {
        var cleaning = activityEvent.EnclosureCleaning;
        var waterChange = activityEvent.EnclosureWaterChange;

        return new EnclosureCleaningActivityDto
        {
            ActivityEventId = activityEvent.Id,
            EnclosureId = enclosureId,
            EnclosureName = enclosureName,
            CleaningType = cleaning?.CleaningType ?? EnclosureCleaningType.WaterChange,
            WaterChangePercent = cleaning?.WaterChangePercent ?? waterChange?.WaterChangePercent,
            SubstrateChanged = cleaning?.SubstrateChanged ?? false,
            EquipmentCleaned = cleaning?.EquipmentCleaned,
            OccurredAt = activityEvent.OccurredAt,
            Notes = activityEvent.Notes,
            PerformedBy = activityEvent.PerformedBy,
            CreatedAt = activityEvent.CreatedAt,
            UpdatedAt = activityEvent.UpdatedAt,
        };
    }

    public static string ToTimelineTitle(EnclosureCleaningActivity cleaning)
    {
        var title = cleaning.CleaningType == EnclosureCleaningType.WaterChange &&
            cleaning.WaterChangePercent.HasValue
            ? $"{FormatPercent(cleaning.WaterChangePercent.Value)}% water change completed"
            : cleaning.CleaningType switch
            {
                EnclosureCleaningType.SpotClean => "Spot cleaning completed",
                EnclosureCleaningType.PartialClean => "Partial cleaning completed",
                EnclosureCleaningType.FullClean => "Full cleaning completed",
                EnclosureCleaningType.DeepClean => "Deep cleaning completed",
                EnclosureCleaningType.Disinfection => "Disinfection completed",
                EnclosureCleaningType.WaterChange => "Water change completed",
                EnclosureCleaningType.SubstrateChange => "Substrate change completed",
                EnclosureCleaningType.Other => "Cleaning completed",
                _ => "Cleaning completed",
            };

        return cleaning.SubstrateChanged &&
            cleaning.CleaningType != EnclosureCleaningType.SubstrateChange
            ? $"{title} - substrate replaced"
            : title;
    }

    private static string FormatPercent(decimal percent)
    {
        return percent % 1 == 0
            ? decimal.Truncate(percent).ToString("0")
            : percent.ToString("0.##");
    }
}
