namespace ProductCatalog.Application.Common.Exceptions;

/// <summary>
/// Thrown by the persistence layer when a save operation violates a unique constraint
/// (e.g. a colliding product ID generated concurrently by another instance). Application
/// handlers catch this to retry with a new candidate rather than surfacing a 500.
/// </summary>
public class UniqueConstraintViolationException : Exception
{
    public UniqueConstraintViolationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
