# Product Catalog API

A REST API for managing products: CRUD, stock management, and search by name or stock level. Built
with ASP.NET Core and EF Core (code-first, SQL Server) as a take-home assessment.

## Tech stack

- .NET 10, ASP.NET Core Web API (controllers)
- Entity Framework Core 10 with SQL Server, code-first migrations
- MediatR (CQRS) and FluentValidation
- xUnit, Moq and FluentAssertions; Reqnroll (BDD) with `WebApplicationFactory`
- Swagger UI (Swashbuckle)

## Architecture

Clean Architecture in four projects, with dependencies pointing inwards:
`Api → Application → Domain` and `Infrastructure → Application → Domain`.

```
src/
├── ProductCatalog.Domain          Product entity; repository, unit-of-work and ID-generator
│                                  interfaces. No package dependencies.
├── ProductCatalog.Application     Use cases (MediatR commands/queries and handlers),
│                                  validators, Result<T>, DTOs.
├── ProductCatalog.Infrastructure  EF Core DbContext, migrations, repository, unit of work,
│                                  ID generator, seeding.
└── ProductCatalog.Api             Controller, request contracts, error handling, Swagger.
tests/
├── ProductCatalog.UnitTests         Fast tests with mocks: domain, handlers, validators, API helpers.
└── ProductCatalog.AcceptanceTests   BDD scenarios (Reqnroll) against the real API and SQL Server.
```

Each use case has its own folder under `Application/Products/Commands` or `Queries`, holding the
command or query, its handler and its validator. Handlers only depend on `IProductRepository` and
`IUnitOfWork`; EF Core stays inside `Infrastructure`.

## Running locally

**Prerequisites:** .NET 10 SDK and SQL Server LocalDB (included with Visual Studio, or available as
the standalone [SQL Server Express LocalDB](https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb) installer).

```bash
dotnet run --project src/ProductCatalog.Api
```

Swagger UI is at `http://localhost:5077/swagger`, and
`src/ProductCatalog.Api/ProductCatalog.Api.http` has a ready-to-run request for every endpoint.

In the `Development` environment (the default for `dotnet run`) the API applies pending migrations
and seeds sample products on startup, so a fresh clone needs nothing else. Outside `Development`
this is skipped on purpose: migrations should be an explicit deployment step.

```bash
dotnet tool install --global dotnet-ef
dotnet ef database update --project src/ProductCatalog.Infrastructure --startup-project src/ProductCatalog.Api
```

The connection string is `ConnectionStrings:Default` in `src/ProductCatalog.Api/appsettings.json`.
It points at LocalDB with Windows authentication, so there are no secrets.

### Tests

```bash
dotnet test
```

This runs both test projects. The acceptance tests need SQL Server LocalDB: they create their own
`ProductCatalog_AcceptanceTests` database, empty it before every scenario and drop it at the end,
so the development database is never touched.

**Unit tests** (`tests/ProductCatalog.UnitTests`) run in memory, without a database:

- **Domain:** `Product` rules (6-digit ID, name, positive price, non-negative initial stock).
- **Handlers**, with the repository and unit of work mocked:
  - create: ID already taken at the pre-check or at insert time, and running out of attempts;
  - update and delete: retry on a concurrency conflict using a fresh read, product deleted during
    the retry, retries exhausted, and `If-Match` (matching version, stale version, and a conflict
    after the check, which must end in 412 rather than overwrite);
  - stock: success, insufficient stock, stock limit exceeded and unknown product;
  - get by ID, and the paging metadata of the product list.
- **Validators:** every rule, including price precision, `min <= max` and paging bounds.
- **API:** error code to HTTP status mapping, `ETag`/`If-Match` parsing, required JSON fields.
- **Infrastructure:** random ID range and escaping of the search pattern.

**Acceptance tests** (`tests/ProductCatalog.AcceptanceTests`) are BDD scenarios written in Gherkin
(Given/When/Then) and run with Reqnroll. They start the real API in memory with
`WebApplicationFactory`, so every request goes through routing, validation, EF Core and SQL Server.
That covers what unit tests can't: the atomic stock SQL, `rowversion` conflicts, and requests that
genuinely run at the same time.

```gherkin
Scenario: Simultaneous sales never oversell
  Given a product "Lens" with 5 units in stock
  When 5 customers each buy 5 units of "Lens" at the same time
  Then 1 request succeeds and 4 are rejected with error code "INSUFFICIENT_STOCK"
  And "Lens" has 0 units in stock
```

