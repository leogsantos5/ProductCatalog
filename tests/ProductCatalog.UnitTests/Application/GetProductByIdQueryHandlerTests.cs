using FluentAssertions;
using Moq;
using ProductCatalog.Application.Common;
using ProductCatalog.Application.Products.Queries.GetProductById;
using ProductCatalog.Domain.Entities;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.UnitTests.Application;

public class GetProductByIdQueryHandlerTests
{
    private readonly Mock<IProductRepository> _repository = new();
    private readonly GetProductByIdQueryHandler _handler;

    public GetProductByIdQueryHandlerTests()
    {
        _handler = new GetProductByIdQueryHandler(_repository.Object);
    }

    [Fact]
    public async Task Handle_ExistingProduct_ReturnsProduct()
    {
        var product = Product.Create(100001, "Mouse", "A mouse", 19.99m, 10);
        _repository.Setup(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var result = await _handler.Handle(new GetProductByIdQuery(100001), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(100001);
        result.Value.Name.Should().Be("Mouse");
        result.Value.StockQuantity.Should().Be(10);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsNotFound()
    {
        _repository.Setup(r => r.GetByIdAsync(999999, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var result = await _handler.Handle(new GetProductByIdQuery(999999), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.NotFound);
    }
}
