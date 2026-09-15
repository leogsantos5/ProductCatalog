using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProductCatalog.Application.Common;
using ProductCatalog.Application.Common.Exceptions;
using ProductCatalog.Application.Products.Commands.DecrementStock;
using ProductCatalog.Domain.Entities;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.UnitTests.Application;

public class DecrementStockCommandHandlerTests
{
    private readonly Mock<IProductRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly DecrementStockCommandHandler _handler;

    public DecrementStockCommandHandlerTests()
    {
        _handler = new DecrementStockCommandHandler(
            _repository.Object,
            _unitOfWork.Object,
            Mock.Of<ILogger<DecrementStockCommandHandler>>());
    }

    [Fact]
    public async Task Handle_SufficientStock_DecrementsAndSaves()
    {
        var product = Product.Create(100001, "Mouse", null, 10m, 20);
        _repository.Setup(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var result = await _handler.Handle(new DecrementStockCommand(100001, 5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.StockQuantity.Should().Be(15);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsNotFound()
    {
        _repository.Setup(r => r.GetByIdAsync(999999, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var result = await _handler.Handle(new DecrementStockCommand(999999, 5), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.NotFound);
    }

    [Fact]
    public async Task Handle_InsufficientStock_ReturnsInsufficientStockFailure()
    {
        var product = Product.Create(100001, "Mouse", null, 10m, 3);
        _repository.Setup(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var result = await _handler.Handle(new DecrementStockCommand(100001, 10), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.InsufficientStock);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ConcurrencyConflictOnce_DiscardsStaleStateAndRetriesOnFreshRead()
    {
        // The second read returns a different instance, as the real repository does once the stale
        // tracked entity has been discarded — the retry must apply to that fresh state.
        var stale = Product.Create(100001, "Mouse", null, 10m, 20);
        var fresh = Product.Create(100001, "Mouse", null, 10m, 18);
        _repository.SetupSequence(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stale)
            .ReturnsAsync(fresh);

        _unitOfWork.SetupSequence(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("conflict", new Exception()))
            .ReturnsAsync(1);

        var result = await _handler.Handle(new DecrementStockCommand(100001, 5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.StockQuantity.Should().Be(13);
        _unitOfWork.Verify(u => u.DiscardChanges(), Times.Once);
        _repository.Verify(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_ProductDeletedDuringRetry_ReturnsNotFound()
    {
        var product = Product.Create(100001, "Mouse", null, 10m, 20);
        _repository.SetupSequence(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product)
            .ReturnsAsync((Product?)null);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("conflict", new Exception()));

        var result = await _handler.Handle(new DecrementStockCommand(100001, 5), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.NotFound);
    }

    [Fact]
    public async Task Handle_ConcurrencyConflictExceedsRetries_ReturnsConcurrencyFailure()
    {
        var product = Product.Create(100001, "Mouse", null, 10m, 20);
        _repository.Setup(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("conflict", new Exception()));

        var result = await _handler.Handle(new DecrementStockCommand(100001, 5), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.ConcurrencyConflict);
        _unitOfWork.Verify(u => u.DiscardChanges(), Times.Exactly(ConcurrencyPolicy.MaxConcurrencyRetries));
    }
}
