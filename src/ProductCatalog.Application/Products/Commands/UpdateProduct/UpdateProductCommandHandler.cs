using MediatR;
using ProductCatalog.Application.Common;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.Application.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result<ProductDto>>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductCommandHandler(IProductRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductDto>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (product is null)
            return Result<ProductDto>.Failure($"Product {request.Id} was not found.", ErrorCodes.NotFound);

        product.Update(request.Name, request.Description, request.Price);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ProductDto>.Success(product.ToDto());
    }
}
