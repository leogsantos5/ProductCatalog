using System.Net;
using FluentAssertions;
using ProductCatalog.AcceptanceTests.Support;
using Reqnroll;

namespace ProductCatalog.AcceptanceTests.StepDefinitions;

[Binding]
public class EditingSteps
{
    private readonly CatalogueDriver _catalogue;

    public EditingSteps(CatalogueDriver catalogue) => _catalogue = catalogue;

    [Given("{string} has been retrieved")]
    public async Task GivenTheProductHasBeenRetrieved(string product)
    {
        var response = await _catalogue.GetAsync(_catalogue.IdOf(product));
        await CatalogueDriver.ShouldHaveStatusAsync(response, HttpStatusCode.OK);

        _catalogue.RememberRetrievedVersion(product, response.Headers.ETag!.Tag);
    }

    [Given("another user has changed the price of {string} to {float}")]
    public async Task GivenAnotherUserHasChangedThePrice(string product, decimal price) =>
        await CatalogueDriver.ShouldSucceedAsync(await _catalogue.ChangePriceAsync(product, price, ifMatch: null));

    [When("the price of {string} is changed to {float} using the retrieved version")]
    public async Task WhenThePriceIsChangedUsingTheRetrievedVersion(string product, decimal price) =>
        _catalogue.Record(await _catalogue.ChangePriceAsync(product, price, _catalogue.RetrievedVersionOf(product)));

    [When("the price of {string} is changed to {float} without a version")]
    public async Task WhenThePriceIsChangedWithoutAVersion(string product, decimal price) =>
        _catalogue.Record(await _catalogue.ChangePriceAsync(product, price, ifMatch: null));

    [When("two users change the price of {string} at the same time using the retrieved version")]
    public async Task WhenTwoUsersChangeThePriceAtTheSameTime(string product)
    {
        var version = _catalogue.RetrievedVersionOf(product);

        _catalogue.RecordAll(await Task.WhenAll(
            _catalogue.ChangePriceAsync(product, 95.00m, version),
            _catalogue.ChangePriceAsync(product, 96.00m, version)));
    }

    [When("{string} is deleted using the retrieved version")]
    public async Task WhenTheProductIsDeletedUsingTheRetrievedVersion(string product) =>
        _catalogue.Record(await _catalogue.DeleteAsync(product, _catalogue.RetrievedVersionOf(product)));

    [Then("a new version of {string} is returned")]
    public void ThenANewVersionIsReturned(string product) =>
        _catalogue.LastResponse.Headers.ETag!.Tag.Should().NotBe(_catalogue.RetrievedVersionOf(product));

    [Then("{string} still exists")]
    public async Task ThenTheProductStillExists(string product) =>
        await CatalogueDriver.ShouldHaveStatusAsync(await _catalogue.GetAsync(_catalogue.IdOf(product)), HttpStatusCode.OK);

    [Then("{string} no longer exists")]
    public async Task ThenTheProductNoLongerExists(string product) =>
        await CatalogueDriver.ShouldHaveStatusAsync(await _catalogue.GetAsync(_catalogue.IdOf(product)), HttpStatusCode.NotFound);
}
