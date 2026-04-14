using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Infrastructure.Persistence;
using TwoGather.Infrastructure.Persistence.Repositories;
using TwoGather.Infrastructure.Services;
using TwoGather.Infrastructure.Settings;

namespace TwoGather.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IListRepository, ListRepository>();
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<IOptionRepository, OptionRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IListInviteRepository, ListInviteRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IOptionRatingRepository, OptionRatingRepository>();
        services.AddScoped<IOptionClaimRepository, OptionClaimRepository>();

        services.AddScoped<IEmailService, ConsoleEmailService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IDateTimeService, DateTimeService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }
}
