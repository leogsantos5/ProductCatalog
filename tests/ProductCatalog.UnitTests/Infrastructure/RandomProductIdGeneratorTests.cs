using FluentAssertions;
using ProductCatalog.Infrastructure.Products;

namespace ProductCatalog.UnitTests.Infrastructure;

public class RandomProductIdGeneratorTests
{
    [Fact]
    public void NextCandidate_AlwaysReturnsSixDigitNumber()
    {
        var generator = new RandomProductIdGenerator();

        for (var i = 0; i < 1000; i++)
        {
            var candidate = generator.NextCandidate();
            candidate.Should().BeInRange(100_000, 999_999);
        }
    }
}
