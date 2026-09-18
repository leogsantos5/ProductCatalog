namespace ProductCatalog.Domain.Interfaces;

/// <summary>
/// Generates candidate 6-digit product identifiers. A candidate is not guaranteed unique on its
/// own — callers rely on the database's primary key to reject a duplicate and retry on collision.
/// </summary>
public interface IProductIdGenerator
{
    int NextCandidate();
}
