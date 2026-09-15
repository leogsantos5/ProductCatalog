namespace ProductCatalog.Api.Contracts;

public record ApiResponse<T>(T Data)
{
    public static ApiResponse<T> Ok(T data) => new(data);
}
