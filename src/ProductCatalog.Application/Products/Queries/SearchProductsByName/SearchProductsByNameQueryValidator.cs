using FluentValidation;

namespace ProductCatalog.Application.Products.Queries.SearchProductsByName;

public class SearchProductsByNameQueryValidator : AbstractValidator<SearchProductsByNameQuery>
{
    public SearchProductsByNameQueryValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}
