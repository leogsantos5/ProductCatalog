using MediatR;
using ProductCatalog.Application.Common;

namespace ProductCatalog.Application.Products.Commands.UpdateProduct;

public record UpdateProductCommand(int Id, string Name, string? Description, decimal Price)
    : IRequest<Result<ProductDto>>;
