using MediatR;
using ProductCatalog.Application.Common;

namespace ProductCatalog.Application.Products.Commands.CreateProduct;

public record CreateProductCommand(string Name, string? Description, decimal Price, int InitialStock) : IRequest<Result<ProductDto>>;
