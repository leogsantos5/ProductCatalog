using MediatR;
using ProductCatalog.Application.Common;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.Application.Products.Queries.GetProductById;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDto>>
{
    private readonly IProductRepository _productsRepo;

    public GetProductByIdQueryHandler(IProductRepository productsRepo) => _productsRepo = productsRepo;

    public async Task<Result<ProductDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _productsRepo.GetByIdAsync(request.Id, cancellationToken);

        return product is null ? Result<ProductDto>.Failure($"Product {request.Id} was not found.", ErrorCodes.NotFound) : Result<ProductDto>.Success(product.ToDto());
    }
}
