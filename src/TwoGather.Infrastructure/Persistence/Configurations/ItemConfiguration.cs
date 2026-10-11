using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TwoGather.Domain.Entities;

namespace TwoGather.Infrastructure.Persistence.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .ValueGeneratedOnAdd();

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(i => i.ImageUrl)
            .HasMaxLength(2000);

        builder.Property(i => i.PlanningNote)
            .HasMaxLength(1000);

        builder.Property(i => i.CreatedAt)
            .IsRequired();

        builder.Property(i => i.UpdatedAt)
            .IsRequired();

        builder.HasOne(i => i.Category)
            .WithMany()
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Plain column, deliberately without a foreign key: Items -> ItemOptions already
        // cascades, and a SET NULL constraint back to ItemOptions would form a delete cycle.
        // Options of a purchased item are locked, so the reference cannot dangle.
        builder.Property(i => i.PurchasedOptionId);

        builder.HasMany(i => i.Options)
            .WithOne(o => o.Item)
            .HasForeignKey(o => o.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
