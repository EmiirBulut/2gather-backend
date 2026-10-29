using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TwoGather.Domain.Entities;

namespace TwoGather.Infrastructure.Persistence.Configurations;

public class ItemPurchaserConfiguration : IEntityTypeConfiguration<ItemPurchaser>
{
    public void Configure(EntityTypeBuilder<ItemPurchaser> builder)
    {
        builder.HasKey(p => new { p.ItemId, p.UserId });

        builder.HasOne(p => p.Item)
            .WithMany(i => i.Purchasers)
            .HasForeignKey(p => p.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
