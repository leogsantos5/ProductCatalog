using System.Text.Json;
using FluentAssertions;
using ProductCatalog.Api.Contracts;

namespace ProductCatalog.UnitTests.Api;

public class ProductRequestContractTests
{
    // Same defaults ASP.NET Core uses to bind request bodies.
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void CreateProductRequest_AllRequiredFields_Deserializes()
    {
        var request = JsonSerializer.Deserialize<CreateProductRequest>("""{ "name": "Lens", "price": 10.5, "initialStock": 0 }""", WebOptions);

        request.Should().Be(new CreateProductRequest("Lens", null, 10.5m, 0));
    }

    [Theory]
    [InlineData("""{ "price": 10.5, "initialStock": 5 }""")]
    [InlineData("""{ "name": "Lens", "initialStock": 5 }""")]
    [InlineData("""{ "name": "Lens", "price": 10.5 }""")]
    public void CreateProductRequest_MissingRequiredField_Throws(string json)
    {
        var act = () => JsonSerializer.Deserialize<CreateProductRequest>(json, WebOptions);

        act.Should().Throw<JsonException>();
    }

    [Theory]
    [InlineData("""{ "price": 10.5 }""")]
    [InlineData("""{ "name": "Lens" }""")]
    public void UpdateProductRequest_MissingRequiredField_Throws(string json)
    {
        var act = () => JsonSerializer.Deserialize<UpdateProductRequest>(json, WebOptions);

        act.Should().Throw<JsonException>();
    }
}
