using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TwoGather.Domain.Entities;

namespace TwoGather.Infrastructure.Persistence.Configurations;

public class ItemOptionConfiguration : IEntityTypeConfiguration<ItemOption>
{
    public void Configure(EntityTypeBuilder<ItemOption> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .ValueGeneratedOnAdd();

        builder.Property(o => o.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(o => o.Price)
            .HasPrecision(18, 2);

        builder.Property(o => o.Currency)
            .HasMaxLength(10);

        builder.Property(o => o.Link)
            .HasMaxLength(2048);

        builder.Property(o => o.IsSelected)
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .IsRequired();
    }
}
