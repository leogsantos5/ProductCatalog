using MediatR;
using ProductCatalog.Application.Common;

namespace ProductCatalog.Application.Products.Queries.ListProducts;

public record ListProductsQuery : IRequest<Result<IReadOnlyList<ProductDto>>>;
