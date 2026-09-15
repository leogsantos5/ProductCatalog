using MediatR;
using ProductCatalog.Application.Common;

namespace ProductCatalog.Application.Products.Queries.ListProducts;

public record ListProductsQuery(int Page = Paging.DefaultPage, int PageSize = Paging.DefaultPageSize)
    : IRequest<Result<PagedResult<ProductDto>>>, IPagedQuery;
