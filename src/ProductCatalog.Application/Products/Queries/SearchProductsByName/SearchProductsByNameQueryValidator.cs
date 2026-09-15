using FluentValidation;
using ProductCatalog.Application.Common;

namespace ProductCatalog.Application.Products.Queries.SearchProductsByName;

public class SearchProductsByNameQueryValidator : AbstractValidator<SearchProductsByNameQuery>
{
    public SearchProductsByNameQueryValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        Include(new PagingValidator());
    }
}
