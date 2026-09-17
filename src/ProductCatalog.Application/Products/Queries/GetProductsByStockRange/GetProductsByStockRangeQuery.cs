using MediatR;
using ProductCatalog.Application.Common;

namespace ProductCatalog.Application.Products.Queries.GetProductsByStockRange;

public record GetProductsByStockRangeQuery(int Min, int Max, int Page = Paging.DefaultPage, int PageSize = Paging.DefaultPageSize) : IRequest<Result<PagedResult<ProductDto>>>, IPagedQuery;
