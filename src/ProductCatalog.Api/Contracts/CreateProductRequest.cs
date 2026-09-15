using System.Text.Json.Serialization;

namespace ProductCatalog.Api.Contracts;

public record CreateProductRequest([property: JsonRequired] string Name, string? Description,
                                   [property: JsonRequired] decimal Price, [property: JsonRequired] int InitialStock);
