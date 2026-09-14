namespace ProductCatalog.Application.Products;

public record ProductDto(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    DateTime CreatedAt,
    DateTime UpdatedAt);
