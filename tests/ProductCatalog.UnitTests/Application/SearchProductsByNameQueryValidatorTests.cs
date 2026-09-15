using FluentAssertions;
using ProductCatalog.Application.Products.Queries.SearchProductsByName;

namespace ProductCatalog.UnitTests.Application;

public class SearchProductsByNameQueryValidatorTests
{
    private readonly SearchProductsByNameQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyName_HasNoErrors()
    {
        var result = _validator.Validate(new SearchProductsByNameQuery("lens"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BlankName_HasError(string name)
    {
        var result = _validator.Validate(new SearchProductsByNameQuery(name));

        result.IsValid.Should().BeFalse();
    }
}
