using MediatR;
using Microsoft.Extensions.Logging;
using ProductCatalog.Application.Common;
using ProductCatalog.Application.Common.Exceptions;
using ProductCatalog.Domain.Entities;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.Application.Products.Commands.CreateProduct;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    private const int MaxIdGenerationAttempts = 10;

    private readonly IProductRepository _productRepo;
    private readonly IProductIdGenerator _idGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(
        IProductRepository productRepo,
        IProductIdGenerator idGenerator,
        IUnitOfWork unitOfWork,
        ILogger<CreateProductCommandHandler> logger)
    {
        _productRepo = productRepo;
        _idGenerator = idGenerator;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ProductDto>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxIdGenerationAttempts; attempt++)
        {
            var candidateId = _idGenerator.NextCandidate();

            if (await _productRepo.ExistsAsync(candidateId, cancellationToken))
                continue;

            var product = Product.Create(candidateId, request.Name, request.Description, request.Price, request.InitialStock);
            await _productRepo.AddAsync(product, cancellationToken);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result<ProductDto>.Success(product.ToDto());
            }
            catch (UniqueConstraintViolationException)
            {
                _productRepo.Remove(product);
                _logger.LogWarning("Product ID {CandidateId} collided with a concurrently-created product on attempt {Attempt}, retrying", candidateId, attempt);
            }
        }

        return Result<ProductDto>.Failure("Could not generate a unique product ID after multiple attempts.", ErrorCodes.IdGenerationFailed);
    }
}
