using MediatR;
using ProductCatalog.Application.Common;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.Application.Products.Queries.SearchProductsByName;

public class SearchProductsByNameQueryHandler : IRequestHandler<SearchProductsByNameQuery, Result<PagedResult<ProductDto>>>
{
    private readonly IProductRepository _repository;

    public SearchProductsByNameQueryHandler(IProductRepository repository) => _repository = repository;

    public async Task<Result<PagedResult<ProductDto>>> Handle(SearchProductsByNameQuery request, CancellationToken cancellationToken)
    {
        var (products, totalCount) = await _repository.SearchByNameAsync(request.Name, request.Page, request.PageSize, cancellationToken);
        return Result<PagedResult<ProductDto>>.Success(products.ToPagedResult(request, totalCount));
    }
}
