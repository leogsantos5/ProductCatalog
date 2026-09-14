using ProductCatalog.Domain.Entities;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.Infrastructure.Products;

public class RandomProductIdGenerator : IProductIdGenerator
{
    public int NextCandidate() => Random.Shared.Next(Product.MinId, Product.MaxId + 1);
}
