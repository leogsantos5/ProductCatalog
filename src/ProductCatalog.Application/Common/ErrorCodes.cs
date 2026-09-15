namespace ProductCatalog.Application.Common;

public static class ErrorCodes
{
    public const string NotFound = "NOT_FOUND";
    public const string InsufficientStock = "INSUFFICIENT_STOCK";
    public const string StockLimitExceeded = "STOCK_LIMIT_EXCEEDED";
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
    public const string VersionMismatch = "VERSION_MISMATCH";
    public const string IdGenerationFailed = "ID_GENERATION_FAILED";
}
