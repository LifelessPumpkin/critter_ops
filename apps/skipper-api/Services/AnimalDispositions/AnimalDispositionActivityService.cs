using Microsoft.EntityFrameworkCore;
using skipper_api.Data;
using skipper_api.Domain.Activity;
using skipper_api.Domain.Animals;
using skipper_api.Dtos.AnimalDispositions;

namespace skipper_api.Services.AnimalDispositions;

public class AnimalDispositionActivityService : IAnimalDispositionActivityService
{
    private readonly ProfessorDbContext _dbContext;

    public AnimalDispositionActivityService(ProfessorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AnimalDispositionActivityDto>?> GetByAnimalIdAsync(
        int animalId,
        CancellationToken cancellationToken = default)
    {
        var animalExists = await _dbContext.Animals
            .AsNoTracking()
            .AnyAsync(animal => animal.Id == animalId, cancellationToken);

        if (!animalExists)
        {
            return null;
        }

        var dispositionEvents = await DispositionEvents()
            .Where(activityEvent => activityEvent.Animals.Any(association => association.AnimalId == animalId))
            .OrderByDescending(activityEvent => activityEvent.OccurredAt)
            .ThenByDescending(activityEvent => activityEvent.Id)
            .ToListAsync(cancellationToken);

        return dispositionEvents.Select(activityEvent => ToDto(activityEvent, animalId)).ToList();
    }

    public async Task<AnimalDispositionActivityDto?> GetByIdAsync(
        int animalId,
        long activityId,
        CancellationToken cancellationToken = default)
    {
        var dispositionEvent = await DispositionEvents()
            .Where(activityEvent =>
                activityEvent.Id == activityId &&
                activityEvent.Animals.Any(association => association.AnimalId == animalId))
            .SingleOrDefaultAsync(cancellationToken);

        return dispositionEvent is null
            ? null
            : ToDto(dispositionEvent, animalId);
    }

    public async Task<CreateAnimalDispositionActivityResult> CreateAsync(
        int animalId,
        CreateAnimalDispositionActivityDto request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var animal = await _dbContext.Animals
            .Include(animal => animal.Enclosure)
            .SingleOrDefaultAsync(animal => animal.Id == animalId, cancellationToken);

        if (animal is null)
        {
            return CreateAnimalDispositionActivityResult.AnimalNotFound();
        }

        if (animal.Enclosure is null)
        {
            return CreateAnimalDispositionActivityResult.EnclosureNotFound();
        }

        if (OccursBeforeAcquisition(request.OccurredAt!.Value, animal.AcquiredDate))
        {
            return CreateAnimalDispositionActivityResult.OccurredBeforeAcquisition();
        }

        if (IsTerminalStatus(animal.Status))
        {
            return CreateAnimalDispositionActivityResult.AnimalAlreadyDisposed();
        }

        var dispositionType = request.DispositionType!.Value;
        var animalStatus = ToAnimalStatus(dispositionType);
        var now = DateTime.UtcNow;
        var activityEvent = new ActivityEvent
        {
            EventType = ActivityEventType.AnimalDisposition,
            OccurredAt = request.OccurredAt.Value,
            Title = ToAnimalTimelineTitle(dispositionType, request.RecipientOrDestination),
            Notes = request.Notes,
            PerformedBy = request.PerformedBy,
            CreatedAt = now,
            UpdatedAt = now,
            Animals =
            [
                new ActivityEventAnimal
                {
                    AnimalId = animalId,
                    RelationshipType = ActivityEventAnimalRelationshipType.Primary,
                },
            ],
            Enclosures =
            [
                new ActivityEventEnclosure
                {
                    EnclosureId = animal.EnclosureId,
                    RelationshipType = ActivityEventEnclosureRelationshipType.Primary,
                },
            ],
            AnimalDisposition = new AnimalDispositionActivity
            {
                DispositionType = dispositionType,
                Reason = request.Reason,
                RecipientOrDestination = request.RecipientOrDestination,
            },
        };

        animal.Status = animalStatus;
        animal.DispositionDate = DateOnly.FromDateTime(request.OccurredAt.Value);
        animal.DispositionReason = ToDispositionReason(dispositionType, request.Reason);
        animal.UpdatedAt = now;

        _dbContext.ActivityEvents.Add(activityEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return CreateAnimalDispositionActivityResult.Created(ToDto(
            activityEvent,
            animalId,
            animal.Name,
            animal.EnclosureId,
            animal.Enclosure.Name,
            animal.Status));
    }

    public async Task<UpdateAnimalDispositionActivityResult> UpdateAsync(
        int animalId,
        long activityId,
        UpdateAnimalDispositionActivityDto request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var animal = await _dbContext.Animals
            .SingleOrDefaultAsync(animal => animal.Id == animalId, cancellationToken);

        if (animal is null)
        {
            return UpdateAnimalDispositionActivityResult.NotFound();
        }

        if (OccursBeforeAcquisition(request.OccurredAt!.Value, animal.AcquiredDate))
        {
            return UpdateAnimalDispositionActivityResult.OccurredBeforeAcquisition();
        }

        var dispositionEvent = await _dbContext.ActivityEvents
            .Include(activityEvent => activityEvent.AnimalDisposition)
            .Include(activityEvent => activityEvent.Animals)
                .ThenInclude(association => association.Animal)
            .Include(activityEvent => activityEvent.Enclosures)
                .ThenInclude(association => association.Enclosure)
            .SingleOrDefaultAsync(
                activityEvent =>
                    activityEvent.Id == activityId &&
                    activityEvent.EventType == ActivityEventType.AnimalDisposition &&
                    activityEvent.Animals.Any(association => association.AnimalId == animalId),
                cancellationToken);

        if (dispositionEvent?.AnimalDisposition is null)
        {
            return UpdateAnimalDispositionActivityResult.NotFound();
        }

        var dispositionType = request.DispositionType!.Value;
        var animalStatus = ToAnimalStatus(dispositionType);
        var now = DateTime.UtcNow;

        dispositionEvent.OccurredAt = request.OccurredAt.Value;
        dispositionEvent.Notes = request.Notes;
        dispositionEvent.PerformedBy = request.PerformedBy;
        dispositionEvent.Title = ToAnimalTimelineTitle(dispositionType, request.RecipientOrDestination);
        dispositionEvent.UpdatedAt = now;
        dispositionEvent.AnimalDisposition.DispositionType = dispositionType;
        dispositionEvent.AnimalDisposition.Reason = request.Reason;
        dispositionEvent.AnimalDisposition.RecipientOrDestination = request.RecipientOrDestination;

        animal.Status = animalStatus;
        animal.DispositionDate = DateOnly.FromDateTime(request.OccurredAt.Value);
        animal.DispositionReason = ToDispositionReason(dispositionType, request.Reason);
        animal.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return UpdateAnimalDispositionActivityResult.Updated(ToDto(dispositionEvent, animalId, animalStatus));
    }

    public async Task<DeleteAnimalDispositionActivityResult> DeleteAsync(
        int animalId,
        long activityId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var animal = await _dbContext.Animals
            .SingleOrDefaultAsync(animal => animal.Id == animalId, cancellationToken);

        if (animal is null)
        {
            return DeleteAnimalDispositionActivityResult.NotFound;
        }

        var dispositionEvent = await _dbContext.ActivityEvents
            .Include(activityEvent => activityEvent.AnimalDisposition)
            .Include(activityEvent => activityEvent.Animals)
            .SingleOrDefaultAsync(
                activityEvent =>
                    activityEvent.Id == activityId &&
                    activityEvent.EventType == ActivityEventType.AnimalDisposition &&
                    activityEvent.Animals.Any(association => association.AnimalId == animalId),
                cancellationToken);

        if (dispositionEvent?.AnimalDisposition is null)
        {
            return DeleteAnimalDispositionActivityResult.NotFound;
        }

        var laterDispositionExists = await _dbContext.ActivityEvents
            .AsNoTracking()
            .AnyAsync(
                activityEvent =>
                    activityEvent.EventType == ActivityEventType.AnimalDisposition &&
                    activityEvent.Animals.Any(association => association.AnimalId == animalId) &&
                    (activityEvent.OccurredAt > dispositionEvent.OccurredAt ||
                        (activityEvent.OccurredAt == dispositionEvent.OccurredAt && activityEvent.Id > dispositionEvent.Id)),
                cancellationToken);

        if (laterDispositionExists)
        {
            return DeleteAnimalDispositionActivityResult.NotLatestDisposition;
        }

        if (animal.Status != ToAnimalStatus(dispositionEvent.AnimalDisposition.DispositionType))
        {
            return DeleteAnimalDispositionActivityResult.AnimalStateMismatch;
        }

        animal.Status = AnimalStatus.Active;
        animal.DispositionDate = null;
        animal.DispositionReason = null;
        animal.UpdatedAt = DateTime.UtcNow;

        _dbContext.ActivityEvents.Remove(dispositionEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return DeleteAnimalDispositionActivityResult.Deleted;
    }

    private IQueryable<ActivityEvent> DispositionEvents()
    {
        return _dbContext.ActivityEvents
            .AsNoTracking()
            .Include(activityEvent => activityEvent.AnimalDisposition)
            .Include(activityEvent => activityEvent.Animals)
                .ThenInclude(association => association.Animal)
            .Include(activityEvent => activityEvent.Enclosures)
                .ThenInclude(association => association.Enclosure)
            .Where(activityEvent => activityEvent.EventType == ActivityEventType.AnimalDisposition &&
                activityEvent.AnimalDisposition != null);
    }

    private static AnimalDispositionActivityDto ToDto(
        ActivityEvent activityEvent,
        int animalId,
        AnimalStatus? animalStatus = null)
    {
        var animalAssociation = activityEvent.Animals.Single(association => association.AnimalId == animalId);
        var enclosureAssociation = activityEvent.Enclosures
            .OrderBy(association => association.RelationshipType == ActivityEventEnclosureRelationshipType.Primary ? 0 : 1)
            .First();

        return ToDto(
            activityEvent,
            animalId,
            animalAssociation.Animal.Name,
            enclosureAssociation.EnclosureId,
            enclosureAssociation.Enclosure.Name,
            animalStatus ?? ToAnimalStatus(activityEvent.AnimalDisposition!.DispositionType));
    }

    private static AnimalDispositionActivityDto ToDto(
        ActivityEvent activityEvent,
        int animalId,
        string animalName,
        int enclosureId,
        string enclosureName,
        AnimalStatus animalStatus)
    {
        var disposition = activityEvent.AnimalDisposition!;

        return new AnimalDispositionActivityDto
        {
            ActivityEventId = activityEvent.Id,
            AnimalId = animalId,
            AnimalName = animalName,
            EnclosureId = enclosureId,
            EnclosureName = enclosureName,
            DispositionType = disposition.DispositionType,
            AnimalStatus = animalStatus,
            OccurredAt = activityEvent.OccurredAt,
            Reason = disposition.Reason,
            RecipientOrDestination = disposition.RecipientOrDestination,
            Notes = activityEvent.Notes,
            PerformedBy = activityEvent.PerformedBy,
            CreatedAt = activityEvent.CreatedAt,
            UpdatedAt = activityEvent.UpdatedAt,
        };
    }

    private static AnimalStatus ToAnimalStatus(AnimalDispositionType dispositionType)
    {
        return dispositionType switch
        {
            AnimalDispositionType.Sold => AnimalStatus.Sold,
            AnimalDispositionType.Surrendered => AnimalStatus.Surrendered,
            AnimalDispositionType.Transferred => AnimalStatus.Transferred,
            AnimalDispositionType.Released => AnimalStatus.Released,
            AnimalDispositionType.Deceased => AnimalStatus.Deceased,
            AnimalDispositionType.Other => AnimalStatus.Inactive,
            _ => AnimalStatus.Inactive,
        };
    }

    private static bool IsTerminalStatus(AnimalStatus status)
    {
        return status is AnimalStatus.Sold
            or AnimalStatus.Surrendered
            or AnimalStatus.Transferred
            or AnimalStatus.Released
            or AnimalStatus.Deceased
            or AnimalStatus.Inactive;
    }

    private static bool OccursBeforeAcquisition(DateTime occurredAt, DateOnly acquiredDate)
    {
        return DateOnly.FromDateTime(occurredAt) < acquiredDate;
    }

    private static string ToDispositionReason(AnimalDispositionType dispositionType, string? reason)
    {
        return string.IsNullOrWhiteSpace(reason)
            ? dispositionType.ToString()
            : reason;
    }

    public static string ToAnimalTimelineTitle(
        AnimalDispositionType dispositionType,
        string? recipientOrDestination)
    {
        return dispositionType switch
        {
            AnimalDispositionType.Sold when !string.IsNullOrWhiteSpace(recipientOrDestination) =>
                $"Sold to {recipientOrDestination}",
            AnimalDispositionType.Surrendered when !string.IsNullOrWhiteSpace(recipientOrDestination) =>
                $"Surrendered to {recipientOrDestination}",
            AnimalDispositionType.Transferred when !string.IsNullOrWhiteSpace(recipientOrDestination) =>
                $"Transferred to {recipientOrDestination}",
            AnimalDispositionType.Released => "Released",
            AnimalDispositionType.Deceased => "Marked deceased",
            AnimalDispositionType.Other when !string.IsNullOrWhiteSpace(recipientOrDestination) =>
                $"Disposition recorded for {recipientOrDestination}",
            _ => dispositionType.ToString(),
        };
    }
}
