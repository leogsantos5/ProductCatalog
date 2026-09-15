#:property PublishAot=false
// End-to-end checks against a running API, with genuinely parallel requests.
// Start the API (dotnet run --project src/ProductCatalog.Api), then in a second terminal:
//   dotnet run tests/smoke/api-smoke-test.cs [base-url]      (default http://localhost:5077/)
// Uses seeded product 100001: its stock ends where it started, PUTs resend its current values,
// and the product created here is deleted at the end.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

const int id = 100001;
var http = new HttpClient { BaseAddress = new Uri(args.Length > 0 ? args[0] : "http://localhost:5077/") };
var failures = 0;

void Check(string name, bool ok, string details = "")
{
    Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {name}{(details.Length > 0 ? $"  [{details}]" : "")}");
    if (!ok) failures++;
}

string Codes(IEnumerable<HttpResponseMessage> responses) =>
    string.Join(", ", responses.GroupBy(r => (int)r.StatusCode).Select(g => $"{g.Key} x{g.Count()}"));

async Task<(int Stock, string ETag, JsonElement Data)> GetProduct()
{
    var response = await http.GetAsync($"api/products/{id}");
    response.EnsureSuccessStatusCode();
    var data = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
    return (data.GetProperty("stockQuantity").GetInt32(), response.Headers.ETag!.Tag, data);
}

Task<HttpResponseMessage> Send(HttpMethod method, string url, object? body = null, string? ifMatch = null)
{
    var request = new HttpRequestMessage(method, url) { Content = body is null ? null : JsonContent.Create(body) };
    if (ifMatch is not null)
        request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
    return http.SendAsync(request);
}

// --- Stock: atomic updates under real parallelism ---------------------------------------------
if ((await GetProduct()).Stock < 60)
    await Send(HttpMethod.Post, $"api/products/{id}/add-to-stock/60");

var start = await GetProduct();

var decrements = await Task.WhenAll(Enumerable.Range(0, 50).Select(_ => Send(HttpMethod.Post, $"api/products/{id}/decrement-stock/1")));
var afterDecrements = await GetProduct();
Check("50 parallel decrements all return 200", decrements.All(r => r.StatusCode == HttpStatusCode.OK), Codes(decrements));
Check("stock dropped by exactly 50", afterDecrements.Stock == start.Stock - 50, $"{start.Stock} -> {afterDecrements.Stock}");

var additions = await Task.WhenAll(Enumerable.Range(0, 50).Select(_ => Send(HttpMethod.Post, $"api/products/{id}/add-to-stock/1")));
var afterAdditions = await GetProduct();
Check("50 parallel additions all return 200", additions.All(r => r.StatusCode == HttpStatusCode.OK), Codes(additions));
Check("stock back to where it started", afterAdditions.Stock == start.Stock, $"{afterDecrements.Stock} -> {afterAdditions.Stock}");

var tooMuch = await Send(HttpMethod.Post, $"api/products/{id}/decrement-stock/{afterAdditions.Stock + 1}");
Check("decrementing more than available returns 400", tooMuch.StatusCode == HttpStatusCode.BadRequest, ((int)tooMuch.StatusCode).ToString());
Check("failed decrement leaves stock untouched", (await GetProduct()).Stock == afterAdditions.Stock);

var exactly = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Send(HttpMethod.Post, $"api/products/{id}/decrement-stock/{afterAdditions.Stock}")));
Check("5 parallel decrements of the whole stock: exactly one succeeds", exactly.Count(r => r.StatusCode == HttpStatusCode.OK) == 1, Codes(exactly));
Check("stock never goes negative", (await GetProduct()).Stock == 0);
await Send(HttpMethod.Post, $"api/products/{id}/add-to-stock/{afterAdditions.Stock}");

var unknownStock = await Send(HttpMethod.Post, "api/products/999999/decrement-stock/1");
Check("stock change on unknown product returns 404", unknownStock.StatusCode == HttpStatusCode.NotFound, ((int)unknownStock.StatusCode).ToString());

var overflow = await Send(HttpMethod.Post, $"api/products/{id}/add-to-stock/{int.MaxValue}");
var overflowJson = await overflow.Content.ReadFromJsonAsync<JsonElement>();
Check("adding stock past int.MaxValue returns 400 STOCK_LIMIT_EXCEEDED",
    overflow.StatusCode == HttpStatusCode.BadRequest && overflowJson.GetProperty("errorCode").GetString() == "STOCK_LIMIT_EXCEEDED",
    ((int)overflow.StatusCode).ToString());
Check("rejected addition leaves stock untouched", (await GetProduct()).Stock == afterAdditions.Stock);

// --- PUT / DELETE with optional If-Match ------------------------------------------------------
var product = await GetProduct();
var body = new
{
    name = product.Data.GetProperty("name").GetString(),
    description = product.Data.GetProperty("description").GetString(),
    price = product.Data.GetProperty("price").GetDecimal()
};

var putCurrent = await Send(HttpMethod.Put, $"api/products/{id}", body, product.ETag);
Check("PUT with current ETag returns 200 and a new ETag",
    putCurrent.StatusCode == HttpStatusCode.OK && putCurrent.Headers.ETag?.Tag is { } newTag && newTag != product.ETag,
    ((int)putCurrent.StatusCode).ToString());

var putStale = await Send(HttpMethod.Put, $"api/products/{id}", body, product.ETag);
Check("PUT with stale ETag returns 412", putStale.StatusCode == HttpStatusCode.PreconditionFailed, ((int)putStale.StatusCode).ToString());

var putWithoutIfMatch = await Send(HttpMethod.Put, $"api/products/{id}", body);
Check("PUT without If-Match returns 200 (last write wins)", putWithoutIfMatch.StatusCode == HttpStatusCode.OK, ((int)putWithoutIfMatch.StatusCode).ToString());

