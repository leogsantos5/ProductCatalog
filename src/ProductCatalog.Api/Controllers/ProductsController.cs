using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ProductCatalog.Api.Contracts;
using ProductCatalog.Api.Errors;
using ProductCatalog.Api.Http;
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

    /// <summary>Lists products, including their stock. Paged by ID: page defaults to 1, pageSize to 50 (max 100).</summary>
    [HttpGet]
    [ProducesResponseType<PagedApiResponse<ProductDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] int page = Paging.DefaultPage, [FromQuery] int pageSize = Paging.DefaultPageSize,
                                            CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListProductsQuery(page, pageSize), ct);
        return ToPagedActionResult(result);
    }

    /// <summary>Gets a product by its ID. The ETag header holds its current version.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ApiResponse<ProductDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id), ct);
        return ToActionResult(result);
    }

    /// <summary>Creates a product with an auto-generated 6-digit ID.</summary>
    [HttpPost]
    [ProducesResponseType<ApiResponse<ProductDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var command = new CreateProductCommand(request.Name, request.Description, request.Price, request.InitialStock);
        var result = await _mediator.Send(command, ct);

        if (!result.IsSuccess)
            return ToErrorActionResult(result.ErrorCode, result.Error!);

        Response.Headers.ETag = ETags.Format(result.Value!.Version);
        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, ApiResponse<ProductDto>.Ok(result.Value));
    }

    /// <summary>
    /// Updates a product's name, description and price. Send its ETag in If-Match to get a 412
    /// instead of overwriting changes made since you read it.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<ApiResponse<ProductDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request,
                                            [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken ct)
    {
        var command = new UpdateProductCommand(id, request.Name, request.Description, request.Price, ETags.ParseIfMatch(ifMatch));
        var result = await _mediator.Send(command, ct);
        return ToActionResult(result);
    }

    /// <summary>
    /// Deletes a product. Send its ETag in If-Match to get a 412 if it changed since you read it.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> Delete(int id, [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteProductCommand(id, ETags.ParseIfMatch(ifMatch)), ct);

        if (!result.IsSuccess)
            return ToErrorActionResult(result.ErrorCode, result.Error!);

        return NoContent();
    }

    /// <summary>Removes the given quantity from a product's stock.</summary>
    /// <response code="400">Invalid quantity, or not enough stock available.</response>
    [HttpPost("{id:int}/decrement-stock/{quantity:int}")]
    [ProducesResponseType<ApiResponse<ProductDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DecrementStock(int id, int quantity, CancellationToken ct)
    {
        var result = await _mediator.Send(new DecrementStockCommand(id, quantity), ct);
        return ToActionResult(result);
    }

    /// <summary>Adds the given quantity to a product's stock.</summary>
    /// <response code="400">Invalid quantity, or the stock would exceed its maximum.</response>
    [HttpPost("{id:int}/add-to-stock/{quantity:int}")]
    [ProducesResponseType<ApiResponse<ProductDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddToStock(int id, int quantity, CancellationToken ct)
    {
        var result = await _mediator.Send(new AddStockCommand(id, quantity), ct);
        return ToActionResult(result);
    }

    /// <summary>Finds products whose name contains the given text (case-insensitive). Paged like the product list.</summary>
    [HttpGet("search")]
    [ProducesResponseType<PagedApiResponse<ProductDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromQuery] string? name, [FromQuery] int page = Paging.DefaultPage,
                                            [FromQuery] int pageSize = Paging.DefaultPageSize, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SearchProductsByNameQuery(name ?? string.Empty, page, pageSize), ct);
        return ToPagedActionResult(result);
    }

    /// <summary>Lists products whose stock is between min and max (inclusive). Paged like the product list.</summary>
    [HttpGet("stock-level")]
    [ProducesResponseType<PagedApiResponse<ProductDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByStockLevel([FromQuery, BindRequired] int min, [FromQuery, BindRequired] int max,
                                                     [FromQuery] int page = Paging.DefaultPage, [FromQuery] int pageSize = Paging.DefaultPageSize,
                                                     CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProductsByStockRangeQuery(min, max, page, pageSize), ct);
        return ToPagedActionResult(result);
    }

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (!result.IsSuccess)
            return ToErrorActionResult(result.ErrorCode, result.Error!);

        if (result.Value is ProductDto product)
            Response.Headers.ETag = ETags.Format(product.Version);

        return Ok(ApiResponse<T>.Ok(result.Value!));
    }

    private IActionResult ToPagedActionResult<T>(Result<PagedResult<T>> result)
    {
        if (!result.IsSuccess)
            return ToErrorActionResult(result.ErrorCode, result.Error!);

        var page = result.Value!;
        return Ok(new PagedApiResponse<T>(page.Items, page.Page, page.PageSize, page.TotalCount, page.TotalPages));
    }

    private IActionResult ToErrorActionResult(string? errorCode, string error)
    {
        var result = Problem(detail: error, statusCode: ErrorCodeMapper.ToStatusCode(errorCode));
        ((ProblemDetails)result.Value!).Extensions["errorCode"] = errorCode;
        return result;
    }
}
