using FluentValidation;
using ProductCatalog.Application.Common;

namespace ProductCatalog.Application.Products.Queries.ListProducts;

public class ListProductsQueryValidator : AbstractValidator<ListProductsQuery>
{
    public ListProductsQueryValidator()
    {
        Include(new PagingValidator());
    }
}
