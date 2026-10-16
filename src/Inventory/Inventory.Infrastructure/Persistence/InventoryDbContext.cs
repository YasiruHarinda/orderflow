using Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public const string Schema = "inventory";

    public DbSet<Product> Products => Set<Product>();

    public DbSet<StockReservation> StockReservations => Set<StockReservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
    }
}

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Sku).HasMaxLength(32).IsRequired();
        builder.HasIndex(p => p.Sku).IsUnique();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Price).HasPrecision(18, 2);
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();

        // Two orders reserving the last unit at the same moment: one save fails with a
        // concurrency error and is retried, so stock can never go negative.
        builder.Property<uint>("Version").IsRowVersion();

        builder.Ignore(p => p.QuantityAvailable);
        builder.Ignore(p => p.DomainEvents);

        // Database-level safety net in addition to the domain rules.
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_products_reserved_not_negative", "\"QuantityReserved\" >= 0");
            t.HasCheckConstraint("ck_products_reserved_within_on_hand", "\"QuantityReserved\" <= \"QuantityOnHand\"");
        });
    }
}

internal sealed class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("stock_reservations");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        // One reservation per order: the database itself guarantees idempotency.
        builder.HasIndex(r => r.OrderId).IsUnique();
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property<uint>("Version").IsRowVersion();

        // The reserved items are small and always read together, so they are stored
        // as one JSON (jsonb) column instead of a separate table.
        builder.OwnsMany(r => r.Items, items => items.ToJson());
        builder.Navigation(r => r.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(r => r.DomainEvents);
    }
}
