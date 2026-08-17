namespace skipper_api.Services.AnimalMovements;

public enum DeleteAnimalMovementActivityResult
{
    Deleted,
    NotFound,
    NotLatestMovement,
    AnimalLocationMismatch,
}
