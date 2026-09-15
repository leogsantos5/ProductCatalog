using MediatR;
using ProductCatalog.Application.Common;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.Application.Products.Commands.DecrementStock;

public class DecrementStockCommandHandler : IRequestHandler<DecrementStockCommand, Result<ProductDto>>
{
    private readonly IProductRepository _repository;

    public DecrementStockCommandHandler(IProductRepository repository) => _repository = repository;

    public async Task<Result<ProductDto>> Handle(DecrementStockCommand request, CancellationToken cancellationToken)
    {
        var decremented = await _repository.TryDecrementStockAsync(request.Id, request.Quantity, cancellationToken);
        var product = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (product is null)
            return Result<ProductDto>.Failure($"Product {request.Id} was not found.", ErrorCodes.NotFound);

        if (!decremented)
            return Result<ProductDto>.Failure($"Insufficient stock: requested {request.Quantity}, available {product.StockQuantity}.", ErrorCodes.InsufficientStock);

        return Result<ProductDto>.Success(product.ToDto());
    }
}
