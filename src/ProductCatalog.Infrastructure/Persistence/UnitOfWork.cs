using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProductCatalog.Application.Common.Exceptions;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    // SQL Server error numbers for a unique index/constraint violation.
    private static readonly int[] UniqueViolationErrorNumbers = [2601, 2627];

    private readonly AppDbContext _db;

    public UnitOfWork(AppDbContext db) => _db = db;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException("The record was modified by another request.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: var number }
            && UniqueViolationErrorNumbers.Contains(number))
        {
            throw new UniqueConstraintViolationException("A record with the same key already exists.", ex);
        }
    }

    // A tracking query returns the already-tracked instance as-is instead of refreshing it from the
    // database, so a stale entity (and its stale RowVersion) has to be dropped before re-reading.
    public void DiscardChanges() => _db.ChangeTracker.Clear();
}
