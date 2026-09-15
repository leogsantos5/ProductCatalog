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

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Products.AsNoTracking().OrderBy(p => p.Id).ToListAsync(ct);

    public async Task<IReadOnlyList<Product>> SearchByNameAsync(string name, CancellationToken ct = default)
    {
        // User input is matched literally — without escaping, "%" or "_" would match every product.
        var pattern = $"%{EscapeLikePattern(name)}%";

        return await _context.Products
            .AsNoTracking()
            .Where(p => EF.Functions.Like(p.Name, pattern, LikeEscapeCharacter))
            .OrderBy(p => p.Id)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Product>> GetByStockRangeAsync(int min, int max, CancellationToken ct = default) =>
        await _context.Products
            .AsNoTracking()
            .Where(p => p.StockQuantity >= min && p.StockQuantity <= max)
            .OrderBy(p => p.Id)
            .ToListAsync(ct);

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default) =>
        _context.Products.AnyAsync(p => p.Id == id, ct);

    public async Task AddAsync(Product product, CancellationToken ct = default) =>
        await _context.Products.AddAsync(product, ct);

    public void Remove(Product product) => _context.Products.Remove(product);

    private static string EscapeLikePattern(string value) =>
        value
            .Replace(LikeEscapeCharacter, LikeEscapeCharacter + LikeEscapeCharacter)
            .Replace("%", LikeEscapeCharacter + "%")
            .Replace("_", LikeEscapeCharacter + "_")
            .Replace("[", LikeEscapeCharacter + "[");
}
