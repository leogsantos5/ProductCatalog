using FluentAssertions;
using ProductCatalog.Application.Products.Commands.DecrementStock;

namespace ProductCatalog.UnitTests.Application;

public class DecrementStockCommandValidatorTests
{
    private readonly DecrementStockCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(new DecrementStockCommand(100001, 5));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveQuantity_HasError(int quantity)
    {
        var result = _validator.Validate(new DecrementStockCommand(100001, quantity));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DecrementStockCommand.Quantity));
    }

    [Fact]
    public void Validate_NonPositiveId_HasError()
    {
        var result = _validator.Validate(new DecrementStockCommand(0, 5));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DecrementStockCommand.Id));
    }
}
