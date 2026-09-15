using FluentValidation;

namespace ProductCatalog.Application.Products.Commands.DecrementStock;

public class DecrementStockCommandValidator : AbstractValidator<DecrementStockCommand>
{
    public DecrementStockCommandValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
