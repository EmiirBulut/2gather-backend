using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TwoGather.Domain.Entities;

namespace TwoGather.Infrastructure.Persistence.Configurations;

public class ListMemberConfiguration : IEntityTypeConfiguration<ListMember>
{
    public void Configure(EntityTypeBuilder<ListMember> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedOnAdd();

        builder.HasIndex(m => new { m.ListId, m.UserId })
            .IsUnique();

        builder.Property(m => m.Role)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(m => m.JoinedAt)
            .IsRequired();

        builder.HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
