using ProductCatalog.Application.Common;
using ProductCatalog.Domain.Entities;

namespace ProductCatalog.Application.Products;

public static class ProductMappingExtensions
{
    public static ProductDto ToDto(this Product product) => new(
        product.Id,
        product.Name,
        product.Description,
        product.Price,
        product.StockQuantity,
        product.CreatedAt,
        product.UpdatedAt,
        product.GetVersion());

    public static string GetVersion(this Product product) => Convert.ToBase64String(product.RowVersion);

    public static PagedResult<ProductDto> ToPagedResult(this IReadOnlyList<Product> products, IPagedQuery query, int totalCount) =>
        new(products.Select(p => p.ToDto()).ToList(), query.Page, query.PageSize, totalCount);
}
