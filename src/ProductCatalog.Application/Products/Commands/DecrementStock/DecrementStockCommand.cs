using MediatR;
using ProductCatalog.Application.Common;

namespace ProductCatalog.Application.Products.Commands.DecrementStock;

public record DecrementStockCommand(int Id, int Quantity) : IRequest<Result<ProductDto>>;
