using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProductCatalog.Application.Common;
using ProductCatalog.Application.Common.Exceptions;
using ProductCatalog.Application.Products.Commands.CreateProduct;
using ProductCatalog.Domain.Entities;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.UnitTests.Application;

public class CreateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _repository = new();
    private readonly Mock<IProductIdGenerator> _idGenerator = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        _handler = new CreateProductCommandHandler(
            _repository.Object,
            _idGenerator.Object,
            _unitOfWork.Object,
            Mock.Of<ILogger<CreateProductCommandHandler>>());
    }

    [Fact]
    public async Task Handle_FirstCandidateFree_CreatesProductWithThatId()
    {
        _idGenerator.Setup(g => g.NextCandidate()).Returns(100001);
        _repository.Setup(r => r.ExistsAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var command = new CreateProductCommand("Mouse", "A mouse", 19.99m, 10);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(100001);
        result.Value.Name.Should().Be("Mouse");
        _repository.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_IdAlreadyExists_RetriesWithNextCandidate()
    {
        _idGenerator.SetupSequence(g => g.NextCandidate())
            .Returns(100001)
            .Returns(100002);

        _repository.SetupSequence(r => r.ExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        var command = new CreateProductCommand("Mouse", null, 19.99m, 10);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(100002);
        _repository.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SaveThrowsUniqueConstraintViolationOnce_RetriesAndSucceeds()
    {
        _idGenerator.SetupSequence(g => g.NextCandidate())
            .Returns(100001)
            .Returns(100002);

        _repository.Setup(r => r.ExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        _unitOfWork.SetupSequence(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintViolationException("collision", new Exception()))
            .ReturnsAsync(1);

        var command = new CreateProductCommand("Mouse", null, 19.99m, 10);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(100002);
        _repository.Verify(r => r.Remove(It.IsAny<Product>()), Times.Once);
        _repository.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_AllCandidatesCollide_ReturnsIdGenerationFailure()
    {
        _idGenerator.Setup(g => g.NextCandidate()).Returns(100001);
        _repository.Setup(r => r.ExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var command = new CreateProductCommand("Mouse", null, 19.99m, 10);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.IdGenerationFailed);
        _repository.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
