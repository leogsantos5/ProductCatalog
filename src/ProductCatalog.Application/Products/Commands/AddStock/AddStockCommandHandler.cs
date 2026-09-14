using MediatR;
using Microsoft.Extensions.Logging;
using ProductCatalog.Application.Common;
using ProductCatalog.Application.Common.Exceptions;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.Application.Products.Commands.AddStock;

public class AddStockCommandHandler : IRequestHandler<AddStockCommand, Result<ProductDto>>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddStockCommandHandler> _logger;

    public AddStockCommandHandler(
        IProductRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<AddStockCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ProductDto>> Handle(AddStockCommand request, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= ConcurrencyPolicy.MaxStockUpdateRetries; attempt++)
        {
            var product = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (product is null)
                return Result<ProductDto>.Failure($"Product {request.Id} was not found.", ErrorCodes.NotFound);

            product.AddStock(request.Quantity);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result<ProductDto>.Success(product.ToDto());
            }
            catch (ConcurrencyConflictException)
            {
                _logger.LogWarning("Concurrency conflict adding stock for product {ProductId}, attempt {Attempt}, retrying", request.Id, attempt);
            }
        }

        return Result<ProductDto>.Failure("The product was updated concurrently too many times; please retry.", ErrorCodes.ConcurrencyConflict);
    }
}
