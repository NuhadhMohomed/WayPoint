using System.Text.RegularExpressions;

namespace WayPoint.API.Middleware;

/// <summary>
/// Security Fix 06 — Sanitizer for stripping sensitive data, secrets, database details,
/// and internal file system paths from exception messages and error responses.
/// </summary>
public static class SensitiveDataSanitizer
{
    private static readonly Regex JwtRegex = new(
        @"eyJ[A-Za-z0-9-_=]+\.[A-Za-z0-9-_=]+\.[A-Za-z0-9-_.+/=]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex BearerRegex = new(
        @"Bearer\s+(?!\[REDACTED)[A-Za-z0-9\-._~+/]+=*",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ConnectionStringRegex = new(
        @"(?i)(Host|Server|Database|User\s*Id|Username|Password|Port|User\s*ID)\s*=\s*(?!\[REDACTED\])[^;]+;?",
        RegexOptions.Compiled);

    private static readonly Regex PostgresUriRegex = new(
        @"postgres(?:ql)?://(?!\[REDACTED)[^\s""'@]+:[^\s""'@]+@[^\s""'/]+/[^\s""'?]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PasswordFieldRegex = new(
        @"(?i)([""']?(?:password|secret|client_secret|api_?key|jwt_?secret)[""']?\s*[:=]\s*[""']?)(?!\[REDACTED)([^""';,\s]+)([""']?)",
        RegexOptions.Compiled);

    private static readonly Regex WindowsPathRegex = new(
        @"[a-zA-Z]:\\(?!\[REDACTED)(?:[^\\\/:*?""<>|\r\n]+\\)*[^\\\/:*?""<>|\r\n]*",
        RegexOptions.Compiled);

    private static readonly Regex UnixPathRegex = new(
        @"/(?:home|usr|etc|var|tmp|app|src|root)/(?!\[REDACTED)[a-zA-Z0-9_\-./]+",
        RegexOptions.Compiled);

    private static readonly Regex CreditCardRegex = new(
        @"\b(?:\d[ -]*?){13,16}\b",
        RegexOptions.Compiled);

    private static readonly Regex SqlStatementRegex = new(
        @"(?i)\b(?:SELECT|INSERT\s+INTO|UPDATE\s+\w+\s+SET|DELETE\s+FROM|CREATE\s+TABLE|DROP\s+TABLE|ALTER\s+TABLE)\b[^;\r\n]*",
        RegexOptions.Compiled);

    private static readonly Regex DatabaseInternalDetailsRegex = new(
        @"(?i)(?:duplicate key value violates unique constraint|relation ""[^""]+"" does not exist|column ""[^""]+"" of relation|PostgreSQL Error Code: \w+)",
        RegexOptions.Compiled);

    /// <summary>
    /// Strips sensitive credentials, database connection strings, JWTs, card numbers,
    /// and file paths from the provided text.
    /// </summary>
    public static string Sanitize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var sanitized = text;

        // 1. Redact Connection Strings and DB URIs
        sanitized = ConnectionStringRegex.Replace(sanitized, "$1=[REDACTED];");
        sanitized = PostgresUriRegex.Replace(sanitized, "postgresql://[REDACTED_CONNECTION_STRING]");

        // 2. Redact Bearer and JWT tokens
        sanitized = BearerRegex.Replace(sanitized, "Bearer [REDACTED_TOKEN]");
        sanitized = JwtRegex.Replace(sanitized, "[REDACTED_JWT]");

        // 3. Redact Password / Secret fields
        sanitized = PasswordFieldRegex.Replace(sanitized, "$1[REDACTED_SECRET]$3");

        // 4. Redact Credit Card Numbers
        sanitized = CreditCardRegex.Replace(sanitized, "[REDACTED_CARD]");

        // 5. Redact Raw SQL statements
        sanitized = SqlStatementRegex.Replace(sanitized, "[REDACTED_SQL_STATEMENT]");

        // 6. Redact Database internal details
        sanitized = DatabaseInternalDetailsRegex.Replace(sanitized, "[DATABASE_OPERATION_FAILED]");

        // 7. Redact File System Paths
        sanitized = WindowsPathRegex.Replace(sanitized, "[REDACTED_PATH]");
        sanitized = UnixPathRegex.Replace(sanitized, "[REDACTED_PATH]");

        return sanitized;
    }

    /// <summary>
    /// Checks if the text contains sensitive patterns.
    /// </summary>
    public static bool ContainsSensitiveInformation(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        if (JwtRegex.IsMatch(text)) return true;
        if (BearerRegex.IsMatch(text)) return true;
        if (PostgresUriRegex.IsMatch(text)) return true;
        if (CreditCardRegex.IsMatch(text)) return true;
        if (PasswordFieldRegex.IsMatch(text)) return true;
        if (ConnectionStringRegex.IsMatch(text)) return true;
        if (WindowsPathRegex.IsMatch(text)) return true;
        if (UnixPathRegex.IsMatch(text)) return true;

        return false;
    }
}
