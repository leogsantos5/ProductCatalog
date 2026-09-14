namespace ProductCatalog.Application.Common;

public class Result<T>
{
    public T? Value { get; }
    public bool IsSuccess { get; }
    public string? Error { get; }
    public string? ErrorCode { get; }

    private Result(T? value, bool isSuccess, string? error, string? errorCode)
    {
        Value = value;
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result<T> Success(T value) => new(value, true, null, null);
    public static Result<T> Failure(string error, string? errorCode = null) => new(default, false, error, errorCode);
}

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public string? ErrorCode { get; }

    private Result(bool isSuccess, string? error, string? errorCode)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result Success() => new(true, null, null);
    public static Result Failure(string error, string? errorCode = null) => new(false, error, errorCode);
}
