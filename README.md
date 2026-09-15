# Product Catalog API

A REST API for managing products, built as a take-home technical assessment. It exposes standard
CRUD for a `Product` entity plus stock management and search endpoints, backed by EF Core
(code-first) against SQL Server.

## Tech stack

- .NET 10 / ASP.NET Core Web API (Controllers)
- Entity Framework Core 10 (SQL Server, code-first migrations)
- MediatR (CQRS) + FluentValidation
- xUnit + Moq + FluentAssertions
- Swagger / OpenAPI (Swashbuckle)

## Architecture

Clean Architecture in four projects, following the dependency rule `Api → Application → Domain`
and `Infrastructure → Application → Domain` (Domain has no dependency on anything else):

```
src/
├── ProductCatalog.Domain          Product entity (rich model, no setters exposed), repository/
│                                  unit-of-work/ID-generator interfaces. Zero package dependencies.
├── ProductCatalog.Application     CQRS use cases (MediatR commands/queries + handlers),
│                                  FluentValidation validators, Result<T>, DTOs.
├── ProductCatalog.Infrastructure  EF Core AppDbContext, migrations, repository + unit-of-work
│                                  implementations, ID generator, DB seeding.
└── ProductCatalog.Api             Thin Controllers, global exception handling, Swagger, DI wiring.
tests/
└── ProductCatalog.UnitTests       Domain, Application (handlers + validators) and Infrastructure
                                   unit tests (xUnit + Moq + FluentAssertions).
```

Each use case (e.g. `CreateProduct`) lives in its own folder under
`Application/Products/Commands` or `Queries`, containing the command/query record, its handler,
and its validator. Handlers depend only on `IProductRepository` / `IUnitOfWork` abstractions
defined in `Domain`, never on EF Core directly — `Infrastructure` is the only project that knows
about SQL Server.

## Running locally

**Prerequisites:** .NET 10 SDK, SQL Server LocalDB (installed with Visual Studio, or the
[SQL Server Express LocalDB](https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb) installer).

```bash
dotnet restore

# Apply migrations (creates the ProductCatalog database on (localdb)\MSSQLLocalDB)
dotnet ef database update --project src/ProductCatalog.Infrastructure --startup-project src/ProductCatalog.Api

# Run the API
dotnet run --project src/ProductCatalog.Api
```

The API also **auto-applies pending migrations and seeds sample data on startup when running in
the `Development` environment** (the default for `dotnet run`), so the `dotnet ef database update`
step above is a convenience, not a requirement — a fresh clone works with just `dotnet run`. This
is deliberately **not** done in Release/Production builds; migrations there should be applied as an
explicit, reviewed step.

Swagger UI: `http://localhost:5077/swagger`.

Connection string lives in `src/ProductCatalog.Api/appsettings.json` (`ConnectionStrings:Default`)
— it points at LocalDB, so no secrets are involved.

### Running tests

```bash
dotnet test
```

## API endpoints

