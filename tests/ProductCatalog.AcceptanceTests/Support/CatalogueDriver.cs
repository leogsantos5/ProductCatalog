using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using ProductCatalog.Api.Contracts;
using ProductCatalog.Application.Products;

namespace ProductCatalog.AcceptanceTests.Support;

public sealed class CatalogueDriver
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client = TestRunHooks.Factory.CreateClient();
    private readonly Dictionary<string, int> _productIds = [];
    private readonly Dictionary<string, string> _retrievedVersions = [];

    public HttpResponseMessage LastResponse { get; private set; } = null!;
    public IReadOnlyList<HttpResponseMessage> LastResponses { get; private set; } = [];
    public List<int> BrowsedProductIds { get; } = [];

    public IReadOnlyCollection<int> CreatedProductIds => _productIds.Values;

    public int UnknownProductId => _productIds.ContainsValue(999_999) ? 999_998 : 999_999;

    public int IdOf(string product) => _productIds[product];

    public string RetrievedVersionOf(string product) => _retrievedVersions[product];

    public void Record(HttpResponseMessage response) => LastResponse = response;

    public void RecordAll(IReadOnlyList<HttpResponseMessage> responses) => LastResponses = responses;

    public void RememberProduct(ProductDto product) => _productIds[product.Name] = product.Id;

    public void RememberRetrievedVersion(string product, string etag) => _retrievedVersions[product] = etag;

    public async Task GivenProductAsync(string name, decimal price, int stock)
    {
        var response = await CreateAsync(new { name, price, initialStock = stock });
        await ShouldHaveStatusAsync(response, HttpStatusCode.Created);
        RememberProduct(await ReadDataAsync<ProductDto>(response));
    }

    public async Task<ProductDto> GetProductAsync(string product)
    {
        var response = await GetAsync(IdOf(product));
        await ShouldHaveStatusAsync(response, HttpStatusCode.OK);
        return await ReadDataAsync<ProductDto>(response);
    }

    public Task<HttpResponseMessage> CreateAsync(object body) => _client.PostAsJsonAsync("api/products", body);

    public Task<HttpResponseMessage> GetAsync(int id) => _client.GetAsync($"api/products/{id}");

    public Task<HttpResponseMessage> SellAsync(int id, int quantity) => _client.PostAsync($"api/products/{id}/decrement-stock/{quantity}", null);

    public Task<HttpResponseMessage> RestockAsync(int id, int quantity) => _client.PostAsync($"api/products/{id}/add-to-stock/{quantity}", null);

    public Task<HttpResponseMessage> ChangePriceAsync(string product, decimal price, string? ifMatch) =>
        SendAsync(HttpMethod.Put, $"api/products/{IdOf(product)}", new { name = product, description = (string?)null, price }, ifMatch);

    public Task<HttpResponseMessage> DeleteAsync(string product, string? ifMatch) =>
        SendAsync(HttpMethod.Delete, $"api/products/{IdOf(product)}", body: null, ifMatch);

    public Task<HttpResponseMessage> ListAsync(int page, int pageSize) => _client.GetAsync($"api/products?page={page}&pageSize={pageSize}");

    public Task<HttpResponseMessage> SearchAsync(string name) => _client.GetAsync($"api/products/search?name={Uri.EscapeDataString(name)}");

    public Task<HttpResponseMessage> GetByStockLevelAsync(int min, int max) => _client.GetAsync($"api/products/stock-level?min={min}&max={max}");

    public static async Task ShouldHaveStatusAsync(HttpResponseMessage response, HttpStatusCode expected) =>
        response.StatusCode.Should().Be(expected, "the response body was: {0}", await response.Content.ReadAsStringAsync());

    public static async Task ShouldSucceedAsync(HttpResponseMessage response) =>
        response.IsSuccessStatusCode.Should().BeTrue("the response was {0}: {1}", (int)response.StatusCode, await response.Content.ReadAsStringAsync());

    public static async Task<T> ReadDataAsync<T>(HttpResponseMessage response) =>
        JsonSerializer.Deserialize<ApiResponse<T>>(await response.Content.ReadAsStringAsync(), JsonOptions)!.Data;

    public static async Task<PagedApiResponse<ProductDto>> ReadPageAsync(HttpResponseMessage response) =>
        JsonSerializer.Deserialize<PagedApiResponse<ProductDto>>(await response.Content.ReadAsStringAsync(), JsonOptions)!;

    public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body, string? ifMatch)
    {
        var request = new HttpRequestMessage(method, url) { Content = body is null ? null : JsonContent.Create(body) };

        if (ifMatch is not null)
            request.Headers.TryAddWithoutValidation("If-Match", ifMatch);

        return _client.SendAsync(request);
    }
}
