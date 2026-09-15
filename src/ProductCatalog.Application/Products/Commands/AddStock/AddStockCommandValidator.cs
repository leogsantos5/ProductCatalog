using FluentValidation;

namespace ProductCatalog.Application.Products.Commands.AddStock;

public class AddStockCommandValidator : AbstractValidator<AddStockCommand>
{
    public AddStockCommandValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
