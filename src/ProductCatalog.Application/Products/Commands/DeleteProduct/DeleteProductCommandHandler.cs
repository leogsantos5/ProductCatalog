using MediatR;
using ProductCatalog.Application.Common;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.Application.Products.Commands.DeleteProduct;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductCommandHandler(IProductRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (product is null)
            return Result.Failure($"Product {request.Id} was not found.", ErrorCodes.NotFound);

        _repository.Remove(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
