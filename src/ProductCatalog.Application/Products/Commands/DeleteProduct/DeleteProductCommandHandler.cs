using MediatR;
using Microsoft.Extensions.Logging;
using ProductCatalog.Application.Common;
using ProductCatalog.Application.Common.Exceptions;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.Application.Products.Commands.DeleteProduct;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteProductCommandHandler> _logger;

    public DeleteProductCommandHandler(IProductRepository repository, IUnitOfWork unitOfWork, ILogger<DeleteProductCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= ConcurrencyPolicy.MaxConcurrencyRetries; attempt++)
        {
            var product = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (product is null)
                return Result.Failure($"Product {request.Id} was not found.", ErrorCodes.NotFound);

            _repository.Remove(product);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }
            catch (ConcurrencyConflictException)
            {
                _unitOfWork.DiscardChanges();
                _logger.LogWarning("Concurrency conflict deleting product {ProductId}, attempt {Attempt}, retrying", request.Id, attempt);
            }
        }

        return Result.Failure(
            "The product was updated concurrently too many times; please retry.",
            ErrorCodes.ConcurrencyConflict);
    }
}
