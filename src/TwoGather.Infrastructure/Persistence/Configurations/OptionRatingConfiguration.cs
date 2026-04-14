using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TwoGather.Domain.Entities;

namespace TwoGather.Infrastructure.Persistence.Configurations;

public class OptionRatingConfiguration : IEntityTypeConfiguration<OptionRating>
{
    public void Configure(EntityTypeBuilder<OptionRating> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedOnAdd();

        builder.Property(r => r.Score)
            .IsRequired();

        builder.ToTable(t => t.HasCheckConstraint("CK_OptionRating_Score", "\"Score\" >= 1 AND \"Score\" <= 5"));

        builder.HasIndex(r => new { r.OptionId, r.UserId })
            .IsUnique();

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.HasOne(r => r.Option)
            .WithMany(o => o.Ratings)
            .HasForeignKey(r => r.OptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
