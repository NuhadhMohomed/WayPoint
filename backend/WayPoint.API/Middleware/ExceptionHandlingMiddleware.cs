using System.Data.Common;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WayPoint.API.Middleware;

/// <summary>
/// Security Fix 06 — Production Error Information Exposure & Centralized Exception Handling.
/// Ensures all exceptions return standardized RFC 7807 ProblemDetails responses without
/// leaking internal implementation details, connection strings, SQL queries, or stack traces in production.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("Request was cancelled by client: {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
        }
        catch (Exception ex)
        {
            // Secure server-side logging: Logs full exception and TraceIdentifier,
            // never logging sensitive headers (Authorization) or body payloads.
            _logger.LogError(ex,
                "Unhandled exception occurred while processing {Method} {Path} [TraceId: {TraceId}]",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);

            if (!context.Response.HasStarted)
            {
                await HandleExceptionAsync(context, ex);
            }
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, title, detail) = MapException(context, exception);

        context.Response.StatusCode = (int)statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        // Standard trace identifier for client tracking without exposing internal details
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        var responseJson = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(responseJson);
    }

    private (HttpStatusCode statusCode, string title, string detail) MapException(HttpContext context, Exception exception)
    {
        var isDevelopment = _environment.IsDevelopment();

        // 1. KeyNotFoundException -> 404 Not Found
        if (exception is KeyNotFoundException knf)
        {
            return (
                HttpStatusCode.NotFound,
                "Resource Not Found",
                SensitiveDataSanitizer.Sanitize(knf.Message)
            );
        }

        // 2. UnauthorizedAccessException -> 403 Forbidden (if authenticated) or 401 Unauthorized
        if (exception is UnauthorizedAccessException uae)
        {
            var isAuthenticated = context.User?.Identity?.IsAuthenticated == true;
            return (
                isAuthenticated ? HttpStatusCode.Forbidden : HttpStatusCode.Unauthorized,
                isAuthenticated ? "Access Denied" : "Unauthorized",
                SensitiveDataSanitizer.Sanitize(uae.Message)
            );
        }

        // 3. Database Concurrency & Constraint Exceptions -> 409 Conflict or 500
        if (exception is DbUpdateConcurrencyException)
        {
            return (
                HttpStatusCode.Conflict,
                "Concurrency Conflict",
                "A concurrency conflict occurred. The resource was modified or locked by another operation."
            );
        }

        if (exception is DbUpdateException dbUpdateEx)
        {
            var innerMsg = dbUpdateEx.InnerException?.Message ?? dbUpdateEx.Message;
            if (IsUniqueConstraintViolation(innerMsg))
            {
                return (
                    HttpStatusCode.Conflict,
                    "Resource Conflict",
                    "A concurrency conflict occurred. The requested resource or seat has already been allocated."
                );
            }

            return (
                HttpStatusCode.InternalServerError,
                "Internal Server Error",
                isDevelopment
                    ? SensitiveDataSanitizer.Sanitize(dbUpdateEx.Message)
                    : "A database error occurred while processing your request. Please try again later."
            );
        }

        if (exception is DbException dbEx)
        {
            if (IsUniqueConstraintViolation(dbEx.Message))
            {
                return (
                    HttpStatusCode.Conflict,
                    "Resource Conflict",
                    "A concurrency conflict occurred. The requested resource has already been allocated."
                );
            }

            return (
                HttpStatusCode.InternalServerError,
                "Internal Server Error",
                isDevelopment
                    ? SensitiveDataSanitizer.Sanitize(dbEx.Message)
                    : "A database error occurred while processing your request. Please try again later."
            );
        }

        // 4. InvalidOperationException -> 410 Gone, 409 Conflict, or 400 Bad Request
        if (exception is InvalidOperationException invEx)
        {
            var msg = invEx.Message;

            if (msg.Contains("expired", StringComparison.OrdinalIgnoreCase))
            {
                return (
                    HttpStatusCode.Gone,
                    "Hold Expired or Invalid",
                    SensitiveDataSanitizer.Sanitize(msg)
                );
            }

            if (msg.Contains("conflict", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("already exists", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("already assigned", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("already booked", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("already cancelled", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("already held", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
            {
                return (
                    HttpStatusCode.Conflict,
                    "Conflict",
                    SensitiveDataSanitizer.Sanitize(msg)
                );
            }

            if (msg.Contains("payment", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("transaction", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("reservation group", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("isolation", StringComparison.OrdinalIgnoreCase))
            {
                return (
                    HttpStatusCode.BadRequest,
                    "Payment Verification Failed",
                    SensitiveDataSanitizer.Sanitize(msg)
                );
            }

            return (
                HttpStatusCode.BadRequest,
                "Bad Request",
                SensitiveDataSanitizer.Sanitize(msg)
            );
        }

        // 5. Validation and Argument Exceptions -> 400 Bad Request
        if (exception is ArgumentException argEx)
        {
            return (
                HttpStatusCode.BadRequest,
                "Validation Error",
                SensitiveDataSanitizer.Sanitize(argEx.Message)
            );
        }

        if (exception is BadHttpRequestException badReq)
        {
            return (
                HttpStatusCode.BadRequest,
                "Bad Request",
                SensitiveDataSanitizer.Sanitize(badReq.Message)
            );
        }

        // 6. Generic / Unexpected Exceptions -> 500 Internal Server Error
        // In production: Return strictly generic safe message with zero internal details
        // In development: Return sanitized exception message without sensitive connection strings/paths
        return (
            HttpStatusCode.InternalServerError,
            "Internal Server Error",
            isDevelopment
                ? SensitiveDataSanitizer.Sanitize(exception.Message)
                : "An unexpected error occurred while processing your request. Please try again later."
        );
    }

    private static bool IsUniqueConstraintViolation(string message)
    {
        return message.Contains("23505", StringComparison.OrdinalIgnoreCase) // Postgres unique_violation code
            || message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase)
            || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase);
    }
}
