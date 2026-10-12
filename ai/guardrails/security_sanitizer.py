"""
WayPoint AI — Security Sanitizer & Secret Redaction Guardrail (Security Fix 05).

Provides redaction and masking for sensitive credentials:
- Passwords
- JWT Bearer tokens
- Google Gemini / OpenAI API keys
- Database connection strings
- Logging filter to prevent secret leakage in application logs
"""

import logging
import re
from typing import Any

# Regex patterns for sensitive data redaction
_JWT_PATTERN = re.compile(
    r"eyJ[A-Za-z0-9-_=]+\.[A-Za-z0-9-_=]+\.[A-Za-z0-9-_.+/=]+",
    re.MULTILINE,
)

_BEARER_PATTERN = re.compile(
    r"Bearer\s+[A-Za-z0-9\-._~+/]+=*",
    re.IGNORECASE,
)

_API_KEY_PATTERNS = [
    re.compile(r"AIza[0-9A-Za-z-_]{35}"),  # Google API key
    re.compile(r"sk-[A-Za-z0-9]{20,}"),     # OpenAI / general sk- key
]

_PASSWORD_FIELD_PATTERN = re.compile(
    r'(?i)(["\']?(?:password|client_secret|secret|api_?key)["\']?\s*[:=]\s*["\'])([^"\']+)(["\'])'
)

_CONNECTION_STRING_PATTERN = re.compile(
    r'(?i)(postgres(?:ql)?://[^:]+:)([^@]+)(@)'
)


def mask_secret(value: str | None, visible_prefix: int = 3, visible_suffix: int = 3) -> str:
    """
    Mask a sensitive string for safe display/logging.

    Example: 'sk-abcdef123456789' -> 'sk-...789'
    """
    if not value:
        return "[EMPTY]"
    if len(value) <= visible_prefix + visible_suffix:
        return "********"
    return f"{value[:visible_prefix]}...{value[-visible_suffix:]}"


def sanitize_text(text: str | None) -> str:
    """
    Redact secrets, passwords, JWT tokens, and connection strings from arbitrary text.

    Args:
        text: Raw text that may contain sensitive credentials.

    Returns:
        Sanitized text with all credentials replaced with safe redaction placeholders.
    """
    if not text:
        return ""

    sanitized = str(text)

    # 1. Redact JWT tokens
    sanitized = _JWT_PATTERN.sub("[REDACTED_JWT]", sanitized)

    # 2. Redact Authorization Bearer headers
    sanitized = _BEARER_PATTERN.sub("Bearer [REDACTED_TOKEN]", sanitized)

    # 3. Redact API keys
    for pattern in _API_KEY_PATTERNS:
        sanitized = pattern.sub("[REDACTED_API_KEY]", sanitized)

    # 4. Redact password/secret key-value fields in JSON or query-like strings
    sanitized = _PASSWORD_FIELD_PATTERN.sub(r"\1[REDACTED_CREDENTIAL]\3", sanitized)

    # 5. Redact passwords in connection strings
    sanitized = _CONNECTION_STRING_PATTERN.sub(r"\1[REDACTED_PASSWORD]\3", sanitized)

    return sanitized


def sanitize_dict(data: dict[str, Any]) -> dict[str, Any]:
    """
    Recursively sanitize dictionary values to prevent credential leakage.
    """
    sanitized: dict[str, Any] = {}
    for key, value in data.items():
        lower_key = str(key).lower()
        if any(secret_term in lower_key for secret_term in ("password", "secret", "token", "apikey", "api_key")):
            sanitized[key] = "[REDACTED]"
        elif isinstance(value, dict):
            sanitized[key] = sanitize_dict(value)
        elif isinstance(value, str):
            sanitized[key] = sanitize_text(value)
        elif isinstance(value, list):
            sanitized[key] = [
                sanitize_dict(item) if isinstance(item, dict)
                else sanitize_text(item) if isinstance(item, str)
                else item
                for item in value
            ]
        else:
            sanitized[key] = value
    return sanitized


class SensitiveDataLogFilter(logging.Filter):
    """
    Logging filter that intercepts and sanitizes log records before output.
    Ensures passwords, tokens, API keys, and connection strings are never written to logs.
    """

    def filter(self, record: logging.LogRecord) -> bool:
        if isinstance(record.msg, str):
            record.msg = sanitize_text(record.msg)
        if record.args:
            if isinstance(record.args, dict):
                record.args = sanitize_dict(record.args)
            elif isinstance(record.args, tuple):
                record.args = tuple(
                    sanitize_text(a) if isinstance(a, str) else a
                    for a in record.args
                )
        return True
