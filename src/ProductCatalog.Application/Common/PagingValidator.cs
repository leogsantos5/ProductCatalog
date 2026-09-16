using FluentValidation;

namespace ProductCatalog.Application.Common;

public class PagingValidator : AbstractValidator<IPagedQuery>
{
    public PagingValidator()
    {
        RuleFor(x => x.Page).InclusiveBetween(1, int.MaxValue / Paging.MaxPageSize);
        RuleFor(x => x.PageSize).InclusiveBetween(1, Paging.MaxPageSize);
    }
}
