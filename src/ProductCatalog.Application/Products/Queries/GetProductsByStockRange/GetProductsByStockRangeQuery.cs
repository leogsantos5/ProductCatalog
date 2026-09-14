using MediatR;
using ProductCatalog.Application.Common;

namespace ProductCatalog.Application.Products.Queries.GetProductsByStockRange;

public record GetProductsByStockRangeQuery(int Min, int Max) : IRequest<Result<IReadOnlyList<ProductDto>>>;
