using System.Net;
using FluentAssertions;
using ProductCatalog.AcceptanceTests.Support;
using ProductCatalog.Application.Products;
using Reqnroll;

namespace ProductCatalog.AcceptanceTests.StepDefinitions;

[Binding]
public class CatalogueSteps
{
    private readonly CatalogueDriver _catalogue;

    public CatalogueSteps(CatalogueDriver catalogue) => _catalogue = catalogue;

    [When("a product {string} is created with price {float} and {int} unit(s) in stock")]
    public async Task WhenAProductIsCreated(string name, decimal price, int stock) =>
        _catalogue.Record(await _catalogue.CreateAsync(new { name, price, initialStock = stock }));

    [When("a product {string} is created without a price")]
    public async Task WhenAProductIsCreatedWithoutAPrice(string name) =>
        _catalogue.Record(await _catalogue.CreateAsync(new { name, initialStock = 5 }));

    [When("an unknown product is retrieved")]
    public async Task WhenAnUnknownProductIsRetrieved() =>
        _catalogue.Record(await _catalogue.GetAsync(_catalogue.UnknownProductId));

    [When("products are searched for {string}")]
    public async Task WhenProductsAreSearchedFor(string name) =>
        _catalogue.Record(await _catalogue.SearchAsync(name));

    [When("products with between {int} and {int} units in stock are requested")]
    public async Task WhenProductsWithStockBetweenAreRequested(int min, int max) =>
        _catalogue.Record(await _catalogue.GetByStockLevelAsync(min, max));

    [When("page {int} of the catalogue is requested with {int} products per page")]
    public async Task WhenAPageOfTheCatalogueIsRequested(int page, int pageSize) =>
        _catalogue.Record(await _catalogue.ListAsync(page, pageSize));

    [When("every page of the catalogue is requested with {int} products per page")]
    public async Task WhenEveryPageOfTheCatalogueIsRequested(int pageSize)
    {
        var page = 1;
        int totalPages;

        do
        {
            var response = await _catalogue.ListAsync(page, pageSize);
            await CatalogueDriver.ShouldHaveStatusAsync(response, HttpStatusCode.OK);

            var result = await CatalogueDriver.ReadPageAsync(response);
            _catalogue.BrowsedProductIds.AddRange(result.Data.Select(p => p.Id));
            totalPages = result.TotalPages;
            page++;
        }
        while (page <= totalPages);
    }

    [Then("the product is created with a 6-digit ID")]
    public async Task ThenTheProductIsCreatedWithASixDigitId()
    {
        await CatalogueDriver.ShouldHaveStatusAsync(_catalogue.LastResponse, HttpStatusCode.Created);
        _catalogue.LastResponse.Headers.Location.Should().NotBeNull();

        var product = await CatalogueDriver.ReadDataAsync<ProductDto>(_catalogue.LastResponse);
        product.Id.Should().BeInRange(100_000, 999_999);

        _catalogue.RememberProduct(product);
    }

    [Then("the request is rejected with a validation error for {string}")]
    public async Task ThenTheRequestIsRejectedWithAValidationErrorFor(string field)
    {
        await CatalogueDriver.ShouldHaveStatusAsync(_catalogue.LastResponse, HttpStatusCode.BadRequest);

        var body = await CatalogueDriver.ReadJsonAsync(_catalogue.LastResponse);
        body.GetProperty("errors").TryGetProperty(field, out _).Should().BeTrue("the response body was: {0}", body);
    }

    [Then("the request is rejected as invalid")]
    public async Task ThenTheRequestIsRejectedAsInvalid()
    {
        await CatalogueDriver.ShouldHaveStatusAsync(_catalogue.LastResponse, HttpStatusCode.BadRequest);
        _catalogue.LastResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Then("the results are:")]
    public async Task ThenTheResultsAre(DataTable expected)
    {
        await CatalogueDriver.ShouldHaveStatusAsync(_catalogue.LastResponse, HttpStatusCode.OK);

        var result = await CatalogueDriver.ReadPageAsync(_catalogue.LastResponse);
        result.Data.Select(p => p.Name).Should().BeEquivalentTo(expected.Rows.Select(row => row["name"]));
    }

    [Then("there are no results")]
    public async Task ThenThereAreNoResults()
    {
        await CatalogueDriver.ShouldHaveStatusAsync(_catalogue.LastResponse, HttpStatusCode.OK);
        (await CatalogueDriver.ReadPageAsync(_catalogue.LastResponse)).Data.Should().BeEmpty();
    }

    [Then("{int} products are returned")]
    public async Task ThenProductsAreReturned(int count) =>
        (await CatalogueDriver.ReadPageAsync(_catalogue.LastResponse)).Data.Should().HaveCount(count);

    [Then("the catalogue reports {int} products across {int} pages")]
    public async Task ThenTheCatalogueReportsTotals(int totalCount, int totalPages)
    {
        var result = await CatalogueDriver.ReadPageAsync(_catalogue.LastResponse);

        result.TotalCount.Should().Be(totalCount);
        result.TotalPages.Should().Be(totalPages);
    }

    [Then("each product appears exactly once, in order of ID")]
    public void ThenEachProductAppearsExactlyOnceInOrderOfId() =>
        _catalogue.BrowsedProductIds.Should().Equal(_catalogue.CreatedProductIds.Order());
}
