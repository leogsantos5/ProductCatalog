using FluentAssertions;
using ProductCatalog.Application.Products.Commands.CreateProduct;

namespace ProductCatalog.UnitTests.Application;

public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(new CreateProductCommand("Mouse", "A mouse", 19.99m, 10));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyName_HasError()
    {
        var result = _validator.Validate(new CreateProductCommand("", null, 19.99m, 10));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.Name));
    }

    [Fact]
    public void Validate_ZeroPrice_HasError()
    {
        var result = _validator.Validate(new CreateProductCommand("Mouse", null, 0m, 10));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.Price));
    }

    [Fact]
    public void Validate_PriceWithMoreThanTwoDecimals_HasError()
    {
        var result = _validator.Validate(new CreateProductCommand("Mouse", null, 19.999m, 10));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.Price));
    }

    [Fact]
    public void Validate_PriceWithTrailingZeros_HasNoErrors()
    {
        var result = _validator.Validate(new CreateProductCommand("Mouse", null, 19.9000m, 10));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NegativeInitialStock_HasError()
    {
        var result = _validator.Validate(new CreateProductCommand("Mouse", null, 19.99m, -1));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.InitialStock));
    }
}
