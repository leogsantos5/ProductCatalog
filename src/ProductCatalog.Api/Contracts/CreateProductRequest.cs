namespace ProductCatalog.Api.Contracts;

public record CreateProductRequest(string Name, string? Description, decimal Price, int InitialStock);
