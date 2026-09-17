using MediatR;
using ProductCatalog.Application.Common;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.Application.Products.Queries.ListProducts;

public class ListProductsQueryHandler : IRequestHandler<ListProductsQuery, Result<PagedResult<ProductDto>>>
{
    private readonly IProductRepository _productsRepo;

    public ListProductsQueryHandler(IProductRepository productsRepo) => _productsRepo = productsRepo;

    public async Task<Result<PagedResult<ProductDto>>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        var (products, totalCount) = await _productsRepo.GetAllAsync(request.Page, request.PageSize, cancellationToken);
        return Result<PagedResult<ProductDto>>.Success(products.ToPagedResult(request, totalCount));
    }
}
