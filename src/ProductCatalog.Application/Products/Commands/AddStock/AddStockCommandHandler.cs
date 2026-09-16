using MediatR;
using ProductCatalog.Application.Common;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.Application.Products.Commands.AddStock;

public class AddStockCommandHandler : IRequestHandler<AddStockCommand, Result<ProductDto>>
{
    private readonly IProductRepository _productRepo;

    public AddStockCommandHandler(IProductRepository productRepo) => _productRepo = productRepo;

    public async Task<Result<ProductDto>> Handle(AddStockCommand request, CancellationToken cancellationToken)
    {
        var added = await _productRepo.TryAddStockAsync(request.Id, request.Quantity, cancellationToken);
        var product = await _productRepo.GetByIdAsync(request.Id, cancellationToken);

        if (product is null)
            return Result<ProductDto>.Failure($"Product {request.Id} was not found.", ErrorCodes.NotFound);

        if (!added)
            return Result<ProductDto>.Failure($"Adding {request.Quantity} would exceed the maximum stock of {int.MaxValue}; current stock is {product.StockQuantity}.", ErrorCodes.StockLimitExceeded);

        return Result<ProductDto>.Success(product.ToDto());
    }
}
