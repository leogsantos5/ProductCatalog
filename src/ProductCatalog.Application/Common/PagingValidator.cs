using FluentValidation;

namespace ProductCatalog.Application.Common;

// Included by every paged query's validator.
public class PagingValidator : AbstractValidator<IPagedQuery>
{
    public PagingValidator()
    {
        // The upper bound keeps (Page - 1) * PageSize within int range when skipping rows.
        RuleFor(x => x.Page).InclusiveBetween(1, int.MaxValue / Paging.MaxPageSize);
        RuleFor(x => x.PageSize).InclusiveBetween(1, Paging.MaxPageSize);
    }
}
