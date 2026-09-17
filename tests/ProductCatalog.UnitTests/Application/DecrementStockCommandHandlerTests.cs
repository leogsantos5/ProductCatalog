using FluentAssertions;
using Moq;
using ProductCatalog.Application.Common;
using ProductCatalog.Application.Products.Commands.DecrementStock;
using ProductCatalog.Domain.Entities;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.UnitTests.Application;

public class DecrementStockCommandHandlerTests
{
    private readonly Mock<IProductRepository> _productsRepo = new();
    private readonly DecrementStockCommandHandler _handler;

    public DecrementStockCommandHandlerTests()
    {
        _handler = new DecrementStockCommandHandler(_productsRepo.Object);
    }

    [Fact]
    public async Task Handle_SufficientStock_ReturnsProductAfterDecrement()
    {
        _productsRepo.Setup(r => r.TryDecrementStockAsync(100001, 5, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _productsRepo.Setup(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync(Product.Create(100001, "Mouse", null, 10m, 15));

        var result = await _handler.Handle(new DecrementStockCommand(100001, 5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.StockQuantity.Should().Be(15);
        _productsRepo.Verify(r => r.TryDecrementStockAsync(100001, 5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InsufficientStock_ReturnsInsufficientStockFailure()
    {
        _productsRepo.Setup(r => r.TryDecrementStockAsync(100001, 10, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _productsRepo.Setup(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync(Product.Create(100001, "Mouse", null, 10m, 3));

        var result = await _handler.Handle(new DecrementStockCommand(100001, 10), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.InsufficientStock);
        result.Error.Should().Contain("available 3");
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsNotFound()
    {
        _productsRepo.Setup(r => r.TryDecrementStockAsync(999999, 5, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _productsRepo.Setup(r => r.GetByIdAsync(999999, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var result = await _handler.Handle(new DecrementStockCommand(999999, 5), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.NotFound);
    }
}
