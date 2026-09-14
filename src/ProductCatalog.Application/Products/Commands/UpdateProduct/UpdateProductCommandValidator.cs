using FluentValidation;

namespace ProductCatalog.Application.Products.Commands.UpdateProduct;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Price).GreaterThan(0);
    }
}
