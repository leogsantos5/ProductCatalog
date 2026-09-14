using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ProductCatalog.Api.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (statusCode, title, errors) = exception switch
        {
            ValidationException ve => (StatusCodes.Status400BadRequest, "Validation failed", ve.Errors.Select(e => e.ErrorMessage).ToList()),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", new List<string> { "Please try again later." })
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        if (errors.Count > 0)
            problem.Extensions["errors"] = errors;

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}
