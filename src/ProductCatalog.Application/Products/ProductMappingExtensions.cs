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
        product.UpdatedAt);
}
