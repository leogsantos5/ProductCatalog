using ProductCatalog.Domain.Entities;

namespace ProductCatalog.Domain.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);

    // List methods return one page, ordered by ID, plus the total number of matches.
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task<(IReadOnlyList<Product> Items, int TotalCount)> SearchByNameAsync(string name, int page, int pageSize, CancellationToken ct = default);
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetByStockRangeAsync(int min, int max, int page, int pageSize, CancellationToken ct = default);

    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    void Remove(Product product);

    // Stock changes run as a single atomic UPDATE, applied immediately rather than through IUnitOfWork.
    // They return false when no row matched: the product doesn't exist, lacks the stock to decrement,
    // or would go past int.MaxValue.
    Task<bool> TryDecrementStockAsync(int id, int quantity, CancellationToken ct = default);
    Task<bool> TryAddStockAsync(int id, int quantity, CancellationToken ct = default);
}
