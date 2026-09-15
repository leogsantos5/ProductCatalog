using FluentAssertions;
using Microsoft.AspNetCore.Http;
using ProductCatalog.Api.Errors;
using ProductCatalog.Application.Common;

namespace ProductCatalog.UnitTests.Api;

public class ErrorCodeMapperTests
{
    [Theory]
    [InlineData(ErrorCodes.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorCodes.InsufficientStock, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorCodes.StockLimitExceeded, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorCodes.ConcurrencyConflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorCodes.VersionMismatch, StatusCodes.Status412PreconditionFailed)]
    [InlineData(ErrorCodes.IdGenerationFailed, StatusCodes.Status500InternalServerError)]
    public void ToStatusCode_KnownErrorCode_ReturnsMappedStatus(string errorCode, int expectedStatus)
    {
        var status = ErrorCodeMapper.ToStatusCode(errorCode);

        status.Should().Be(expectedStatus);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("SOMETHING_ELSE")]
    public void ToStatusCode_UnknownErrorCode_ReturnsInternalServerError(string? errorCode)
    {
        var status = ErrorCodeMapper.ToStatusCode(errorCode);

        status.Should().Be(StatusCodes.Status500InternalServerError);
    }
}
