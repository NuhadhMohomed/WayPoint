"""
WayPoint AI — Shared HTTP Client for Backend Tool Calls.

Provides an authenticated httpx.AsyncClient that:
- Targets API_BASE_URL from config
- Authenticates via POST /api/v1/auth/login with AI service account
- Caches the JWT and injects Bearer token into all requests
- Re-authenticates on 401 (token expired)
- Returns structured error dicts on failure (RFC 7807 ProblemDetails)
"""

import logging
from typing import Any

import httpx

from config import (
    API_BASE_URL,
    AI_SERVICE_EMAIL,
    AI_SERVICE_PASSWORD,
    HTTP_TIMEOUT_SECONDS,
)

logger = logging.getLogger("waypoint.ai.http")

# Module-level token cache
_cached_token: str | None = None


async def _authenticate(client: httpx.AsyncClient) -> str:
    """
    Authenticate with the backend using the AI service account.

    Calls POST /api/v1/auth/login (see api-design.md §4.1).
    Response: { token, expiresAt, user: { userId, email, fullName, role } }

    Returns:
        JWT bearer token string.
    """
    global _cached_token

    if _cached_token:
        return _cached_token

    try:
        response = await client.post(
            f"{API_BASE_URL}/auth/login",
            json={
                "email": AI_SERVICE_EMAIL,
                "password": AI_SERVICE_PASSWORD,
            },
        )
        response.raise_for_status()
        data = response.json()
        _cached_token = data.get("token", "")
        logger.info("AI service account authenticated successfully")
        return _cached_token
    except httpx.HTTPStatusError as exc:
        logger.error(
            "AI service account authentication failed: %s",
            exc.response.text,
        )
        raise RuntimeError(
            f"Failed to authenticate AI service account: "
            f"{exc.response.status_code}"
        ) from exc


def clear_token_cache() -> None:
    """Clear the cached JWT token (e.g. on 401 responses)."""
    global _cached_token
    _cached_token = None


async def make_tool_request(
    method: str,
    path: str,
    *,
    json_body: dict[str, Any] | None = None,
    params: dict[str, Any] | None = None,
) -> dict[str, Any]:
    """
    Make an authenticated HTTP request to the backend API.

    Args:
        method: HTTP method (GET, POST, PUT, DELETE).
        path: API path relative to base URL (e.g. "/routes").
        json_body: JSON request body for POST/PUT.
        params: Query parameters for GET.

    Returns:
        Parsed JSON response as a dictionary.
        On error, returns ``{"error": True, "status_code": ..., "detail": ...}``.
    """
    async with httpx.AsyncClient(
        base_url=API_BASE_URL,
        timeout=httpx.Timeout(HTTP_TIMEOUT_SECONDS),
    ) as client:
        # Authenticate
        token = await _authenticate(client)
        client.headers["Authorization"] = f"Bearer {token}"

        try:
            response = await client.request(
                method=method,
                url=path,
                json=json_body,
                params=params,
            )

            # Handle 401 — token expired, re-authenticate once
            if response.status_code == 401:
                logger.warning("JWT expired, re-authenticating...")
                clear_token_cache()
                token = await _authenticate(client)
                client.headers["Authorization"] = f"Bearer {token}"
                response = await client.request(
                    method=method,
                    url=path,
                    json=json_body,
                    params=params,
                )

            response.raise_for_status()

            # Handle empty responses (204 No Content, etc.)
            if response.status_code == 204 or not response.content:
                return {"success": True}

            return response.json()

        except httpx.HTTPStatusError as exc:
            error_body: dict[str, Any] = {}
            try:
                error_body = exc.response.json()
            except Exception:
                error_body = {"detail": exc.response.text}

            logger.error(
                "Backend tool request failed: %s %s → %s: %s",
                method,
                path,
                exc.response.status_code,
                error_body,
            )
            return {
                "error": True,
                "status_code": exc.response.status_code,
                "detail": error_body.get("detail", str(exc)),
                "title": error_body.get("title", "Tool Request Failed"),
            }

        except httpx.TimeoutException:
            logger.error(
                "Backend tool request timed out: %s %s", method, path
            )
            return {
                "error": True,
                "status_code": 408,
                "detail": (
                    f"Request to {path} timed out "
                    f"after {HTTP_TIMEOUT_SECONDS}s"
                ),
                "title": "Timeout",
            }
