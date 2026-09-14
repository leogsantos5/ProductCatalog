namespace ProductCatalog.Application.Common;

public record ApiResponse<T>(T? Data, IReadOnlyList<string> Errors)
{
    public static ApiResponse<T> Ok(T data) => new(data, []);

    public static ApiResponse<T> Fail(string error) => new(default, [error]);
}
