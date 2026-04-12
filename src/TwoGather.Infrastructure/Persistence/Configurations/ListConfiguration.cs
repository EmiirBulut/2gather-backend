using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TwoGather.Infrastructure.Persistence.Configurations;

public class ListConfiguration : IEntityTypeConfiguration<Domain.Entities.List>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.List> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .ValueGeneratedOnAdd();

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(l => l.CreatedAt)
            .IsRequired();

        builder.HasOne(l => l.Owner)
            .WithMany()
            .HasForeignKey(l => l.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(l => l.Members)
            .WithOne(m => m.List)
            .HasForeignKey(m => m.ListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(l => l.Items)
            .WithOne(i => i.List)
            .HasForeignKey(i => i.ListId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
