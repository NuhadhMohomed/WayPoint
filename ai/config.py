"""
WayPoint AI Microservice — Centralised Configuration.

Loads environment variables from .env file and provides typed
configuration constants with strict security validation for staging and production.
Security Fix 05: Credentials Security & Least-Privilege Service Account.
"""

import os
from pathlib import Path
from dotenv import load_dotenv

# ---------------------------------------------------------------------------
# Load .env — ai/.env first, then root .env as fallback
# ---------------------------------------------------------------------------
_ai_env = Path(__file__).parent / ".env"
_root_env = Path(__file__).parent.parent / ".env"

if _ai_env.exists():
    load_dotenv(_ai_env)
if _root_env.exists():
    load_dotenv(_root_env, override=False)


class ConfigurationError(ValueError):
    """Raised when mandatory security configurations are invalid or missing."""
    pass


# ---------------------------------------------------------------------------
# Environment Awareness (Security Fix 05)
# ---------------------------------------------------------------------------
ENVIRONMENT: str = (
    os.getenv("ENVIRONMENT")
    or os.getenv("APP_ENV")
    or os.getenv("ASPNETCORE_ENVIRONMENT")
    or "development"
).lower().strip()

IS_PRODUCTION_OR_STAGING: bool = ENVIRONMENT in ("production", "staging", "prod")

# Insecure credentials that must NEVER be used in production or staging
DEFAULT_INSECURE_PASSWORDS: set[str] = {
    "password123!",
    "admin@123",
    "password",
    "admin",
    "waypoint_ai_service_2026",
    "123456",
    "secret",
}

DEFAULT_INSECURE_EMAILS: set[str] = {
    "admin@waypoint.lk",
    "root@waypoint.lk",
    "test@waypoint.lk",
}

PLACEHOLDER_API_KEYS: set[str] = {
    "your_gemini_api_key_here",
    "your_openai_api_key_here",
    "your_ai_service_api_key_here",
    "placeholder",
    "dummy",
    "test",
    "changeme",
}

DEFAULT_AI_SERVICE_EMAIL: str = "operator@waypoint.lk"
DEFAULT_AI_SERVICE_PASSWORD: str = "Password123!"

# ---------------------------------------------------------------------------
# Backend API Configuration
# ---------------------------------------------------------------------------
API_BASE_URL: str = os.getenv("API_BASE_URL", "http://localhost:5010/api/v1")
BACKEND_ORIGIN: str = os.getenv("BACKEND_ORIGIN", "http://localhost:5010")

# ---------------------------------------------------------------------------
# Google Gemini / LLM Configuration
# ---------------------------------------------------------------------------
GEMINI_API_KEY: str = os.getenv("GEMINI_API_KEY") or os.getenv("OPENAI_API_KEY", "")
LLM_MODEL: str = os.getenv("LLM_MODEL", "gemini-3.5-flash")
LLM_TEMPERATURE: float = float(os.getenv("LLM_TEMPERATURE", "0.1"))

# ---------------------------------------------------------------------------
# Service-to-Service Authentication Key (FastAPI endpoints)
# ---------------------------------------------------------------------------
AI_SERVICE_API_KEY: str | None = os.getenv("AI_SERVICE_API_KEY") or os.getenv("AI_API_KEY")

# ---------------------------------------------------------------------------
# AI Service Account Credentials (for authenticated backend tool calls)
# Least privilege: In development, defaults to operator@waypoint.lk, NOT admin.
# In production/staging, no insecure fallbacks are provided.
# ---------------------------------------------------------------------------
if IS_PRODUCTION_OR_STAGING:
    AI_SERVICE_EMAIL: str = os.getenv("AI_SERVICE_EMAIL", "")
    AI_SERVICE_PASSWORD: str = os.getenv("AI_SERVICE_PASSWORD", "")
else:
    # Development fallback uses least-privilege Operator role, never Admin
    AI_SERVICE_EMAIL = os.getenv("AI_SERVICE_EMAIL", DEFAULT_AI_SERVICE_EMAIL)
    AI_SERVICE_PASSWORD = os.getenv("AI_SERVICE_PASSWORD", DEFAULT_AI_SERVICE_PASSWORD)

