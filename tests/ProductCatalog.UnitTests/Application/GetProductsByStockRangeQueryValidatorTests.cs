using FluentAssertions;
using ProductCatalog.Application.Products.Queries.GetProductsByStockRange;

namespace ProductCatalog.UnitTests.Application;

public class GetProductsByStockRangeQueryValidatorTests
{
    private readonly GetProductsByStockRangeQueryValidator _validator = new();

    [Fact]
    public void Validate_MinLessThanMax_HasNoErrors()
    {
        var result = _validator.Validate(new GetProductsByStockRangeQuery(0, 100));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MinEqualsMax_HasNoErrors()
    {
        var result = _validator.Validate(new GetProductsByStockRangeQuery(50, 50));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MinGreaterThanMax_HasError()
    {
        var result = _validator.Validate(new GetProductsByStockRangeQuery(100, 0));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_NegativeMin_HasError()
    {
        var result = _validator.Validate(new GetProductsByStockRangeQuery(-1, 10));

        result.IsValid.Should().BeFalse();
    }
}
