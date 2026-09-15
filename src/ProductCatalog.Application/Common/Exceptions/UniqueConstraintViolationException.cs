namespace ProductCatalog.Application.Common.Exceptions;

/// <summary>A save violated a unique constraint, e.g. a product ID taken by another instance.</summary>
public class UniqueConstraintViolationException : Exception
{
    public UniqueConstraintViolationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
