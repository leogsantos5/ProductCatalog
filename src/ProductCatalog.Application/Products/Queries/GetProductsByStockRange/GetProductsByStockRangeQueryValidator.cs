using FluentValidation;

namespace ProductCatalog.Application.Products.Queries.GetProductsByStockRange;

public class GetProductsByStockRangeQueryValidator : AbstractValidator<GetProductsByStockRangeQuery>
{
    public GetProductsByStockRangeQueryValidator()
    {
        RuleFor(x => x.Min).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Max).GreaterThanOrEqualTo(0);
        RuleFor(x => x).Must(x => x.Min <= x.Max)
            .WithMessage("'min' must be less than or equal to 'max'.");
    }
}
