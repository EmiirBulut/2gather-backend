using Microsoft.EntityFrameworkCore;
using TwoGather.Domain.Entities;

namespace TwoGather.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Domain.Entities.List> Lists => Set<Domain.Entities.List>();
    public DbSet<ListMember> ListMembers => Set<ListMember>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemOption> ItemOptions => Set<ItemOption>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ListInvite> ListInvites => Set<ListInvite>();
    public DbSet<OptionRating> OptionRatings => Set<OptionRating>();
    public DbSet<OptionClaim> OptionClaims => Set<OptionClaim>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
