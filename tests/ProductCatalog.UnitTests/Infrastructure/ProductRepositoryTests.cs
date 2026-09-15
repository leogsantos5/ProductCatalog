using FluentAssertions;
using ProductCatalog.Infrastructure.Persistence.Repositories;

namespace ProductCatalog.UnitTests.Infrastructure;

public class ProductRepositoryTests
{
    [Theory]
    [InlineData("lens", "lens")]
    [InlineData("50%", @"50\%")]
    [InlineData("a_b", @"a\_b")]
    [InlineData("[x]", @"\[x]")]
    [InlineData(@"a\b", @"a\\b")]
    public void EscapeLikePattern_Input_EscapesWildcardCharacters(string input, string expected)
    {
        var escaped = ProductRepository.EscapeLikePattern(input);

        escaped.Should().Be(expected);
    }
}
