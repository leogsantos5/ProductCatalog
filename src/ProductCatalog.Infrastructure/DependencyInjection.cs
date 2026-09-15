using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductCatalog.Domain.Interfaces;
using ProductCatalog.Infrastructure.Persistence;
using ProductCatalog.Infrastructure.Persistence.Repositories;
using ProductCatalog.Infrastructure.Products;

namespace ProductCatalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("Default")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddSingleton<IProductIdGenerator, RandomProductIdGenerator>();

        return services;
    }

    public static async Task InitialiseDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync(ct);
        await DbSeeder.SeedAsync(db, ct);
    }
}
