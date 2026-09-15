using FluentAssertions;
using ProductCatalog.Application.Common;
using ProductCatalog.Application.Products.Queries.ListProducts;

namespace ProductCatalog.UnitTests.Application;

public class ListProductsQueryValidatorTests
{
    private readonly ListProductsQueryValidator _validator = new();

    [Fact]
    public void Validate_DefaultPaging_HasNoErrors()
    {
        var result = _validator.Validate(new ListProductsQuery());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MaxPageSize_HasNoErrors()
    {
        var result = _validator.Validate(new ListProductsQuery(PageSize: Paging.MaxPageSize));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void Validate_PageOutOfRange_HasError(int page)
    {
        var result = _validator.Validate(new ListProductsQuery(Page: page));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ListProductsQuery.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(Paging.MaxPageSize + 1)]
    public void Validate_PageSizeOutOfRange_HasError(int pageSize)
    {
        var result = _validator.Validate(new ListProductsQuery(PageSize: pageSize));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ListProductsQuery.PageSize));
    }
}
