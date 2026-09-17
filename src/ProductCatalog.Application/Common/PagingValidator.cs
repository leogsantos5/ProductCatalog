using FluentValidation;

namespace ProductCatalog.Application.Common;

public class PagingValidator : AbstractValidator<IPagedQuery>
{
    public PagingValidator()
    {
        // Capping the page keeps (page - 1) * pageSize from overflowing when the query skips rows.
        RuleFor(x => x.Page).InclusiveBetween(1, int.MaxValue / Paging.MaxPageSize);
        RuleFor(x => x.PageSize).InclusiveBetween(1, Paging.MaxPageSize);
    }
}