var current = await GetProduct();
var race = await Task.WhenAll(
    Send(HttpMethod.Put, $"api/products/{id}", body, current.ETag),
    Send(HttpMethod.Put, $"api/products/{id}", body, current.ETag));
Check("two simultaneous PUTs with the same ETag: one 200, one 412",
    race.Count(r => r.StatusCode == HttpStatusCode.OK) == 1 && race.Count(r => r.StatusCode == HttpStatusCode.PreconditionFailed) == 1,
    Codes(race));

var deleteStale = await Send(HttpMethod.Delete, $"api/products/{id}", ifMatch: product.ETag);
Check("DELETE with stale ETag returns 412", deleteStale.StatusCode == HttpStatusCode.PreconditionFailed, ((int)deleteStale.StatusCode).ToString());
Check("product still exists after the rejected DELETE", (await http.GetAsync($"api/products/{id}")).StatusCode == HttpStatusCode.OK);

// --- Create, validation, errors ---------------------------------------------------------------
var created = await http.PostAsJsonAsync("api/products", new { name = "Smoke Test Lens", price = 10.50m, initialStock = 5 });
Check("POST returns 201 with Location and ETag",
    created.StatusCode == HttpStatusCode.Created && created.Headers.Location is not null && created.Headers.ETag is not null,
    ((int)created.StatusCode).ToString());

if (created.StatusCode == HttpStatusCode.Created)
{
    var createdId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("id").GetInt32();
    var deleteCreated = await Send(HttpMethod.Delete, $"api/products/{createdId}", ifMatch: created.Headers.ETag!.Tag);
    Check("DELETE with current ETag returns 204", deleteCreated.StatusCode == HttpStatusCode.NoContent, ((int)deleteCreated.StatusCode).ToString());
}

var missingPrice = await http.PostAsJsonAsync("api/products", new { name = "No Price", initialStock = 5 });
Check("POST without price returns 400 problem+json",
    missingPrice.StatusCode == HttpStatusCode.BadRequest && missingPrice.Content.Headers.ContentType?.MediaType == "application/problem+json",
    $"{(int)missingPrice.StatusCode} {missingPrice.Content.Headers.ContentType?.MediaType}");

var invalidPrice = await http.PostAsJsonAsync("api/products", new { name = "Bad Price", price = 19.999m, initialStock = 5 });
var invalidPriceJson = await invalidPrice.Content.ReadFromJsonAsync<JsonElement>();
Check("POST with 3-decimal price returns 400 with errors keyed by field",
    invalidPrice.StatusCode == HttpStatusCode.BadRequest && invalidPriceJson.GetProperty("errors").TryGetProperty("Price", out _),
    $"{(int)invalidPrice.StatusCode}");

var notFound = await http.GetAsync("api/products/999999");
var notFoundJson = await notFound.Content.ReadFromJsonAsync<JsonElement>();
Check("GET unknown product returns 404 with errorCode NOT_FOUND",
    notFound.StatusCode == HttpStatusCode.NotFound && notFoundJson.GetProperty("errorCode").GetString() == "NOT_FOUND",
    ((int)notFound.StatusCode).ToString());

var wildcardSearch = await http.GetFromJsonAsync<JsonElement>("api/products/search?name=%25");
Check("search for \"%\" is literal (no products match)", wildcardSearch.GetProperty("data").GetArrayLength() == 0);

var missingMax = await http.GetAsync("api/products/stock-level?min=0");
Check("stock-level without max returns 400", missingMax.StatusCode == HttpStatusCode.BadRequest, ((int)missingMax.StatusCode).ToString());

// --- Paging ------------------------------------------------------------------------------------
List<int> Ids(JsonElement page) => page.GetProperty("data").EnumerateArray().Select(p => p.GetProperty("id").GetInt32()).ToList();

var firstPage = await http.GetFromJsonAsync<JsonElement>("api/products?page=1&pageSize=2");
var totalCount = firstPage.GetProperty("totalCount").GetInt32();
Check("list returns the requested page with paging metadata",
    Ids(firstPage).Count == Math.Min(2, totalCount) && firstPage.GetProperty("page").GetInt32() == 1
    && firstPage.GetProperty("pageSize").GetInt32() == 2 && firstPage.GetProperty("totalPages").GetInt32() == (totalCount + 1) / 2,
    $"totalCount {totalCount}");

var secondPage = await http.GetFromJsonAsync<JsonElement>("api/products?page=2&pageSize=2");
var bothPages = Ids(firstPage).Concat(Ids(secondPage)).ToList();
Check("consecutive pages don't overlap and are ordered by ID",
    bothPages.Distinct().Count() == bothPages.Count && bothPages.SequenceEqual(bothPages.Order()),
    string.Join(", ", bothPages));

var defaultPage = await http.GetFromJsonAsync<JsonElement>("api/products");
Check("list defaults to page 1 and pageSize 50",
    defaultPage.GetProperty("page").GetInt32() == 1 && defaultPage.GetProperty("pageSize").GetInt32() == 50);

var pageSizeTooBig = await http.GetAsync("api/products?pageSize=101");
Check("pageSize above 100 returns 400", pageSizeTooBig.StatusCode == HttpStatusCode.BadRequest, ((int)pageSizeTooBig.StatusCode).ToString());

var pageZero = await http.GetAsync("api/products/search?name=lens&page=0");
Check("page 0 returns 400", pageZero.StatusCode == HttpStatusCode.BadRequest, ((int)pageZero.StatusCode).ToString());

Console.WriteLine();
Console.WriteLine(failures == 0 ? "All checks passed." : $"{failures} check(s) failed.");
return failures == 0 ? 0 : 1;
