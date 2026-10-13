using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.Common.Interfaces.Disruption;
using WayPoint.Application.Features.JourneyPlanning;
using WayPoint.Application.Common.Interfaces.Fleet;
using WayPoint.Infrastructure.Data;
using WayPoint.Infrastructure.Services;
using WayPoint.Infrastructure.Services.Disruption;
using WayPoint.Infrastructure.Services.Fleet;

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

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IWayPointDbContext>(provider => provider.GetRequiredService<WayPointDbContext>());
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IJourneyPlanningService, JourneyPlanningService>();
        services.AddScoped<IBookingService, BookingService>();

        // Component 2: Fleet, Seat & Resource Feasibility Services (Nuhadh)
        services.AddScoped<IBusService, BusService>();
        services.AddScoped<ISeatLayoutService, SeatLayoutService>();
        services.AddScoped<IDriverService, DriverService>();
        services.AddScoped<ISeatAvailabilityService, SeatAvailabilityService>();
        services.AddScoped<IResourceFeasibilityService, ResourceFeasibilityService>();
        services.AddScoped<IReviewService, ReviewService>();

        // Component 4: Disruption, Rebooking & Approval Services (Dineth)
        services.AddScoped<IDisruptionService, DisruptionService>();
        services.AddScoped<IRebookingService, RebookingService>();
        services.AddScoped<IApprovalService, ApprovalService>();
        services.AddScoped<IServiceAlertService, ServiceAlertService>();
        services.AddScoped<IAiWorkflowQueryService, AiWorkflowQueryService>();
        // Agentic AI Persistence Services
        services.AddScoped<IAiWorkflowService, AiWorkflowService>();

        // Passenger Notifications
        services.AddScoped<INotificationService, NotificationService>();

        // AI Microservice Gateway Client
        var aiBaseUrl = configuration["AI_SERVICE_URL"] ?? configuration["AiSubsystem:BaseUrl"] ?? "http://localhost:8000";
        services.AddHttpClient<IAiRecommendationClient, AiRecommendationClient>(client =>
        {
            client.BaseAddress = new Uri(aiBaseUrl);
            var timeoutSeconds = int.TryParse(configuration["AI_TIMEOUT_SECONDS"], out var sec) ? sec : 30;
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

            var aiApiKey = configuration["AI_API_KEY"] ?? configuration["AI_SERVICE_API_KEY"] ?? configuration["AiSubsystem:ApiKey"];
            if (!string.IsNullOrWhiteSpace(aiApiKey))
            {
                client.DefaultRequestHeaders.Add("X-API-Key", aiApiKey);
            }
        });

        return services;
    }

    public static string ResolveConnectionString(IConfiguration configuration)
    {
        // 1. Explicit local test / development connection string (User Secrets, appsettings.Development.json, or test overrides)
        var localConnection = configuration.GetConnectionString("TestConnection")
                              ?? configuration.GetConnectionString("LocalConnection");

        // 2. Check if configuration explicitly passed a mock DATABASE_URL (for unit test overrides)
        var configDbUrl = configuration["ConnectionStrings:DATABASE_URL"]
                          ?? configuration["DATABASE_URL"];

        // 3. Local database URL override from configuration or environment
        var localDbUrl = configuration["LOCAL_DATABASE_URL"]
                         ?? configuration["ConnectionStrings:LOCAL_DATABASE_URL"]
                         ?? Environment.GetEnvironmentVariable("LOCAL_DATABASE_URL");

        // 4. User Secrets or Development override: check if DefaultConnection is configured with local database (e.g. waypoint_test)
        var defaultConnection = configuration.GetConnectionString("DefaultConnection");
        var isCustomDefault = !string.IsNullOrWhiteSpace(defaultConnection) &&
                              !string.Equals(defaultConnection, "Host=localhost;Port=5432;Database=waypoint;Username=postgres;Password=postgres", StringComparison.OrdinalIgnoreCase);

        string databaseUrl;
        if (!string.IsNullOrWhiteSpace(localConnection))
        {
            databaseUrl = localConnection;
        }
        else if (!string.IsNullOrWhiteSpace(configDbUrl) && !configDbUrl.Contains("iriguchi.proxy.rlwy.net"))
        {
            // Unit test or explicit non-default configuration override
            databaseUrl = configDbUrl;
        }
        else if (!string.IsNullOrWhiteSpace(localDbUrl))
        {
            databaseUrl = localDbUrl;
        }
        else if (isCustomDefault && defaultConnection != null)
        {
            databaseUrl = defaultConnection;
        }
        else
        {
            // Production / Railway fallback
            databaseUrl = !string.IsNullOrWhiteSpace(configDbUrl) ? configDbUrl
                          : Environment.GetEnvironmentVariable("DATABASE_URL")
                          ?? defaultConnection
                          ?? "Host=localhost;Port=5432;Database=waypoint_test;Username=postgres;Password=postgres";
        }

        return ParseOrBuildConnectionString(databaseUrl);
    }

    private static string ParseOrBuildConnectionString(string databaseUrl)
    {
        // If it starts with postgres:// or postgresql://, parse it into an NpgsqlConnectionStringBuilder
        if (databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            databaseUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':');
            var isLocal = uri.Host == "localhost" || uri.Host == "127.0.0.1";
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : 5432,
                Username = userInfo.Length > 0 ? userInfo[0] : "",
                Password = userInfo.Length > 1 ? userInfo[1] : "",
                Database = uri.AbsolutePath.TrimStart('/'),
                SslMode = isLocal ? SslMode.Prefer : SslMode.Require,
                Timeout = 15,
                CommandTimeout = 60
            };
            if (!isLocal)
            {
                builder.SslNegotiation = SslNegotiation.Direct;
            }
            return builder.ToString();
        }

        return databaseUrl;
    }
}
