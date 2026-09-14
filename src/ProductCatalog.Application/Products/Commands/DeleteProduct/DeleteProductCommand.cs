using MediatR;
using ProductCatalog.Application.Common;

namespace ProductCatalog.Application.Products.Commands.DeleteProduct;

public record DeleteProductCommand(int Id) : IRequest<Result>;
