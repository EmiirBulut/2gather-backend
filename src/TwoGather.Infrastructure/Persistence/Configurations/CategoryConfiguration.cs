using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TwoGather.Domain.Entities;

namespace TwoGather.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.RoomLabel)
            .HasMaxLength(100);

        builder.Property(c => c.IsSystem)
            .IsRequired();

        builder.HasOne(c => c.List)
            .WithMany()
            .HasForeignKey(c => c.ListId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