# ---------------------------------------------------------------------------
# Execution Guardrails (NFR-PERF-004, BR-AIVAL-002)
# ---------------------------------------------------------------------------
AI_EXECUTION_TIMEOUT_SECONDS: int = int(
    os.getenv("AI_EXECUTION_TIMEOUT_SECONDS", "30")
)
AI_MAX_RETRIES: int = int(os.getenv("AI_MAX_RETRIES", "3"))
HTTP_TIMEOUT_SECONDS: int = int(os.getenv("HTTP_TIMEOUT_SECONDS", "8"))


def get_environment() -> str:
    """Return normalized current environment name."""
    return (
        os.getenv("ENVIRONMENT")
        or os.getenv("APP_ENV")
        or os.getenv("ASPNETCORE_ENVIRONMENT")
        or ENVIRONMENT
    ).lower().strip()


def is_production_or_staging(env: str | None = None) -> bool:
    """Check if given or current environment is production or staging."""
    current_env = (env or get_environment()).lower().strip()
    return current_env in ("production", "staging", "prod")


def is_placeholder_key(key: str | None) -> bool:
    """Check if an API key contains known placeholder or insecure patterns."""
    if not key:
        return True
    k = key.lower().strip()
    if k in PLACEHOLDER_API_KEYS:
        return True
    return any(p in k for p in ("placeholder", "changeme", "your-", "your_", "insecure", "example", "dummy"))


def validate_config(raise_on_error: bool = True) -> bool:
    """
    Validate security configuration for the current environment.

    In production and staging:
    - AI_SERVICE_EMAIL must be explicitly provided and not an administrative superuser account.
    - AI_SERVICE_PASSWORD must be explicitly provided and not a known default password.
    - GEMINI_API_KEY must be provided and not a placeholder.
    - AI_SERVICE_API_KEY must be provided for service-to-service authentication.
    - Startup fails safely if any check fails.

    Returns:
        True if configuration is valid.
    Raises:
        ConfigurationError if invalid and raise_on_error is True.
    """
    errors: list[str] = []

    current_env = get_environment()
    is_prod = is_production_or_staging(current_env)

    # In production/staging, check environment variables directly so tests and dynamic boots are enforced
    email = os.getenv("AI_SERVICE_EMAIL") if is_prod else (os.getenv("AI_SERVICE_EMAIL") or AI_SERVICE_EMAIL)
    password = os.getenv("AI_SERVICE_PASSWORD") if is_prod else (os.getenv("AI_SERVICE_PASSWORD") or AI_SERVICE_PASSWORD)
    gemini_key = os.getenv("GEMINI_API_KEY") or os.getenv("OPENAI_API_KEY") or (GEMINI_API_KEY if not is_prod else "")
    service_api_key = os.getenv("AI_SERVICE_API_KEY") or os.getenv("AI_API_KEY") or (AI_SERVICE_API_KEY if not is_prod else "")

    if is_prod:
        # 1. AI Service Email Validation
        if not email or not email.strip():
            errors.append("AI_SERVICE_EMAIL is required in production/staging environments.")
        elif email.lower().strip() in DEFAULT_INSECURE_EMAILS:
            errors.append(
                f"AI_SERVICE_EMAIL ({email}) must not use default superuser administrative accounts. "
                "Enforce least-privilege service account."
            )

        # 2. AI Service Password Validation
        if not password or not password.strip():
            errors.append("AI_SERVICE_PASSWORD is required in production/staging environments.")
        elif password.lower().strip() in DEFAULT_INSECURE_PASSWORDS:
            errors.append("AI_SERVICE_PASSWORD must not use known default or insecure passwords in production.")

        # 3. Gemini / LLM API Key Validation
        if not gemini_key or not gemini_key.strip():
            errors.append("GEMINI_API_KEY is required in production/staging environments.")
        elif is_placeholder_key(gemini_key):
            errors.append("GEMINI_API_KEY contains a placeholder or insecure value.")

        # 4. Service-to-Service API Key Validation
        if not service_api_key or not service_api_key.strip():
            errors.append("AI_SERVICE_API_KEY is required in production/staging environments.")
        elif is_placeholder_key(service_api_key):
            errors.append("AI_SERVICE_API_KEY contains a placeholder or insecure value.")

    if errors:
        formatted = "; ".join(errors)
        if raise_on_error:
            raise ConfigurationError(f"Security Configuration Validation Failed: {formatted}")
        return False

    return True
