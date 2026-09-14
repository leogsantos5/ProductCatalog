using MediatR;
using ProductCatalog.Application.Common;

namespace ProductCatalog.Application.Products.Queries.GetProductById;

public record GetProductByIdQuery(int Id) : IRequest<Result<ProductDto>>;
