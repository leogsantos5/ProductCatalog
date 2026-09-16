using ProductCatalog.AcceptanceTests.Support;
using Reqnroll;

namespace ProductCatalog.AcceptanceTests.StepDefinitions;

[Binding]
public class StockSteps
{
    private readonly CatalogueDriver _catalogue;

    public StockSteps(CatalogueDriver catalogue) => _catalogue = catalogue;

    [Given("{int} unit(s) of {string} has/have been sold")]
    public async Task GivenUnitsHaveBeenSold(int quantity, string product) =>
        await CatalogueDriver.ShouldSucceedAsync(await _catalogue.SellAsync(_catalogue.IdOf(product), quantity));

    [When("{int} unit(s) of {string} is/are sold")]
    public async Task WhenUnitsAreSold(int quantity, string product) =>
        _catalogue.Record(await _catalogue.SellAsync(_catalogue.IdOf(product), quantity));

    [When("{int} unit(s) of an unknown product is/are sold")]
    public async Task WhenUnitsOfAnUnknownProductAreSold(int quantity) =>
        _catalogue.Record(await _catalogue.SellAsync(_catalogue.UnknownProductId, quantity));

    [When("{int} unit(s) of {string} is/are added to stock")]
    public async Task WhenUnitsAreAddedToStock(int quantity, string product) =>
        _catalogue.Record(await _catalogue.RestockAsync(_catalogue.IdOf(product), quantity));

    [When("{int} customers each buy {int} unit(s) of {string} at the same time")]
    public async Task WhenCustomersBuyAtTheSameTime(int customers, int quantity, string product)
    {
        var id = _catalogue.IdOf(product);
        _catalogue.RecordAll(await Task.WhenAll(Enumerable.Range(0, customers).Select(_ => _catalogue.SellAsync(id, quantity))));
    }

    [When("{int} deliveries each add {int} unit(s) of {string} at the same time")]
    public async Task WhenDeliveriesAddStockAtTheSameTime(int deliveries, int quantity, string product)
    {
        var id = _catalogue.IdOf(product);
        _catalogue.RecordAll(await Task.WhenAll(Enumerable.Range(0, deliveries).Select(_ => _catalogue.RestockAsync(id, quantity))));
    }
}
