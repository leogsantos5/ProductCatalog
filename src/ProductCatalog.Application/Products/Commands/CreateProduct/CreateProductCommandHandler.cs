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

    private readonly IProductRepository _productsRepo;
    private readonly IProductIdGenerator _idGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(IProductRepository productsRepo, IProductIdGenerator idGenerator,
                                       IUnitOfWork unitOfWork, ILogger<CreateProductCommandHandler> logger)
    {
        _productsRepo = productsRepo;
        _idGenerator = idGenerator;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ProductDto>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxIdGenerationAttempts; attempt++)
        {
            // No existence check first: the primary key rejects a taken ID, and the catch below retries.
            var candidateId = _idGenerator.NextCandidate();
            var product = Product.Create(candidateId, request.Name, request.Description, request.Price, request.InitialStock);
            await _productsRepo.AddAsync(product, cancellationToken);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result<ProductDto>.Success(product.ToDto());
            }
            catch (UniqueConstraintViolationException)
            {
                // Another product (possibly one created concurrently by another instance) has this ID.
                // The failed insert leaves the entity tracked as Added; without detaching it, the next
                // attempt would try to insert it again alongside the new one and collide every time.
                _productsRepo.Remove(product);
                _logger.LogWarning("Product ID {CandidateId} is already taken (attempt {Attempt}), retrying", candidateId, attempt);
            }
        }

        return Result<ProductDto>.Failure("Could not generate a unique product ID after multiple attempts.", ErrorCodes.IdGenerationFailed);
    }
}
