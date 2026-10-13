using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using WayPoint.Infrastructure;
using WayPoint.Infrastructure.Data;
using Xunit.Abstractions;

namespace WayPoint.Tests;

public class LocalPostgreSqlVerificationTests
{
    private readonly ITestOutputHelper _output;

    public LocalPostgreSqlVerificationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Verify_Local_PostgreSql_Test_Database_Connection()
    {
        // 1. Load .env by walking up to solution root
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        string? envPath = null;
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, ".env");
            if (File.Exists(candidate))
            {
                envPath = candidate;
                break;
            }
            dir = dir.Parent;
        }

        if (envPath != null && File.Exists(envPath))
        {
            foreach (var line in File.ReadAllLines(envPath))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#")) continue;
                var separatorIdx = trimmed.IndexOf('=');
                if (separatorIdx > 0)
                {
                    var key = trimmed[..separatorIdx].Trim();
                    var val = trimmed[(separatorIdx + 1)..].Trim().Trim('"', '\'');
                    Environment.SetEnvironmentVariable(key, val);
                }
            }
        }

        var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var connStr = DependencyInjection.ResolveConnectionString(config);

        var builder = new NpgsqlConnectionStringBuilder(connStr);
        _output.WriteLine($"Target Host: {builder.Host}");
        _output.WriteLine($"Target Port: {builder.Port}");
        _output.WriteLine($"Target Database: {builder.Database}");
        _output.WriteLine($"Target Username: {builder.Username}");

        // Strict safety check: Must connect to local waypoint_test only
        builder.Host.Should().Match(h => h == "localhost" || h == "127.0.0.1", "Must connect to local host");
        builder.Database.Should().Be("waypoint_test", "Must connect to waypoint_test database only");

        // 2. Test raw connection and query current_database(), current_user, version()
        await using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT current_database(), current_user, version();";
        await using var reader = await cmd.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();

        var dbName = reader.GetString(0);
        var currentUser = reader.GetString(1);
        var pgVersion = reader.GetString(2);

        _output.WriteLine($"Connected Database: {dbName}");
        _output.WriteLine($"Current User: {currentUser}");
        _output.WriteLine($"PostgreSQL Version: {pgVersion}");

        dbName.Should().Be("waypoint_test");
        currentUser.Should().Be("postgres");

        // 3. Test WayPointDbContext CanConnectAsync
        var options = new DbContextOptionsBuilder<WayPointDbContext>()
            .UseNpgsql(connStr)
            .Options;

        await using var dbContext = new WayPointDbContext(options);
        var canConnect = await dbContext.Database.CanConnectAsync();
        canConnect.Should().BeTrue("WayPointDbContext must connect successfully");
    }
}
