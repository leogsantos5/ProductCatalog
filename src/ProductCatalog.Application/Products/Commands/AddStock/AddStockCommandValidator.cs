using FluentValidation;

namespace ProductCatalog.Application.Products.Commands.AddStock;

public class AddStockCommandValidator : AbstractValidator<AddStockCommand>
{
    public AddStockCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
