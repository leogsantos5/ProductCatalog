using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProductCatalog.Application.Common;
using ProductCatalog.Application.Common.Exceptions;
using ProductCatalog.Application.Products.Commands.UpdateProduct;
using ProductCatalog.Domain.Entities;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.UnitTests.Application;

public class UpdateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly UpdateProductCommandHandler _handler;

    private static readonly byte[] CurrentRowVersion = [0, 0, 0, 0, 0, 0, 7, 209];
    private static readonly byte[] NewerRowVersion = [0, 0, 0, 0, 0, 0, 7, 210];
    private const string CurrentVersion = "AAAAAAAAB9E=";
    private const string StaleVersion = "AAAAAAAAB9A=";

    public UpdateProductCommandHandlerTests()
    {
        _handler = new UpdateProductCommandHandler(
            _repository.Object,
            _unitOfWork.Object,
            Mock.Of<ILogger<UpdateProductCommandHandler>>());
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

    [Fact]
    public async Task Handle_ConcurrencyConflictOnce_DiscardsStaleStateAndRetriesOnFreshRead()
    {
        // e.g. a stock change landed between the read and the save — the update is reapplied on top
        // of it, keeping the fresh stock quantity.
        var stale = Product.Create(100001, "Mouse", "Old", 10m, 5);
        var fresh = Product.Create(100001, "Mouse", "Old", 10m, 4);
        _repository.SetupSequence(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stale)
            .ReturnsAsync(fresh);

        _unitOfWork.SetupSequence(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("conflict", new Exception()))
            .ReturnsAsync(1);

        var result = await _handler.Handle(new UpdateProductCommand(100001, "Wireless Mouse", "New", 25m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Wireless Mouse");
        result.Value.StockQuantity.Should().Be(4);
        _unitOfWork.Verify(u => u.DiscardChanges(), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductDeletedDuringRetry_ReturnsNotFound()
    {
        var product = Product.Create(100001, "Mouse", "Old", 10m, 5);
        _repository.SetupSequence(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product)
            .ReturnsAsync((Product?)null);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("conflict", new Exception()));

        var result = await _handler.Handle(new UpdateProductCommand(100001, "Wireless Mouse", "New", 25m), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.NotFound);
    }

    [Fact]
    public async Task Handle_ConcurrencyConflictExceedsRetries_ReturnsConcurrencyFailure()
    {
        var product = Product.Create(100001, "Mouse", "Old", 10m, 5);
        _repository.Setup(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("conflict", new Exception()));

        var result = await _handler.Handle(new UpdateProductCommand(100001, "Wireless Mouse", "New", 25m), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.ConcurrencyConflict);
        _unitOfWork.Verify(u => u.DiscardChanges(), Times.Exactly(ConcurrencyPolicy.MaxConcurrencyRetries));
    }

    [Fact]
    public async Task Handle_ExpectedVersionMatches_UpdatesAndSaves()
    {
        var product = Product.Create(100001, "Mouse", "Old", 10m, 5).WithRowVersion(CurrentRowVersion);
        _repository.Setup(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var result = await _handler.Handle(new UpdateProductCommand(100001, "Wireless Mouse", "New", 25m, CurrentVersion), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExpectedVersionIsStale_ReturnsVersionMismatchWithoutSaving()
    {
        var product = Product.Create(100001, "Mouse", "Old", 10m, 5).WithRowVersion(CurrentRowVersion);
        _repository.Setup(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var result = await _handler.Handle(new UpdateProductCommand(100001, "Wireless Mouse", "New", 25m, StaleVersion), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.VersionMismatch);
        product.Name.Should().Be("Mouse");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ConcurrencyConflictWithExpectedVersion_ReturnsVersionMismatchInsteadOfRetrying()
    {
        // The version matched on read, but another write landed before the save.
        _repository.SetupSequence(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Product.Create(100001, "Mouse", "Old", 10m, 5).WithRowVersion(CurrentRowVersion))
            .ReturnsAsync(Product.Create(100001, "Mouse", "Old", 10m, 4).WithRowVersion(NewerRowVersion));

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("conflict", new Exception()));

        var result = await _handler.Handle(new UpdateProductCommand(100001, "Wireless Mouse", "New", 25m, CurrentVersion), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.VersionMismatch);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.DiscardChanges(), Times.Once);
    }
}
