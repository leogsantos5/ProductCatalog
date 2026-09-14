using MediatR;
using ProductCatalog.Application.Common;

namespace ProductCatalog.Application.Products.Commands.AddStock;

public record AddStockCommand(int Id, int Quantity) : IRequest<Result<ProductDto>>;
