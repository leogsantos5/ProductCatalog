namespace ProductCatalog.Domain.Interfaces;

/// <summary>
/// Generates candidate 6-digit product identifiers. A candidate is not guaranteed unique on its
/// own — callers must verify uniqueness (e.g. against the repository/DB) and retry on collision.
/// </summary>
public interface IProductIdGenerator
{
    int NextCandidate();
}
