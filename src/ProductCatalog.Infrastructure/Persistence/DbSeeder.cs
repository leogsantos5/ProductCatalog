using Microsoft.EntityFrameworkCore;
using ProductCatalog.Domain.Entities;

namespace ProductCatalog.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context, CancellationToken ct = default)
    {
        if (await context.Products.AnyAsync(ct))
            return;

        context.Products.AddRange(
            Product.Create(100001, "ZEISS Single Vision Lens", "Precision single vision lens with standard anti-reflective coating", 89.90m, 500),
            Product.Create(100002, "ZEISS Progressive Individual 2", "Fully personalized progressive lens design", 249.00m, 120),
            Product.Create(100003, "ZEISS DuraVision Platinum Coating", "Premium anti-reflective and anti-scratch lens coating", 39.90m, 300),
            Product.Create(100004, "ZEISS PhotoFusion X Photochromic Lens", "Lens that darkens automatically in sunlight", 129.50m, 80),
            Product.Create(100005, "ZEISS BlueGuard Digital Lens", "Blue light filtering lens for digital screen use", 99.00m, 0),
            Product.Create(100006, "ZEISS UVProtect Sun Lens", "UV-protective lens for prescription sunglasses", 149.00m, 45));

        await context.SaveChangesAsync(ct);
    }
}
