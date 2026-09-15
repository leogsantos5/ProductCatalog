using FluentAssertions;
using ProductCatalog.Application.Products.Commands.UpdateProduct;

namespace ProductCatalog.UnitTests.Application;

public class UpdateProductCommandValidatorTests
{
    private readonly UpdateProductCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(new UpdateProductCommand(100001, "Mouse", "A mouse", 19.99m));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyName_HasError()
    {
        var result = _validator.Validate(new UpdateProductCommand(100001, "", null, 19.99m));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProductCommand.Name));
    }

    [Fact]
    public void Validate_ZeroPrice_HasError()
    {
        var result = _validator.Validate(new UpdateProductCommand(100001, "Mouse", null, 0m));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProductCommand.Price));
    }

    [Fact]
    public void Validate_PriceWithMoreThanTwoDecimals_HasError()
    {
        var result = _validator.Validate(new UpdateProductCommand(100001, "Mouse", null, 19.999m));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProductCommand.Price));
    }
}
