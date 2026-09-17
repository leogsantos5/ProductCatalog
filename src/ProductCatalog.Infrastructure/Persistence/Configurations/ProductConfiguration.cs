using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ProductCatalog.Domain.Entities;

namespace ProductCatalog.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    private static readonly ValueConverter<DateTime, DateTime> UtcDateTimeConverter = new(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
        builder.Property(p => p.StockQuantity).IsRequired();
        builder.Property(p => p.CreatedAt).HasConversion(UtcDateTimeConverter);
        builder.Property(p => p.UpdatedAt).HasConversion(UtcDateTimeConverter);

        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.HasIndex(p => p.Name);
    }
}
