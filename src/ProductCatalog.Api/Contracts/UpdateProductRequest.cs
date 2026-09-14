namespace ProductCatalog.Api.Contracts;

public record UpdateProductRequest(string Name, string? Description, decimal Price);
