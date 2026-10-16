using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Domain;

namespace Orders.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the Order aggregate to tables. The domain classes have no EF attributes;
/// all database details live here, in Infrastructure.
/// </summary>
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.Currency).HasMaxLength(3).IsRequired();
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.RejectionReason).HasMaxLength(500);

        // Optimistic concurrency: PostgreSQL's built-in "xmin" row version. If two
        // processes change the same order at once, the second save fails instead of
        // silently overwriting the first.
        builder.Property<uint>("Version").IsRowVersion();

        builder.HasIndex(o => o.CustomerId);
        builder.HasIndex(o => new { o.Status, o.CreatedAt });

        // Lines are stored through the private _lines field, so EF can fill the list
        // while the public API stays read-only.
        builder.HasMany(o => o.Lines)
            .WithOne()
            .HasForeignKey("OrderId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(o => o.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(o => o.Total);
        builder.Ignore(o => o.DomainEvents);
    }
}
