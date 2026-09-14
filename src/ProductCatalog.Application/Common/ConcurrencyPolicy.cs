namespace ProductCatalog.Application.Common;

public static class ConcurrencyPolicy
{
    // Optimistic-concurrency retry count for stock updates: two instances decrementing/adding
    // stock on the same product at the same time will make one of them lose the RowVersion race.
    // Reloading and reapplying the operation a few times resolves that without surfacing a
    // spurious failure to the caller.
    public const int MaxStockUpdateRetries = 3;
}