All responses use the envelope `{ "data": ..., "errors": [] }` on success; errors are returned as
[RFC 7807](https://www.rfc-editor.org/rfc/rfc7807) `ProblemDetails`.

| Method | Route | Description | Success | Failure |
|---|---|---|---|---|
| GET | `/api/products` | List all products (includes stock) | 200 | – |
| GET | `/api/products/{id}` | Get a product by ID | 200 | 404 |
| POST | `/api/products` | Create a product | 201 | 400 (validation) |
| PUT | `/api/products/{id}` | Update name/description/price | 200 | 400, 404, 409 (concurrency) |
| DELETE | `/api/products/{id}` | Delete a product | 204 | 404, 409 (concurrency) |
| POST | `/api/products/{id}/decrement-stock/{quantity}` | Decrement stock | 200 | 400 (insufficient stock / invalid quantity), 404, 409 (concurrency) |
| POST | `/api/products/{id}/add-to-stock/{quantity}` | Increment stock | 200 | 400 (invalid quantity), 404, 409 (concurrency) |
| GET | `/api/products/search?name=` | Partial, case-insensitive name search | 200 | 400 (missing name) |
| GET | `/api/products/stock-level?min=&max=` | Products with stock in `[min, max]` | 200 | 400 (missing or negative `min`/`max`, min > max) |

## Design decisions & trade-offs

This section exists because the review explicitly asked for documented rationale on
implementation choices — especially the ones that differ from a "default" approach.

- **Controllers, not Minimal APIs.** The assessment brief talks about "controllers remaining
  lightweight," so classic ASP.NET Core Controllers were used instead of Minimal API endpoints,
  even though Minimal APIs are a perfectly valid modern choice. Controllers here contain no
  business logic — each action just sends a MediatR command/query and translates the `Result<T>`
  into an HTTP response.

- **6-digit product ID generation (safe across multiple instances).** IDs are generated
  application-side as a random number in `[100000, 999999]`. Two layers make this safe under
  concurrent, multi-instance load:
  1. A cheap existence pre-check (`SELECT` by ID) avoids hitting the database in the common case.
  2. The `Id` column is the table's **primary key**, so the database itself rejects a genuine
     collision (e.g. two instances both winning the pre-check for the same number at the same
     time). `UnitOfWork` translates that SQL unique-constraint violation into a
     `UniqueConstraintViolationException`, which `CreateProductCommandHandler` catches and retries
     (up to 10 attempts) with a new candidate.

  The pre-check alone would have a race window; the constraint is what actually guarantees
  uniqueness regardless of how many instances are running. **Trade-off:** the requirement fixes
  the ID at 6 digits, capping the space at ~900,000 values — as the number of existing products
  approaches that ceiling, candidates collide more often and more creates hit the
  already-exists/unique-constraint path before landing on a free ID. Acceptable here since the
  exercise's scale is nowhere near that, but worth being explicit about.

  **Alternative considered:** a SQL `SEQUENCE` combined with an affine bijection
  (`id = 100000 + (seq * a + b) mod 900000`, with `a` coprime to `900000`, e.g. `a = 524287 =
  2^19 - 1`) would give the same multi-instance-safe uniqueness — atomically, straight from the
  database — with zero retries and zero collision probability up to 900,000 *lifetime* inserts,
  while still producing IDs that don't visibly reveal insertion order. It was set aside for two
  reasons: it fails deterministically and permanently once 900,000 products have ever been
  created (even if most were since deleted), whereas the retry approach above only degrades as
  the *currently live* ID space fills up — a better fit here since deletions are expected; and
  the assessment only requires uniqueness, not unguessability (which this technique doesn't
  actually provide anyway — it's a reversible linear map, not encryption: two known consecutive
  IDs are enough to recover `a` and predict the rest of the series).

- **Optimistic concurrency on every write to an existing product.** `Product` carries a SQL Server
  `rowversion` (`RowVersion`) column as an EF Core concurrency token. Because it covers the whole
  row, `decrement-stock`, `add-to-stock`, `PUT` and `DELETE` all reload-and-retry (up to 3 attempts)
  on a `DbUpdateConcurrencyException` (translated to `ConcurrencyConflictException` in
  `UnitOfWork`), so two concurrent requests against the same product don't silently lose one
  update, and a `PUT` racing a stock change doesn't fail. Before each retry the handler calls
  `IUnitOfWork.DiscardChanges()` (clears EF Core's change tracker): a tracking query hands back an
  already-tracked instance as-is instead of refreshing it from the database, so without that step
  the retry would reuse the stale entity — and its stale `RowVersion` — and conflict again every
  time. This wasn't explicitly required by the assessment's PDF,
  but it directly addresses "consider how your implementation would behave under concurrent
  workloads," which was called out separately.

- **Stock underflow is a 400, not a 409.** Decrementing more stock than is available is treated as
  a client input problem (you asked for something invalid given current state), not a resource
  conflict — 409 is reserved for the genuine concurrency-conflict case above.

- **`Result<T>` instead of exceptions for expected failures.** Not-found, validation, insufficient
  stock, and concurrency-exhausted are all modelled as `Result<T>.Failure(...)` with an error code,
  not thrown exceptions — keeps the happy path and the expected failure paths equally explicit in
  handlers. The global exception handler (`IExceptionHandler`) is reserved for truly unexpected
  errors and for FluentValidation's `ValidationException` (raised by the MediatR pipeline
  behaviour), both mapped to `ProblemDetails`.

- **`AsNoTracking()` on read-only queries.** List/search/stock-range queries don't need EF Core's
  change tracker, so they skip it; `GetById` (used by update/delete/stock operations, which mutate
  the entity) keeps tracking enabled.

- **Case-insensitive partial name search via `LIKE`.** Uses `EF.Functions.Like` with `%name%`
  rather than `.Contains()` + `.ToLower()` in C# — SQL Server's default collation is already
  case-insensitive, and this form translates to a single indexable `LIKE` rather than pulling rows
  into memory to filter. `%`, `_` and `[` in the search term are escaped, so user input is matched
  literally instead of acting as wildcards.

- **SQL Server LocalDB, not SQLite/Postgres.** Chosen for zero extra setup on a Windows machine
  with Visual Studio already installed, and because it matches what most enterprise .NET shops run
  in practice.

- **Unit tests only — no integration tests, no BDD.** The BDD suite mentioned in the brief is
  explicitly optional; integration tests were also left out. Both were a deliberate scope cut
  given the assessment's short turnaround, in favour of thorough unit coverage of the actual
  business rules (domain invariants, ID-generation retry, concurrency retry, validation) where the
  real risk of bugs lives. `IProductRepository`/`IUnitOfWork` being interfaces means integration
  tests (e.g. via `WebApplicationFactory` + a real/containerized SQL Server) could be added later
  without changing any production code.

- **Single README instead of multiple docs.** Given the small scope of a single-entity API, one
  README covers setup, endpoints and rationale — no need for the topic-per-file documentation
  style that makes sense for a larger, multi-module product.
