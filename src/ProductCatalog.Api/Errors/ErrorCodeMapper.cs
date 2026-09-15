using ProductCatalog.Application.Common;

namespace ProductCatalog.Api.Errors;

public static class ErrorCodeMapper
{
    public static int ToStatusCode(string? errorCode) => errorCode switch
    {
        ErrorCodes.NotFound => StatusCodes.Status404NotFound,
        ErrorCodes.InsufficientStock => StatusCodes.Status400BadRequest,
        ErrorCodes.StockLimitExceeded => StatusCodes.Status400BadRequest,
        ErrorCodes.ConcurrencyConflict => StatusCodes.Status409Conflict,
        ErrorCodes.VersionMismatch => StatusCodes.Status412PreconditionFailed,
        ErrorCodes.IdGenerationFailed => StatusCodes.Status500InternalServerError,
        _ => StatusCodes.Status500InternalServerError
    };
}