| Feature | Scenarios |
|---|---|
| `StockManagement` | Selling and restocking; selling more than is available (400); 50 simultaneous sales and 50 simultaneous deliveries all counted; 5 buyers competing for the whole stock (exactly one succeeds, stock never negative); restocking past `int.MaxValue` (400); unknown product (404) |
| `ConcurrentEditing` | Saving with an up-to-date version; saving over someone else's change (412); a sale also makes the retrieved version out of date (412); two users saving at the same moment (one succeeds, one 412); saving without a version (last write wins); deleting with a stale (412) or current version |
| `ProductCatalogue` | Creating a product (201, 6-digit ID); invalid name, price or stock (400 for that field); missing price (400); unknown product (404); partial, case-insensitive search; `%` matched literally; filtering by stock level, and `min` above `max` (400); paging totals, every product exactly once across pages, page size above 100 (400) |

## API

| Method | Route | Description | Success | Errors |
|---|---|---|---|---|
| GET | `/api/products` | List products (paged) | 200 | 400 |
| GET | `/api/products/{id}` | Get a product | 200 | 404 |
| POST | `/api/products` | Create a product | 201 | 400, 500 (no free ID found) |
| PUT | `/api/products/{id}` | Update name, description and price | 200 | 400, 404, 409, 412 |
| DELETE | `/api/products/{id}` | Delete a product | 204 | 404, 409, 412 |
| POST | `/api/products/{id}/decrement-stock/{quantity}` | Remove stock | 200 | 400 (invalid quantity or insufficient stock), 404 |
| POST | `/api/products/{id}/add-to-stock/{quantity}` | Add stock | 200 | 400 (invalid quantity or stock limit exceeded), 404 |
| GET | `/api/products/search?name={name}` | Partial, case-insensitive name match | 200 | 400 |
| GET | `/api/products/stock-level?min={min}&max={max}` | Products with stock between `min` and `max`, inclusive | 200 | 400 |

Successful responses are wrapped as `{ "data": ... }`, and every product includes its
`stockQuantity`. The three list endpoints are paged: they take optional `page` (default 1) and
`pageSize` (default 50, maximum 100) query parameters, order results by ID, and return `page`,
`pageSize`, `totalCount` and `totalPages` alongside `data`. Errors are [Problem Details](https://www.rfc-editor.org/rfc/rfc9457) responses:

- **400 validation errors** list the problems per field in `errors`, whether they come from a
  validator or from a missing or malformed field in the request.
- **Business errors** include an `errorCode` (`NOT_FOUND`, `INSUFFICIENT_STOCK`,
  `STOCK_LIMIT_EXCEEDED`, `CONCURRENCY_CONFLICT`, `VERSION_MISMATCH`, `ID_GENERATION_FAILED`) and a
  readable `detail`.

Every product carries a `version`, and responses for a single product also return it as an `ETag`
header. `PUT` and `DELETE` accept it back in an optional `If-Match` header (see Concurrency below).

## Design decisions

- **Clean Architecture and CQRS for a single entity.** This is more structure than a CRUD this
  size strictly needs, chosen because the brief asks for professional standards. Each use case is
  one small command or query with its own handler and validator, so a change stays in one folder
  and handlers can be unit-tested without HTTP or a database. MediatR also provides a single place
  for steps every request goes through (logging, validation).

- **Controllers rather than Minimal APIs.** The nine endpoints sit in one attribute-routed
  controller. It has no logic of its own: each action sends one MediatR request and maps the
  result to an HTTP response.

- **Unique 6-digit IDs across instances.** The application picks a random ID between 100000 and
  999999. The database primary key is what guarantees uniqueness: if two instances pick the same
  ID at the same moment, the second insert fails, and `CreateProductCommandHandler` retries with a
  new ID (up to 10 attempts). A quick existence check before inserting avoids that failed insert in
  the usual case.

  Six digits allow 900,000 IDs, so collisions become more likely as the table fills up. That is far
  beyond the scale of this exercise. A database sequence mapped onto the 6-digit range was also
  considered. It needs no retries, but it runs out for good after 900,000 inserts, even if most of
  those products were deleted. Random IDs only get slower as the *live* ID space fills.

- **Concurrency.** The brief doesn't ask for it, but several instances writing the same product at
  once must not lose updates. Two techniques are used, depending on the kind of write:

  - **Stock changes are a single atomic `UPDATE`.** `decrement-stock` and `add-to-stock` are
    relative changes, so the database applies them directly
    (`SET StockQuantity = StockQuantity - @quantity WHERE Id = @id AND StockQuantity >= @quantity`,
    via `ExecuteUpdateAsync`). Nothing is read first, so there is nothing to go stale: no retries,
    no conflicts, and stock can never go negative, however many requests arrive together. Additions
    work the same way: the `WHERE` clause also requires `StockQuantity <= int.MaxValue - @quantity`,
    so an addition that would overflow matches no row and returns a 400 instead of failing in SQL.
  - **`PUT` and `DELETE` use optimistic concurrency.** `Product` has a SQL Server `rowversion`
    column that changes on every write, including the atomic stock updates, exposed as the
    product's `ETag`. The risk here isn't the milliseconds inside the server but the minutes a user
    spends on an edit screen: two back-office users open the same product, and whoever saves second
    silently overwrites the other's change. Sending the `ETag` in `If-Match` prevents that: if the
    product changed since it was read, the request fails with **412 Precondition Failed** and the
    client can reload before trying again.

    `If-Match` is optional. Without it, the last write wins: if the row changes between the
    server's read and save, the handler reloads the product and retries (up to 3 attempts, then
    409), clearing EF Core's change tracker first so it doesn't reuse the stale entity.

