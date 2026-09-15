namespace ProductCatalog.Domain.Entities;

public class Product
{
    public const int MinId = 100_000;
    public const int MaxId = 999_999;

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Optimistic concurrency token for PUT and DELETE; SQL Server also bumps it on atomic stock updates.
    public byte[] RowVersion { get; private set; } = [];

    private Product() { }

    public static Product Create(int id, string name, string? description, decimal price, int initialStock)
    {
        var product = new Product
        {
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        product.SetId(id);
        product.SetName(name);
        product.Description = description;
        product.SetPrice(price);
        product.SetInitialStock(initialStock);

        return product;
    }

    public void Update(string name, string? description, decimal price)
    {
        SetName(name);
        Description = description;
        SetPrice(price);
        UpdatedAt = DateTime.UtcNow;
    }

    private void SetId(int id)
    {
        if (id < MinId || id > MaxId)
            throw new ArgumentOutOfRangeException(nameof(id), $"Product ID must be a 6-digit number between {MinId} and {MaxId}.");

        Id = id;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        Name = name.Trim();
    }

    private void SetPrice(decimal price)
    {
        if (price <= 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be greater than zero.");

        Price = price;
    }

    private void SetInitialStock(int initialStock)
    {
        if (initialStock < 0)
            throw new ArgumentOutOfRangeException(nameof(initialStock), "Initial stock cannot be negative.");

        StockQuantity = initialStock;
    }
}
