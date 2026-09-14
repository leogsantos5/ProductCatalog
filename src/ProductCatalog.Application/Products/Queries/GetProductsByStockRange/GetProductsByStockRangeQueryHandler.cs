using MediatR;
using ProductCatalog.Application.Common;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.Application.Products.Queries.GetProductsByStockRange;

public class GetProductsByStockRangeQueryHandler : IRequestHandler<GetProductsByStockRangeQuery, Result<IReadOnlyList<ProductDto>>>
{
    private readonly IProductRepository _repository;

    public GetProductsByStockRangeQueryHandler(IProductRepository repository) => _repository = repository;

    public async Task<Result<IReadOnlyList<ProductDto>>> Handle(GetProductsByStockRangeQuery request, CancellationToken cancellationToken)
    {
        var products = await _repository.GetByStockRangeAsync(request.Min, request.Max, cancellationToken);
        return Result<IReadOnlyList<ProductDto>>.Success(products.Select(p => p.ToDto()).ToList());
    }
}
