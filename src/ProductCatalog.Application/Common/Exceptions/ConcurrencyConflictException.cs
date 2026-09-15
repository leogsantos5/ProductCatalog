namespace ProductCatalog.Application.Common.Exceptions;

/// <summary>The row changed between read and save (RowVersion mismatch).</summary>
public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
