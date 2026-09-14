using FluentAssertions;
using ProductCatalog.Domain.Entities;

namespace ProductCatalog.UnitTests.Domain;

public class ProductTests
{
    [Fact]
    public void Create_ValidData_SetsAllFields()
    {
        var product = Product.Create(100001, "Mouse", "A mouse", 19.99m, 10);

        product.Id.Should().Be(100001);
        product.Name.Should().Be("Mouse");
        product.Description.Should().Be("A mouse");
        product.Price.Should().Be(19.99m);
        product.StockQuantity.Should().Be(10);
    }

    [Theory]
    [InlineData(99999)]
    [InlineData(1000000)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_IdNotSixDigits_Throws(int id)
    {
        var act = () => Product.Create(id, "Mouse", null, 10m, 5);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_BlankName_Throws(string? name)
    {
        var act = () => Product.Create(100001, name!, null, 10m, 5);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_NonPositivePrice_Throws(decimal price)
    {
        var act = () => Product.Create(100001, "Mouse", null, price, 5);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_NegativeInitialStock_Throws()
    {
        var act = () => Product.Create(100001, "Mouse", null, 10m, -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void DecrementStock_SufficientStock_ReducesQuantity()
    {
        var product = Product.Create(100001, "Mouse", null, 10m, 20);

        product.DecrementStock(5);

        product.StockQuantity.Should().Be(15);
    }

    [Fact]
    public void DecrementStock_ExactStock_ReducesToZero()
    {
        var product = Product.Create(100001, "Mouse", null, 10m, 20);

        product.DecrementStock(20);

        product.StockQuantity.Should().Be(0);
    }

    [Fact]
    public void DecrementStock_MoreThanAvailable_Throws()
    {
        var product = Product.Create(100001, "Mouse", null, 10m, 5);

        var act = () => product.DecrementStock(6);

        act.Should().Throw<InvalidOperationException>();
        product.StockQuantity.Should().Be(5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DecrementStock_NonPositiveQuantity_Throws(int quantity)
    {
        var product = Product.Create(100001, "Mouse", null, 10m, 5);

        var act = () => product.DecrementStock(quantity);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AddStock_ValidQuantity_IncreasesQuantity()
    {
        var product = Product.Create(100001, "Mouse", null, 10m, 5);

        product.AddStock(10);

        product.StockQuantity.Should().Be(15);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddStock_NonPositiveQuantity_Throws(int quantity)
    {
        var product = Product.Create(100001, "Mouse", null, 10m, 5);

        var act = () => product.AddStock(quantity);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Update_ValidData_UpdatesNameDescriptionAndPrice()
    {
        var product = Product.Create(100001, "Mouse", "Old description", 10m, 5);

        product.Update("Wireless Mouse", "New description", 25m);

        product.Name.Should().Be("Wireless Mouse");
        product.Description.Should().Be("New description");
        product.Price.Should().Be(25m);
    }
}
