using MediatR;
using ProductCatalog.Application.Common;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.Application.Products.Queries.SearchProductsByName;

public class SearchProductsByNameQueryHandler : IRequestHandler<SearchProductsByNameQuery, Result<PagedResult<ProductDto>>>
{
    private readonly IProductRepository _productsRepo;

    public SearchProductsByNameQueryHandler(IProductRepository productsRepo) => _productsRepo = productsRepo;

    public async Task<Result<PagedResult<ProductDto>>> Handle(SearchProductsByNameQuery request, CancellationToken cancellationToken)
    {
        var (products, totalCount) = await _productsRepo.SearchByNameAsync(request.Name, request.Page, request.PageSize, cancellationToken);
        return Result<PagedResult<ProductDto>>.Success(products.ToPagedResult(request, totalCount));
    }
}
