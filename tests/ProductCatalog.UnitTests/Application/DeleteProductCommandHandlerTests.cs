using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProductCatalog.Application.Common;
using ProductCatalog.Application.Common.Exceptions;
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
        _handler = new DeleteProductCommandHandler(
            _repository.Object,
            _unitOfWork.Object,
            Mock.Of<ILogger<DeleteProductCommandHandler>>());
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

    [Fact]
    public async Task Handle_ConcurrencyConflictOnce_DiscardsStaleStateAndRemovesFreshInstance()
    {
        var stale = Product.Create(100001, "Mouse", null, 10m, 5);
        var fresh = Product.Create(100001, "Mouse", null, 10m, 4);
        _repository.SetupSequence(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stale)
            .ReturnsAsync(fresh);

        _unitOfWork.SetupSequence(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("conflict", new Exception()))
            .ReturnsAsync(1);

        var result = await _handler.Handle(new DeleteProductCommand(100001), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _unitOfWork.Verify(u => u.DiscardChanges(), Times.Once);
        _repository.Verify(r => r.Remove(fresh), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductDeletedConcurrently_ReturnsNotFound()
    {
        var product = Product.Create(100001, "Mouse", null, 10m, 5);
        _repository.SetupSequence(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product)
            .ReturnsAsync((Product?)null);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("conflict", new Exception()));

        var result = await _handler.Handle(new DeleteProductCommand(100001), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.NotFound);
    }

    [Fact]
    public async Task Handle_ConcurrencyConflictExceedsRetries_ReturnsConcurrencyFailure()
    {
        var product = Product.Create(100001, "Mouse", null, 10m, 5);
        _repository.Setup(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("conflict", new Exception()));

        var result = await _handler.Handle(new DeleteProductCommand(100001), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.ConcurrencyConflict);
        _unitOfWork.Verify(u => u.DiscardChanges(), Times.Exactly(ConcurrencyPolicy.MaxConcurrencyRetries));
    }
}
