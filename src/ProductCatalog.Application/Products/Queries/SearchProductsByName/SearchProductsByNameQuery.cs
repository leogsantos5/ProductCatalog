using MediatR;
using ProductCatalog.Application.Common;

namespace ProductCatalog.Application.Products.Queries.SearchProductsByName;

public record SearchProductsByNameQuery(string Name, int Page = Paging.DefaultPage, int PageSize = Paging.DefaultPageSize)
    : IRequest<Result<PagedResult<ProductDto>>>, IPagedQuery;
