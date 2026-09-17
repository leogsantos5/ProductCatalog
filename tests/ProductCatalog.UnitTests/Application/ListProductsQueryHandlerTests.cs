using FluentAssertions;
using Moq;
using ProductCatalog.Application.Products.Queries.ListProducts;
using ProductCatalog.Domain.Entities;
using ProductCatalog.Domain.Interfaces;

namespace ProductCatalog.UnitTests.Application;

public class ListProductsQueryHandlerTests
{
    private readonly Mock<IProductRepository> _productsRepo = new();
    private readonly ListProductsQueryHandler _handler;

    public ListProductsQueryHandlerTests()
    {
        _handler = new ListProductsQueryHandler(_productsRepo.Object);
    }

    [Fact]
    public async Task Handle_RequestedPage_ReturnsItemsWithPagingMetadata()
    {
        IReadOnlyList<Product> secondPage = [Product.Create(100003, "Lens", null, 10m, 1), Product.Create(100004, "Frame", null, 20m, 2)];
        _productsRepo.Setup(r => r.GetAllAsync(2, 2, It.IsAny<CancellationToken>())).ReturnsAsync((secondPage, 5));

        var result = await _handler.Handle(new ListProductsQuery(Page: 2, PageSize: 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Select(p => p.Id).Should().Equal(100003, 100004);
        result.Value.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(2);
        result.Value.TotalCount.Should().Be(5);
        result.Value.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task Handle_NoProducts_ReturnsEmptyPageWithZeroTotals()
    {
        _productsRepo.Setup(r => r.GetAllAsync(1, 50, It.IsAny<CancellationToken>())).ReturnsAsync(((IReadOnlyList<Product>)[], 0));

        var result = await _handler.Handle(new ListProductsQuery(), CancellationToken.None);

        result.Value!.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.TotalPages.Should().Be(0);
    }
}
