namespace ProductCatalog.Api.Contracts;

public record PagedApiResponse<T>(IReadOnlyList<T> Data, int Page, int PageSize, int TotalCount, int TotalPages);
