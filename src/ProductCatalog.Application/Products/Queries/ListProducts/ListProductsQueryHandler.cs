using MediatR;
using ProductCatalog.Application.Common;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.Application.Products.Queries.ListProducts;

public class ListProductsQueryHandler : IRequestHandler<ListProductsQuery, Result<IReadOnlyList<ProductDto>>>
{
    private readonly IProductRepository _repository;

    public ListProductsQueryHandler(IProductRepository repository) => _repository = repository;

    public async Task<Result<IReadOnlyList<ProductDto>>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _repository.GetAllAsync(cancellationToken);
        return Result<IReadOnlyList<ProductDto>>.Success(products.Select(p => p.ToDto()).ToList());
    }
}
