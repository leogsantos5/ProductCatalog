using FluentAssertions;
using ProductCatalog.Application.Products.Commands.AddStock;

namespace ProductCatalog.UnitTests.Application;

public class AddStockCommandValidatorTests
{
    private readonly AddStockCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(new AddStockCommand(100001, 5));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveQuantity_HasError(int quantity)
    {
        var result = _validator.Validate(new AddStockCommand(100001, quantity));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddStockCommand.Quantity));
    }
}
