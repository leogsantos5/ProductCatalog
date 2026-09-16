namespace ProductCatalog.Domain.Entities;

public class Product
{
    public const int MinId = 100_000;
    public const int MaxId = 999_999;

    public int Id
    {
        get;
        private set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, MinId);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxId);
            field = value;
        }
    }

    public string Name
    {
        get;
        private set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            field = value.Trim();
        }
    } = string.Empty;

    public string? Description { get; private set; }

    public decimal Price
    {
        get;
        private set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            field = value;
        }
    }

    public int StockQuantity
    {
        get;
        private set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            field = value;
        }
    }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    private Product() { }

    public static Product Create(int id, string name, string? description, decimal price, int initialStock)
    {
        var now = DateTime.UtcNow;

        return new Product
        {
            Id = id,
            Name = name,
            Description = description,
            Price = price,
            StockQuantity = initialStock,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(string name, string? description, decimal price)
    {
        Name = name;
        Description = description;
        Price = price;
        UpdatedAt = DateTime.UtcNow;
    }
}
