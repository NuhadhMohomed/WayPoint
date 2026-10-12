using System.Data.Common;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using WayPoint.API.Middleware;
using Xunit;

namespace WayPoint.Tests;

/// <summary>
/// Security Fix 06 — Production Error Information Exposure & Centralized Error Handling Tests.
/// Validates:
/// 1. Unexpected exceptions return sanitized HTTP 500 responses in production.
/// 2. Production responses never reveal stack traces.
/// 3. Database errors do not expose SQL or connection strings.
/// 4. Authentication failures return correct HTTP status codes (401).
/// 5. Ownership violations return correct authorization errors (403).
/// 6. Payment verification failures preserve appropriate responses (400).
/// 7. Seat conflicts still return 409 Conflict.
/// 8. Expired holds still return 410 Gone.
/// 9. Safe validation messages remain available for business errors.
/// 10. Internal exception details are logged securely with TraceId.
/// 11. SensitiveDataSanitizer effectively strips secrets, tokens, and paths.
/// </summary>
public class ErrorHandlingSecurityTests
{
    private static (ExceptionHandlingMiddleware middleware, Mock<ILogger<ExceptionHandlingMiddleware>> mockLogger) CreateMiddleware(
        RequestDelegate next,
        string environmentName = "Production")
    {
        var mockLogger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        var mockEnv = new Mock<IHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns(environmentName);

        var middleware = new ExceptionHandlingMiddleware(next, mockLogger.Object, mockEnv.Object);
        return (middleware, mockLogger);
    }

