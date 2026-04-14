using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TwoGather.Domain.Entities;

namespace TwoGather.Infrastructure.Persistence.Configurations;

public class OptionClaimConfiguration : IEntityTypeConfiguration<OptionClaim>
{
    public void Configure(EntityTypeBuilder<OptionClaim> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd();

        builder.Property(c => c.Percentage)
            .IsRequired();

        builder.ToTable(t => t.HasCheckConstraint("CK_OptionClaim_Percentage",
            "\"Percentage\" = 25 OR \"Percentage\" = 50 OR \"Percentage\" = 75 OR \"Percentage\" = 100"));

        builder.Property(c => c.Status)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.HasIndex(c => new { c.OptionId, c.UserId });

        builder.HasOne(c => c.Option)
            .WithMany(o => o.Claims)
            .HasForeignKey(c => c.OptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
