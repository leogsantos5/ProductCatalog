using FluentAssertions;
using Moq;
using ProductCatalog.Application.Common;
using ProductCatalog.Application.Products.Commands.UpdateProduct;
using ProductCatalog.Domain.Entities;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.UnitTests.Application;

public class UpdateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly UpdateProductCommandHandler _handler;

    public UpdateProductCommandHandlerTests()
    {
        _handler = new UpdateProductCommandHandler(_repository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ExistingProduct_UpdatesAndSaves()
    {
        var product = Product.Create(100001, "Mouse", "Old", 10m, 5);
        _repository.Setup(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var result = await _handler.Handle(new UpdateProductCommand(100001, "Wireless Mouse", "New", 25m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Wireless Mouse");
        result.Value.Price.Should().Be(25m);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsNotFound()
    {
        _repository.Setup(r => r.GetByIdAsync(999999, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var result = await _handler.Handle(new UpdateProductCommand(999999, "Mouse", null, 10m), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.NotFound);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
