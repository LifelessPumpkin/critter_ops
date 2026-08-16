namespace skipper_api.Services.AnimalDispositions;

public enum DeleteAnimalDispositionActivityResult
{
    Deleted,
    NotFound,
    NotLatestDisposition,
    AnimalStateMismatch,
}
