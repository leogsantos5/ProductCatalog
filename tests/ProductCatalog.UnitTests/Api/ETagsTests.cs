using FluentAssertions;
using ProductCatalog.Api.Http;

namespace ProductCatalog.UnitTests.Api;

public class ETagsTests
{
    [Fact]
    public void Format_Version_WrapsItInQuotes()
    {
        var etag = ETags.Format("AAAAAAAAB9E=");

        etag.Should().Be("\"AAAAAAAAB9E=\"");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("*")]
    public void ParseIfMatch_NoPrecondition_ReturnsNull(string? ifMatch)
    {
        var version = ETags.ParseIfMatch(ifMatch);

        version.Should().BeNull();
    }

    [Fact]
    public void ParseIfMatch_StrongETag_ReturnsVersionWithoutQuotes()
    {
        var version = ETags.ParseIfMatch("\"AAAAAAAAB9E=\"");

        version.Should().Be("AAAAAAAAB9E=");
    }

    [Theory]
    [InlineData("W/\"AAAAAAAAB9E=\"")]
    [InlineData("\"AAAAAAAAB9E=\", \"AAAAAAAAB9I=\"")]
    public void ParseIfMatch_UnsupportedValue_ReturnsValueThatCannotMatch(string ifMatch)
    {
        var version = ETags.ParseIfMatch(ifMatch);

        version.Should().NotBe("AAAAAAAAB9E=");
    }
}
