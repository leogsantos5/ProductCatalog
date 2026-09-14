using FluentAssertions;
using Moq;
using ProductCatalog.Application.Common;
using ProductCatalog.Application.Products.Commands.DeleteProduct;
using ProductCatalog.Domain.Entities;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.UnitTests.Application;

public class DeleteProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly DeleteProductCommandHandler _handler;

    public DeleteProductCommandHandlerTests()
    {
        _handler = new DeleteProductCommandHandler(_repository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ExistingProduct_RemovesAndSaves()
    {
        var product = Product.Create(100001, "Mouse", null, 10m, 5);
        _repository.Setup(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var result = await _handler.Handle(new DeleteProductCommand(100001), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repository.Verify(r => r.Remove(product), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsNotFound()
    {
        _repository.Setup(r => r.GetByIdAsync(999999, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var result = await _handler.Handle(new DeleteProductCommand(999999), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.NotFound);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
