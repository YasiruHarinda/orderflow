using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Domain;

namespace Orders.Infrastructure.Persistence.Configurations;

internal sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("order_lines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.Sku).HasMaxLength(32).IsRequired();
        builder.Property(l => l.ProductName).HasMaxLength(200).IsRequired();

        // Money is a value object: stored as two columns on the same row, not its own table.
        builder.ComplexProperty(l => l.UnitPrice, money =>
        {
            money.Property(m => m.Amount).HasColumnName("unit_price").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("currency").HasMaxLength(3);
        });

        builder.Ignore(l => l.LineTotal);
    }
}
