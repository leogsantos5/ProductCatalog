using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ProductCatalog.Api.Contracts;
using ProductCatalog.Application.Common;
using ProductCatalog.Application.Products;
using ProductCatalog.Application.Products.Commands.AddStock;
using ProductCatalog.Application.Products.Commands.CreateProduct;
using ProductCatalog.Application.Products.Commands.DecrementStock;
using ProductCatalog.Application.Products.Commands.DeleteProduct;
using ProductCatalog.Application.Products.Commands.UpdateProduct;
using ProductCatalog.Application.Products.Queries.GetProductById;
using ProductCatalog.Application.Products.Queries.GetProductsByStockRange;
using ProductCatalog.Application.Products.Queries.ListProducts;
using ProductCatalog.Application.Products.Queries.SearchProductsByName;

namespace ProductCatalog.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListProductsQuery(), ct);
        return ToActionResult(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id), ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var command = new CreateProductCommand(request.Name, request.Description, request.Price, request.InitialStock);
        var result = await _mediator.Send(command, ct);

        if (!result.IsSuccess)
            return ToErrorActionResult(result.ErrorCode, result.Error!);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, ApiResponse<ProductDto>.Ok(result.Value));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        var command = new UpdateProductCommand(id, request.Name, request.Description, request.Price);
        var result = await _mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteProductCommand(id), ct);

        if (!result.IsSuccess)
            return ToErrorActionResult(result.ErrorCode, result.Error!);

        return NoContent();
    }

    [HttpPost("{id:int}/decrement-stock/{quantity:int}")]
    public async Task<IActionResult> DecrementStock(int id, int quantity, CancellationToken ct)
    {
        var result = await _mediator.Send(new DecrementStockCommand(id, quantity), ct);
        return ToActionResult(result);
    }

    [HttpPost("{id:int}/add-to-stock/{quantity:int}")]
    public async Task<IActionResult> AddToStock(int id, int quantity, CancellationToken ct)
    {
        var result = await _mediator.Send(new AddStockCommand(id, quantity), ct);
        return ToActionResult(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? name, CancellationToken ct)
    {
        var result = await _mediator.Send(new SearchProductsByNameQuery(name ?? string.Empty), ct);
        return ToActionResult(result);
    }

    [HttpGet("stock-level")]
    public async Task<IActionResult> GetByStockLevel([FromQuery, BindRequired] int min, [FromQuery, BindRequired] int max, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProductsByStockRangeQuery(min, max), ct);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(Result<T> result) =>
        result.IsSuccess
            ? Ok(ApiResponse<T>.Ok(result.Value!))
            : ToErrorActionResult(result.ErrorCode, result.Error!);

    private IActionResult ToErrorActionResult(string? errorCode, string error)
    {
        var statusCode = errorCode switch
        {
            ErrorCodes.NotFound => StatusCodes.Status404NotFound,
            ErrorCodes.ConcurrencyConflict => StatusCodes.Status409Conflict,
            ErrorCodes.IdGenerationFailed => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(statusCode, new ProblemDetails
        {
            Status = statusCode,
            Title = errorCode ?? "Error",
            Detail = error
        });
    }
}
