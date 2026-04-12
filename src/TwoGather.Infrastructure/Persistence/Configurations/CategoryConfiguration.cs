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

        builder.HasData(
            new Category { Id = new Guid("10000000-0000-0000-0000-000000000001"), Name = "Salon",        RoomLabel = "Salon",        IsSystem = true },
            new Category { Id = new Guid("10000000-0000-0000-0000-000000000002"), Name = "Yatak Odası",  RoomLabel = "Yatak Odası",  IsSystem = true },
            new Category { Id = new Guid("10000000-0000-0000-0000-000000000003"), Name = "Mutfak",       RoomLabel = "Mutfak",       IsSystem = true },
            new Category { Id = new Guid("10000000-0000-0000-0000-000000000004"), Name = "Banyo",        RoomLabel = "Banyo",        IsSystem = true },
            new Category { Id = new Guid("10000000-0000-0000-0000-000000000005"), Name = "Çocuk Odası",  RoomLabel = "Çocuk Odası",  IsSystem = true },
            new Category { Id = new Guid("10000000-0000-0000-0000-000000000006"), Name = "Genel",        RoomLabel = "Genel",        IsSystem = true }
        );
    }
}
