using MediatR;
using Microsoft.Extensions.Logging;
using ProductCatalog.Application.Common;
using ProductCatalog.Application.Common.Exceptions;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.Application.Products.Commands.DecrementStock;

public class DecrementStockCommandHandler : IRequestHandler<DecrementStockCommand, Result<ProductDto>>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DecrementStockCommandHandler> _logger;

    public DecrementStockCommandHandler(IProductRepository repository, IUnitOfWork unitOfWork, ILogger<DecrementStockCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ProductDto>> Handle(DecrementStockCommand request, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= ConcurrencyPolicy.MaxConcurrencyRetries; attempt++)
        {
            var product = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (product is null)
                return Result<ProductDto>.Failure($"Product {request.Id} was not found.", ErrorCodes.NotFound);

            try
            {
                product.DecrementStock(request.Quantity);
            }
            catch (InvalidOperationException ex)
            {
                return Result<ProductDto>.Failure(ex.Message, ErrorCodes.InsufficientStock);
            }

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result<ProductDto>.Success(product.ToDto());
            }
            catch (ConcurrencyConflictException)
            {
                _unitOfWork.DiscardChanges();
                _logger.LogWarning(
                    "Concurrency conflict decrementing stock for product {ProductId}, attempt {Attempt}, retrying",
                    request.Id, attempt);
            }
        }

        return Result<ProductDto>.Failure(
            "The product was updated concurrently too many times; please retry.",
            ErrorCodes.ConcurrencyConflict);
    }
}
