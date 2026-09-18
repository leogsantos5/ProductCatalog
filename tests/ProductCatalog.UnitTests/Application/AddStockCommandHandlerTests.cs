using FluentAssertions;
using Moq;
using ProductCatalog.Application.Common;
using ProductCatalog.Application.Products.Commands.AddStock;
using ProductCatalog.Domain.Entities;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.UnitTests.Application;

public class AddStockCommandHandlerTests
{
    private readonly Mock<IProductRepository> _productsRepo = new();
    private readonly AddStockCommandHandler _handler;

    public AddStockCommandHandlerTests()
    {
        _handler = new AddStockCommandHandler(_productsRepo.Object);
    }

    [Fact]
    public async Task Handle_ExistingProduct_ReturnsProductAfterAddingStock()
    {
        _productsRepo.Setup(r => r.TryAddStockAsync(100001, 10, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _productsRepo.Setup(r => r.GetByIdAsNoTrackingAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync(Product.Create(100001, "Mouse", null, 10m, 15));

        var result = await _handler.Handle(new AddStockCommand(100001, 10), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.StockQuantity.Should().Be(15);
        _productsRepo.Verify(r => r.TryAddStockAsync(100001, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsNotFound()
    {
        _productsRepo.Setup(r => r.TryAddStockAsync(999999, 10, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _productsRepo.Setup(r => r.GetByIdAsNoTrackingAsync(999999, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var result = await _handler.Handle(new AddStockCommand(999999, 10), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.NotFound);
    }

    [Fact]
    public async Task Handle_StockWouldOverflow_ReturnsStockLimitExceeded()
    {
        _productsRepo.Setup(r => r.TryAddStockAsync(100001, int.MaxValue, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _productsRepo.Setup(r => r.GetByIdAsNoTrackingAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync(Product.Create(100001, "Mouse", null, 10m, 5));

        var result = await _handler.Handle(new AddStockCommand(100001, int.MaxValue), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.StockLimitExceeded);
        result.Error.Should().Contain("current stock is 5");
    }
}
