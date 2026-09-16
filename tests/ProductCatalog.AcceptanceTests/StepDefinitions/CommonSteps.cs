using System.Globalization;
using System.Net;
using FluentAssertions;
using ProductCatalog.AcceptanceTests.Support;
using Reqnroll;

namespace ProductCatalog.AcceptanceTests.StepDefinitions;

[Binding]
public class CommonSteps
{
    private readonly CatalogueDriver _catalogue;

    public CommonSteps(CatalogueDriver catalogue) => _catalogue = catalogue;

    [Given("a product {string} with {int} unit(s) in stock")]
    public Task GivenAProductWithStock(string name, int stock) => _catalogue.GivenProductAsync(name, 10.00m, stock);

    [Given("a product {string} priced {float}")]
    public Task GivenAProductPriced(string name, decimal price) => _catalogue.GivenProductAsync(name, price, 10);

    [Given("the following products exist:")]
    public async Task GivenTheFollowingProductsExist(DataTable table)
    {
        foreach (var row in table.Rows)
            await _catalogue.GivenProductAsync(row["name"], decimal.Parse(row["price"], CultureInfo.InvariantCulture), int.Parse(row["stock"]));
    }

    [Given("{int} products exist")]
    public async Task GivenProductsExist(int count)
    {
        for (var i = 1; i <= count; i++)
            await _catalogue.GivenProductAsync($"Product {i}", 10.00m, 1);
    }

    [Then("the request succeeds")]
    public Task ThenTheRequestSucceeds() => CatalogueDriver.ShouldSucceedAsync(_catalogue.LastResponse);

    [Then("the request is rejected with status {int} and error code {string}")]
    public async Task ThenTheRequestIsRejectedWithErrorCode(int status, string errorCode)
    {
        await CatalogueDriver.ShouldHaveStatusAsync(_catalogue.LastResponse, (HttpStatusCode)status);

        var body = await CatalogueDriver.ReadJsonAsync(_catalogue.LastResponse);
        body.GetProperty("errorCode").GetString().Should().Be(errorCode);
    }

    [Then("all {int} requests succeed")]
    public async Task ThenAllRequestsSucceed(int count)
    {
        _catalogue.LastResponses.Should().HaveCount(count);

        foreach (var response in _catalogue.LastResponses)
            await CatalogueDriver.ShouldSucceedAsync(response);
    }

    [Then("{int} request(s) succeed(s) and {int} is/are rejected with error code {string}")]
    public async Task ThenSomeRequestsSucceedAndTheRestAreRejected(int succeeded, int rejected, string errorCode)
    {
        var statusCodes = string.Join(", ", _catalogue.LastResponses.Select(r => (int)r.StatusCode));
        var failures = _catalogue.LastResponses.Where(r => !r.IsSuccessStatusCode).ToList();

        _catalogue.LastResponses.Count(r => r.IsSuccessStatusCode).Should().Be(succeeded, "the status codes were {0}", statusCodes);
        failures.Should().HaveCount(rejected, "the status codes were {0}", statusCodes);

        foreach (var failure in failures)
            (await CatalogueDriver.ReadJsonAsync(failure)).GetProperty("errorCode").GetString().Should().Be(errorCode);
    }

    [Then("{string} has {int} unit(s) in stock")]
    public async Task ThenTheProductHasStock(string product, int stock) =>
        (await _catalogue.GetProductAsync(product)).StockQuantity.Should().Be(stock);

    [Then("{string} is priced {float}")]
    public async Task ThenTheProductIsPriced(string product, decimal price) =>
        (await _catalogue.GetProductAsync(product)).Price.Should().Be(price);
}