    private static async Task<(HttpContext context, ProblemDetails problemDetails, string rawBody)> ExecutePipelineAsync(
        ExceptionHandlingMiddleware middleware,
        HttpContext context)
    {
        context.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var rawBody = await reader.ReadToEndAsync();

        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(rawBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new ProblemDetails();

        return (context, problemDetails, rawBody);
    }

    // =========================================================================
    // 1 & 2: Unexpected Exceptions & Stack Trace Hiding in Production
    // =========================================================================

    [Fact]
    public async Task UnexpectedException_InProduction_ReturnsGeneric500WithoutInternalDetails()
    {
        const string secretError = "Database crash: NullReference at D:\\WayPoint\\Secret\\Core.cs:124 with key Host=db.prod;Password=SuperSecret123!";
        RequestDelegate next = _ => throw new NullReferenceException(secretError);

        var (middleware, _) = CreateMiddleware(next, "Production");
        var context = new DefaultHttpContext();

        var (ctx, problem, rawBody) = await ExecutePipelineAsync(middleware, context);

        Assert.Equal((int)HttpStatusCode.InternalServerError, ctx.Response.StatusCode);
        Assert.Equal("Internal Server Error", problem.Title);
        Assert.Equal("An unexpected error occurred while processing your request. Please try again later.", problem.Detail);

        // Security check: raw sensitive strings must NOT appear anywhere in the body
        Assert.DoesNotContain("SuperSecret123!", rawBody);
        Assert.DoesNotContain("NullReference", rawBody);
        Assert.DoesNotContain("D:\\WayPoint", rawBody);
        Assert.DoesNotContain("Core.cs", rawBody);
    }

    [Fact]
    public async Task UnexpectedException_InProduction_NeverRevealsStackTrace()
    {
        RequestDelegate next = _ =>
        {
            try
            {
                throw new InvalidOperationException("Internal system fault");
            }
            catch (Exception ex)
            {
                throw new Exception("Chained error with stack trace", ex);
            }
        };

        var (middleware, _) = CreateMiddleware(next, "Production");
        var context = new DefaultHttpContext();

        var (_, problem, rawBody) = await ExecutePipelineAsync(middleware, context);

        Assert.Equal((int)HttpStatusCode.InternalServerError, problem.Status);
        Assert.DoesNotContain("stackTrace", rawBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("at WayPoint.", rawBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("line ", rawBody, StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // 3: Database Errors Do Not Expose SQL or Connection Strings
    // =========================================================================

    [Fact]
    public async Task DatabaseException_InProduction_MasksConnectionStringsAndSql()
    {
        const string dbError = "Npgsql.NpgsqlException: SELECT * FROM Users WHERE Password='SecretPassword!' failed. Host=postgres.prod;Database=waypoint;Username=admin;Password=SecretPassword!;";
        RequestDelegate next = _ => throw new DbUpdateException("Database query failed", new Exception(dbError));

        var (middleware, _) = CreateMiddleware(next, "Production");
        var context = new DefaultHttpContext();

        var (ctx, problem, rawBody) = await ExecutePipelineAsync(middleware, context);

        Assert.Equal((int)HttpStatusCode.InternalServerError, ctx.Response.StatusCode);
        Assert.Equal("Internal Server Error", problem.Title);
        Assert.Equal("A database error occurred while processing your request. Please try again later.", problem.Detail);

        Assert.DoesNotContain("SecretPassword!", rawBody);
        Assert.DoesNotContain("SELECT * FROM", rawBody);
        Assert.DoesNotContain("Host=postgres.prod", rawBody);
    }

    [Fact]
    public async Task DatabaseException_InDevelopment_SanitizesSensitiveConnectionStrings()
    {
        const string dbError = "Host=127.0.0.1;Database=waypoint_dev;Password=DevSecretPassword123!; port 5432 query error";
        RequestDelegate next = _ => throw new DbUpdateException(dbError);

        var (middleware, _) = CreateMiddleware(next, "Development");
        var context = new DefaultHttpContext();

        var (_, _, rawBody) = await ExecutePipelineAsync(middleware, context);

        Assert.DoesNotContain("DevSecretPassword123!", rawBody);
        Assert.Contains("[REDACTED]", rawBody);
    }

    // =========================================================================
    // 4 & 5: Authentication & Ownership Authorization Status Codes
    // =========================================================================

    [Fact]
    public async Task UnauthorizedAccess_UnauthenticatedUser_Returns401Unauthorized()
    {
        RequestDelegate next = _ => throw new UnauthorizedAccessException("Please log in first");

        var (middleware, _) = CreateMiddleware(next, "Production");
        var context = new DefaultHttpContext();
        // Unauthenticated (no user identity)

        var (ctx, problem, _) = await ExecutePipelineAsync(middleware, context);

        Assert.Equal((int)HttpStatusCode.Unauthorized, ctx.Response.StatusCode);
        Assert.Equal("Unauthorized", problem.Title);
    }

    [Fact]
    public async Task UnauthorizedAccess_AuthenticatedUser_Returns403Forbidden()
    {
        RequestDelegate next = _ => throw new UnauthorizedAccessException("Cannot access another passenger's booking");

        var (middleware, _) = CreateMiddleware(next, "Production");
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Passenger")
        }, "TestAuth"));

        var (ctx, problem, _) = await ExecutePipelineAsync(middleware, context);

        Assert.Equal((int)HttpStatusCode.Forbidden, ctx.Response.StatusCode);
        Assert.Equal("Access Denied", problem.Title);
        Assert.Contains("Cannot access another passenger's booking", problem.Detail);
    }

    // =========================================================================
    // 6: Payment Verification Failures
    // =========================================================================

    [Fact]
    public async Task PaymentVerificationFailure_Returns400BadRequest()
    {
        RequestDelegate next = _ => throw new InvalidOperationException("Payment verification failed: transaction ID was already settled");

        var (middleware, _) = CreateMiddleware(next, "Production");
        var context = new DefaultHttpContext();

        var (ctx, problem, _) = await ExecutePipelineAsync(middleware, context);

        Assert.Equal((int)HttpStatusCode.BadRequest, ctx.Response.StatusCode);
        Assert.Equal("Payment Verification Failed", problem.Title);
        Assert.Contains("Payment verification failed", problem.Detail);
    }

    // =========================================================================
    // 7: Seat Conflicts (409 Conflict)
    // =========================================================================

    [Fact]
    public async Task ConcurrencyConflict_InvalidOperation_Returns409Conflict()
    {
        RequestDelegate next = _ => throw new InvalidOperationException("Seat conflict: requested seat 12B is already held");

        var (middleware, _) = CreateMiddleware(next, "Production");
        var context = new DefaultHttpContext();

        var (ctx, problem, _) = await ExecutePipelineAsync(middleware, context);

        Assert.Equal((int)HttpStatusCode.Conflict, ctx.Response.StatusCode);
        Assert.Equal("Conflict", problem.Title);
    }

    [Fact]
    public async Task ConcurrencyConflict_DbUpdateConcurrencyException_Returns409Conflict()
    {
        RequestDelegate next = _ => throw new DbUpdateConcurrencyException("Row version conflict");

        var (middleware, _) = CreateMiddleware(next, "Production");
        var context = new DefaultHttpContext();

        var (ctx, problem, _) = await ExecutePipelineAsync(middleware, context);

        Assert.Equal((int)HttpStatusCode.Conflict, ctx.Response.StatusCode);
        Assert.Equal("Concurrency Conflict", problem.Title);
    }

    [Fact]
    public async Task ConcurrencyConflict_DbUpdateUniqueConstraint_Returns409Conflict()
    {
        RequestDelegate next = _ => throw new DbUpdateException("Error", new Exception("23505: duplicate key value violates unique constraint IX_SeatAllocations_ServiceId_SeatId"));

        var (middleware, _) = CreateMiddleware(next, "Production");
        var context = new DefaultHttpContext();

        var (ctx, problem, _) = await ExecutePipelineAsync(middleware, context);

        Assert.Equal((int)HttpStatusCode.Conflict, ctx.Response.StatusCode);
        Assert.Equal("Resource Conflict", problem.Title);
        Assert.Contains("already been allocated", problem.Detail);
    }

    // =========================================================================
    // 8: Expired Holds (410 Gone)
    // =========================================================================

    [Fact]
    public async Task ExpiredHold_Returns410Gone()
    {
        RequestDelegate next = _ => throw new InvalidOperationException("Seat hold has expired. The 10-minute reservation window elapsed.");

        var (middleware, _) = CreateMiddleware(next, "Production");
        var context = new DefaultHttpContext();

        var (ctx, problem, _) = await ExecutePipelineAsync(middleware, context);

        Assert.Equal((int)HttpStatusCode.Gone, ctx.Response.StatusCode);
        Assert.Equal("Hold Expired or Invalid", problem.Title);
        Assert.Contains("Seat hold has expired", problem.Detail);
    }

    // =========================================================================
    // 9: Safe Validation Messages (400 Bad Request & 404 Not Found)
    // =========================================================================

    [Fact]
    public async Task ValidationArgumentException_Returns400BadRequest()
    {
        RequestDelegate next = _ => throw new ArgumentException("ServiceId must not be empty");

        var (middleware, _) = CreateMiddleware(next, "Production");
        var context = new DefaultHttpContext();

        var (ctx, problem, _) = await ExecutePipelineAsync(middleware, context);

        Assert.Equal((int)HttpStatusCode.BadRequest, ctx.Response.StatusCode);
        Assert.Equal("Validation Error", problem.Title);
        Assert.Contains("ServiceId must not be empty", problem.Detail);
    }

    [Fact]
    public async Task KeyNotFoundException_Returns404NotFound()
    {
        RequestDelegate next = _ => throw new KeyNotFoundException("Transit schedule 'SRV-COL-ELLA-0800' was not found");

        var (middleware, _) = CreateMiddleware(next, "Production");
        var context = new DefaultHttpContext();

        var (ctx, problem, _) = await ExecutePipelineAsync(middleware, context);

        Assert.Equal((int)HttpStatusCode.NotFound, ctx.Response.StatusCode);
        Assert.Equal("Resource Not Found", problem.Title);
        Assert.Contains("SRV-COL-ELLA-0800", problem.Detail);
    }

    // =========================================================================
    // 10: TraceId Injection & Secure Server Logging
    // =========================================================================

    [Fact]
    public async Task ProblemDetails_IncludesTraceIdExtension()
    {
        RequestDelegate next = _ => throw new InvalidOperationException("General bad request");

        var (middleware, _) = CreateMiddleware(next, "Production");
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-test-uuid-9999";

        var (_, problem, _) = await ExecutePipelineAsync(middleware, context);

        Assert.True(problem.Extensions.ContainsKey("traceId"));
        Assert.Equal("trace-test-uuid-9999", problem.Extensions["traceId"]?.ToString());
    }

    // =========================================================================
    // 11: SensitiveDataSanitizer Redaction Verification
    // =========================================================================

    [Theory]
    [InlineData("Host=db.prod;Password=Secret123!;Database=waypoint;", "Password=[REDACTED];")]
    [InlineData("postgresql://user:SecretPass99@db.prod:5432/waypoint", "postgresql://[REDACTED_CONNECTION_STRING]")]
    [InlineData("Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.dummyPayload.dummySig", "Bearer [REDACTED_TOKEN]")]
    [InlineData("User card 4532-0150-0000-0001 charged successfully", "[REDACTED_CARD]")]
    [InlineData("SELECT * FROM Users WHERE Password='ClearText'", "[REDACTED_SQL_STATEMENT]")]
    [InlineData("Error occurred at C:\\Projects\\WayPoint\\SecretFile.cs line 55", "[REDACTED_PATH]")]
    public void SensitiveDataSanitizer_RedactsCredentialsCorrectly(string input, string expectedRedactedSubstring)
    {
        var sanitized = SensitiveDataSanitizer.Sanitize(input);

        Assert.Contains(expectedRedactedSubstring, sanitized);
        Assert.False(SensitiveDataSanitizer.ContainsSensitiveInformation(sanitized));
    }
}
