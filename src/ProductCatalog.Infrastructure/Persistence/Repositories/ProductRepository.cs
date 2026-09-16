using Microsoft.EntityFrameworkCore;
using ProductCatalog.Domain.Entities;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.Infrastructure.Persistence.Repositories;

public class ProductRepository : IProductRepository
{
    private const string LikeEscapeCharacter = "\\";

    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context) => _context = context;

    public Task<Product?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _context.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<(IReadOnlyList<Product> Items, int TotalCount)> GetAllAsync(int page, int pageSize, CancellationToken ct = default) =>
        ToPageAsync(_context.Products.AsNoTracking(), page, pageSize, ct);

    public Task<(IReadOnlyList<Product> Items, int TotalCount)> SearchByNameAsync(string name, int page, int pageSize, CancellationToken ct = default)
    {
        var pattern = $"%{EscapeLikePattern(name)}%";

        return ToPageAsync(_context.Products.AsNoTracking().Where(p => EF.Functions.Like(p.Name, pattern, LikeEscapeCharacter)), page, pageSize, ct);
    }

    public Task<(IReadOnlyList<Product> Items, int TotalCount)> GetByStockRangeAsync(int min, int max, int page, int pageSize, CancellationToken ct = default) =>
        ToPageAsync(_context.Products.AsNoTracking().Where(p => p.StockQuantity >= min && p.StockQuantity <= max), page, pageSize, ct);

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default) => _context.Products.AnyAsync(p => p.Id == id, ct);

    public async Task AddAsync(Product product, CancellationToken ct = default) => await _context.Products.AddAsync(product, ct);

    public void Remove(Product product) => _context.Products.Remove(product);

    public async Task<bool> TryDecrementStockAsync(int id, int quantity, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var affectedRows = await _context.Products.Where(p => p.Id == id && p.StockQuantity >= quantity)
                                                  .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity - quantity)
                                                                            .SetProperty(p => p.UpdatedAt, now), ct);

        return affectedRows > 0;
    }

    public async Task<bool> TryAddStockAsync(int id, int quantity, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var affectedRows = await _context.Products.Where(p => p.Id == id && p.StockQuantity <= int.MaxValue - quantity)
                                                  .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity + quantity)
                                                                            .SetProperty(p => p.UpdatedAt, now), ct);
        return affectedRows > 0;
    }

    private static async Task<(IReadOnlyList<Product> Items, int TotalCount)> ToPageAsync(IQueryable<Product> query, int page, int pageSize, CancellationToken ct)
    {
        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderBy(p => p.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return (items, totalCount);
    }

    internal static string EscapeLikePattern(string value) => value
                          .Replace(LikeEscapeCharacter, LikeEscapeCharacter + LikeEscapeCharacter)
                          .Replace("%", LikeEscapeCharacter + "%")
                          .Replace("_", LikeEscapeCharacter + "_")
                          .Replace("[", LikeEscapeCharacter + "[");
}
