using ProductCatalog.Domain.Entities;

namespace ProductCatalog.Domain.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Product>> SearchByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetByStockRangeAsync(int min, int max, CancellationToken ct = default);
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    void Remove(Product product);
}
