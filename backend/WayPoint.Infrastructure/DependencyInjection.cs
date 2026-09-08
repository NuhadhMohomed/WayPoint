using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Infrastructure.Data;
using WayPoint.Infrastructure.Services;

namespace WayPoint.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = ResolveConnectionString(configuration);

        services.AddDbContext<WayPointDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(WayPointDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
            });
        });

        services.AddScoped<IWayPointDbContext>(provider => provider.GetRequiredService<WayPointDbContext>());
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        return services;
    }

    public static string ResolveConnectionString(IConfiguration configuration)
    {
        // Check DATABASE_URL first (Railway standard)
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
                          ?? configuration["ConnectionStrings:DATABASE_URL"]
                          ?? configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(databaseUrl))
        {
            // Local fallback
            return "Host=localhost;Port=5432;Database=waypoint;Username=postgres;Password=postgres";
        }

        // If it starts with postgres:// or postgresql://, parse it into an NpgsqlConnectionStringBuilder
        if (databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            databaseUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':');
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : 5432,
                Username = userInfo.Length > 0 ? userInfo[0] : "",
                Password = userInfo.Length > 1 ? userInfo[1] : "",
                Database = uri.AbsolutePath.TrimStart('/'),
                SslMode = SslMode.Require,
                SslNegotiation = SslNegotiation.Direct,
                Timeout = 15,
                CommandTimeout = 60
            };
            return builder.ToString();
        }

        return databaseUrl;
    }
}
