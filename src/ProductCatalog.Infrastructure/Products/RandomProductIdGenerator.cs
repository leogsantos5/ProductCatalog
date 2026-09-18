using ProductCatalog.Domain.Entities;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.Infrastructure.Products;

/// <summary>
/// Picks a random 6-digit candidate ID. Uniqueness is not this class's job: the primary key
/// guarantees it, and <c>CreateProductCommandHandler</c> retries with a new candidate when an insert
/// collides, which is what keeps IDs unique with several instances running at once.
/// </summary>
public class RandomProductIdGenerator : IProductIdGenerator
{
    public int NextCandidate() => Random.Shared.Next(Product.MinId, Product.MaxId + 1);
}
