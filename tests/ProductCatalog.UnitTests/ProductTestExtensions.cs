using ProductCatalog.Domain.Entities;

namespace ProductCatalog.UnitTests;

internal static class ProductTestExtensions
{
    // RowVersion is normally set by SQL Server, so tests fake it through its private setter.
    public static Product WithRowVersion(this Product product, byte[] rowVersion)
    {
        typeof(Product).GetProperty(nameof(Product.RowVersion))!.SetValue(product, rowVersion);
        return product;
    }
}
