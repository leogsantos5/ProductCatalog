using System.Text.Json.Serialization;

namespace ProductCatalog.Api.Contracts;

public record UpdateProductRequest([property: JsonRequired] string Name, string? Description, [property: JsonRequired] decimal Price);
