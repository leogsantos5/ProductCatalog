using MediatR;
using ProductCatalog.Application.Common;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.Application.Products.Queries.GetProductsByStockRange;

public class GetProductsByStockRangeQueryHandler : IRequestHandler<GetProductsByStockRangeQuery, Result<PagedResult<ProductDto>>>
{
    private readonly IProductRepository _repository;

    public GetProductsByStockRangeQueryHandler(IProductRepository repository) => _repository = repository;

    public async Task<Result<PagedResult<ProductDto>>> Handle(GetProductsByStockRangeQuery request, CancellationToken cancellationToken)
    {
        var (products, totalCount) = await _repository.GetByStockRangeAsync(request.Min, request.Max, request.Page, request.PageSize, cancellationToken);
        return Result<PagedResult<ProductDto>>.Success(products.ToPagedResult(request, totalCount));
    }
}
