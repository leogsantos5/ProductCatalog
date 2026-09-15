namespace ProductCatalog.Application.Common;

public interface IPagedQuery
{
    int Page { get; }
    int PageSize { get; }
}
