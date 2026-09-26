"""
WayPoint AI Microservice — Centralised Configuration.

Loads environment variables from .env file and provides typed
configuration constants used across the AI subsystem.
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


# ---------------------------------------------------------------------------
# Backend API Configuration
# ---------------------------------------------------------------------------
API_BASE_URL: str = os.getenv("API_BASE_URL", "http://localhost:5010/api/v1")

# ---------------------------------------------------------------------------
# Google Gemini LLM Configuration
# ---------------------------------------------------------------------------
GEMINI_API_KEY: str = os.getenv("GEMINI_API_KEY", "")
LLM_MODEL: str = os.getenv("LLM_MODEL", "gemini-2.0-flash")
LLM_TEMPERATURE: float = float(os.getenv("LLM_TEMPERATURE", "0.1"))

# ---------------------------------------------------------------------------
# AI Service Account Credentials (for authenticated backend tool calls)
# ---------------------------------------------------------------------------
AI_SERVICE_EMAIL: str = os.getenv("AI_SERVICE_EMAIL", "ai-agent@waypoint.lk")
AI_SERVICE_PASSWORD: str = os.getenv(
    "AI_SERVICE_PASSWORD", "WayPoint_AI_Service_2026"
)

# ---------------------------------------------------------------------------
# Execution Guardrails (NFR-PERF-004, BR-AIVAL-002)
# ---------------------------------------------------------------------------
AI_EXECUTION_TIMEOUT_SECONDS: int = int(
    os.getenv("AI_EXECUTION_TIMEOUT_SECONDS", "10")
)
AI_MAX_RETRIES: int = int(os.getenv("AI_MAX_RETRIES", "3"))
HTTP_TIMEOUT_SECONDS: int = int(os.getenv("HTTP_TIMEOUT_SECONDS", "8"))
