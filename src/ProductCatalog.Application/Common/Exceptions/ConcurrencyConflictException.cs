namespace ProductCatalog.Application.Common.Exceptions;

/// <summary>
/// Thrown by the persistence layer when an optimistic concurrency check fails (the row's
/// RowVersion changed between read and save — another request modified it concurrently).
/// Application handlers catch this to reload-and-retry a bounded number of times.
/// </summary>
public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
