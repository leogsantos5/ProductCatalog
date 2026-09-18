using ProductCatalog.Domain.Entities;

namespace ProductCatalog.Domain.Interfaces;

public interface IProductRepository
{
    // Tracked, for handlers that modify or delete the product; read-only callers use GetByIdAsNoTrackingAsync.
    Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Product?> GetByIdAsNoTrackingAsync(int id, CancellationToken ct = default);

    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task<(IReadOnlyList<Product> Items, int TotalCount)> SearchByNameAsync(string name, int page, int pageSize, CancellationToken ct = default);
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetByStockRangeAsync(int min, int max, int page, int pageSize, CancellationToken ct = default);

    Task AddAsync(Product product, CancellationToken ct = default);
    void Remove(Product product);

    // Stock changes run as a single atomic UPDATE, applied immediately rather than through IUnitOfWork.
    // They return false when no row matched: the product doesn't exist, lacks the stock to decrement,
    // or would go past int.MaxValue.
    Task<bool> TryDecrementStockAsync(int id, int quantity, CancellationToken ct = default);
    Task<bool> TryAddStockAsync(int id, int quantity, CancellationToken ct = default);
}
