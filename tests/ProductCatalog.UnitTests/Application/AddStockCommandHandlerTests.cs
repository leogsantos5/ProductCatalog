using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProductCatalog.Application.Common;
using ProductCatalog.Application.Common.Exceptions;
using ProductCatalog.Application.Products.Commands.AddStock;
using ProductCatalog.Domain.Entities;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.UnitTests.Application;

public class AddStockCommandHandlerTests
{
    private readonly Mock<IProductRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly AddStockCommandHandler _handler;

    public AddStockCommandHandlerTests()
    {
        _handler = new AddStockCommandHandler(
            _repository.Object,
            _unitOfWork.Object,
            Mock.Of<ILogger<AddStockCommandHandler>>());
    }

    [Fact]
    public async Task Handle_ExistingProduct_AddsStockAndSaves()
    {
        var product = Product.Create(100001, "Mouse", null, 10m, 5);
        _repository.Setup(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var result = await _handler.Handle(new AddStockCommand(100001, 10), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.StockQuantity.Should().Be(15);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsNotFound()
    {
        _repository.Setup(r => r.GetByIdAsync(999999, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var result = await _handler.Handle(new AddStockCommand(999999, 10), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.NotFound);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ConcurrencyConflictOnce_DiscardsStaleStateAndRetriesOnFreshRead()
    {
        var stale = Product.Create(100001, "Mouse", null, 10m, 5);
        var fresh = Product.Create(100001, "Mouse", null, 10m, 8);
        _repository.SetupSequence(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stale)
            .ReturnsAsync(fresh);

        _unitOfWork.SetupSequence(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("conflict", new Exception()))
            .ReturnsAsync(1);

        var result = await _handler.Handle(new AddStockCommand(100001, 10), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.StockQuantity.Should().Be(18);
        _unitOfWork.Verify(u => u.DiscardChanges(), Times.Once);
        _repository.Verify(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_ConcurrencyConflictExceedsRetries_ReturnsConcurrencyFailure()
    {
        var product = Product.Create(100001, "Mouse", null, 10m, 5);
        _repository.Setup(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("conflict", new Exception()));

        var result = await _handler.Handle(new AddStockCommand(100001, 10), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.ConcurrencyConflict);
        _unitOfWork.Verify(u => u.DiscardChanges(), Times.Exactly(ConcurrencyPolicy.MaxConcurrencyRetries));
    }
}
