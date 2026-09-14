using MediatR;
using ProductCatalog.Application.Common;

namespace ProductCatalog.Application.Products.Queries.SearchProductsByName;

public record SearchProductsByNameQuery(string Name) : IRequest<Result<IReadOnlyList<ProductDto>>>;
