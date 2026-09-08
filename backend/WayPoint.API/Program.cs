using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WayPoint.API.Middleware;
using WayPoint.Application;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Infrastructure;
using WayPoint.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Load .env file automatically if present (for local development without leaking secrets)
var rootDirectory = Directory.GetParent(builder.Environment.ContentRootPath)?.Parent?.FullName 
                    ?? builder.Environment.ContentRootPath;
var envFilePath = Path.Combine(rootDirectory, ".env");
if (!File.Exists(envFilePath))
{
    envFilePath = Path.Combine(builder.Environment.ContentRootPath, ".env");
}

if (File.Exists(envFilePath))
{
    foreach (var line in File.ReadAllLines(envFilePath))
    {
        var trimmed = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#")) continue;
        var separatorIdx = trimmed.IndexOf('=');
        if (separatorIdx > 0)
        {
            var key = trimmed[..separatorIdx].Trim();
            var val = trimmed[(separatorIdx + 1)..].Trim().Trim('"', '\'');
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, val);
            }
        }
    }
}


// 1. Application & Infrastructure Services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 2. Controllers & API Behavior
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 3. JWT Authentication & Authorization
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "WayPoint_Super_Secret_Key_For_Jwt_Token_Authentication_2026";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "WayPoint";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "WayPointClients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireManager", policy => policy.RequireRole("TransportManager", "Admin"));
    options.AddPolicy("RequireOperator", policy => policy.RequireRole("Operator", "TransportManager", "Admin"));
    options.AddPolicy("RequirePassenger", policy => policy.RequireRole("Passenger", "Admin"));
});

// 4. CORS Policy for React Web & Mobile clients
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 5. Swagger with Bearer Authentication
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WayPoint API",
        Version = "v1",
        Description = "Authoritative REST API for WayPoint Intercity Journey Planning & Bus Operations Platform (SE3090 Assignment 1)."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token: e.g., 'Bearer <token>'"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 6. CLI `--seed` argument handling
if (args.Contains("--seed"))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<WayPointDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Applying migrations and seeding database from CLI...");
    await context.Database.MigrateAsync();
    await DbSeeder.SeedAsync(context, passwordHasher);
    logger.LogInformation("Database seeded successfully. Exiting CLI mode.");
    return;
}

// 7. Middleware Pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("EnableSwaggerInProduction"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "WayPoint API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAllOrigins");

app.UseAuthentication();
app.UseAuthorization();

// 8. Health check endpoint (Required by Assignment REQ-DEP-01)
app.MapGet("/health", async (IWayPointDbContext context) =>
{
    var db = (WayPointDbContext)context;
    var canConnect = await db.Database.CanConnectAsync();
    return canConnect
        ? Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow, database = "Connected" })
        : Results.Problem(detail: "Database connection failed", statusCode: StatusCodes.Status503ServiceUnavailable);
})
.WithName("HealthCheck")
.WithTags("Infrastructure");

app.MapControllers();

app.Run();
