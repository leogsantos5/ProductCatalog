using MediatR;
using ProductCatalog.Application.Common;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.Application.Products.Queries.SearchProductsByName;

public class SearchProductsByNameQueryHandler : IRequestHandler<SearchProductsByNameQuery, Result<IReadOnlyList<ProductDto>>>
{
    private readonly IProductRepository _repository;

    public SearchProductsByNameQueryHandler(IProductRepository repository) => _repository = repository;

    public async Task<Result<IReadOnlyList<ProductDto>>> Handle(SearchProductsByNameQuery request, CancellationToken cancellationToken)
    {
        var products = await _repository.SearchByNameAsync(request.Name, cancellationToken);
        return Result<IReadOnlyList<ProductDto>>.Success(products.Select(p => p.ToDto()).ToList());
    }
}