- **Status codes.** Asking for more stock than is available, or pushing it past `int.MaxValue`, returns 400, because the request is
  invalid for the product's current state. 412 means the client's `If-Match` version is out of
  date; 409 means a request without `If-Match` still conflicted after the retries. An unknown ID returns 404 on every endpoint.

- **`Result<T>` for expected failures.** Not found, insufficient stock and exhausted retries are
  returned as results with an error code instead of being thrown. Exceptions are reserved for
  validation failures (raised by the MediatR validation behaviour) and unexpected errors. A global
  `IExceptionHandler` turns both into Problem Details.

- **Validation happens before the handler and matches the database.** Request contracts mark
  required fields `[JsonRequired]`, so a missing `price` or `initialStock` is rejected instead of
  silently binding to 0. A MediatR pipeline behaviour then runs the FluentValidation validators for
  every request, so handlers only ever see valid input. It throws on failure rather than returning a
  `Result`, because a behaviour shared by every request type can't build each one's typed result;
  the global exception handler turns it into a 400. The rules mirror the schema: name up to 200
  characters, description up to 1000, and a positive price with at most two decimals, since the
  `decimal(18,2)` column would otherwise silently round `19.999` to `20.00`.

- **A rich entity, with stock as the deliberate exception.** `Product` has private setters and a
  `Create` factory, so it can't be built in an invalid state. The "stock never below zero" rule is
  the exception: it lives in the atomic `UPDATE`'s `WHERE` clause rather than in an entity method,
  because checking it in memory would bring back the read-then-write race. Correctness under
  concurrency was put ahead of keeping every rule inside the entity.

- **EF Core stays in Infrastructure.** `UnitOfWork` translates EF Core's concurrency and
  duplicate-key exceptions into the Application's own `ConcurrencyConflictException` and
  `UniqueConstraintViolationException`, so handlers can retry without referencing EF Core.
  Timestamps are written as UTC, and a value converter marks them as UTC when read back: `datetime2`
  doesn't store that, so they would otherwise be serialised without the `Z`.

- **HTTP concerns stay in the Api.** Handlers return error codes, not status codes.
  `ErrorCodeMapper` translates them in one tested place, and an unknown code becomes a 500 instead
  of being mistaken for a client error. Responses use DTOs mapped by hand (a few lines, checked at
  compile time) rather than exposing the entity or adding a mapping library. Payloads are wrapped in
  `{ "data": ... }` so a response can gain fields without breaking clients, which is how the paged
  endpoints add their paging metadata.

- **Paging on every list endpoint.** The brief defines the list routes without paging, so paging is
  optional and the routes are unchanged. A default page size of 50 keeps a plain `GET` cheap as the
  catalogue grows, and the maximum of 100 caps any single response. Results are ordered by ID so
  pages are stable, and the total comes from a separate `COUNT` query.

- **Name search.** Uses `LIKE '%name%'`; SQL Server's default collation makes it case-insensitive.
  `%`, `_` and `[` in the search text are escaped, so they're matched literally. A leading wildcard
  can't use an index, which is fine at this size; a large catalogue would call for full-text search.

- **Two layers of tests.** Unit tests pin down the business rules and retry logic quickly, using
  mocks. The BDD acceptance tests (the brief's optional item) describe the expected outcomes in plain
  language and check them against the real API and SQL Server, which is the only way to prove the
  concurrency behaviour. They use LocalDB like the rest of the project; a CI pipeline would point
  them at a containerised SQL Server instead.

- **Pinned package versions.** MediatR stays on 12.x and FluentAssertions on 7.x, the last releases
  before both moved to commercial licensing.

## Known limitations

- There is no authentication or authorisation, as the brief doesn't ask for it.
- `If-Match` supports a single strong ETag. Weak ETags or a list of ETags never match, so they get a 412.
- Without `If-Match`, the last `PUT` wins.
- Paging uses `OFFSET`/`FETCH`, which slows down on very deep pages; a very large catalogue would
  call for keyset (cursor) paging.
