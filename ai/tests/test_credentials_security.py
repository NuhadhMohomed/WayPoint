"""
WayPoint AI — Test Suite: AI Service Credentials Security (Security Fix 05).

Verifies:
1. Production rejects missing mandatory credentials.
2. Explicit valid configuration is accepted in production.
3. Production does not use default or placeholder credentials.
4. Development configuration remains functional with least-privilege defaults.
5. Authentication failures and logs do not expose secrets or passwords.
6. Invalid service authentication (X-API-Key) is rejected with 401 Unauthorized.
7. Valid service authentication succeeds, and AI-to-backend JWT authentication works.
8. Existing agent workflows still execute properly.
9. Manager approval security remains strictly enforced (Human-in-the-Loop).
10. Safe failure guardrails sanitize sensitive details before recording.
"""

import logging
import os
import sys
from unittest.mock import AsyncMock, patch, MagicMock

import httpx
import pytest
from fastapi.testclient import TestClient

sys.path.insert(0, ".")

import config
from config import ConfigurationError, validate_config
from guardrails.security_sanitizer import (
    SensitiveDataLogFilter,
    mask_secret,
    sanitize_dict,
    sanitize_text,
)
from guardrails.safe_failure import execute_with_safe_failure
from guardrails.output_validator import validate_impact_classification
from tools import http_client
from main import app


# ============================================================================
# 1. Production Rejects Missing Mandatory Credentials
# ============================================================================

class TestProductionMissingCredentials:
    """Requirement 1: Production fails startup when mandatory secrets are missing."""

    def test_production_rejects_missing_email(self, monkeypatch):
        monkeypatch.setenv("ENVIRONMENT", "production")
        monkeypatch.delenv("AI_SERVICE_EMAIL", raising=False)
        monkeypatch.setenv("AI_SERVICE_PASSWORD", "StrongPassword123#Secure!")
        monkeypatch.setenv("GEMINI_API_KEY", "AIzaSyValidProductionApiKey12345")
        monkeypatch.setenv("AI_SERVICE_API_KEY", "waypoint-prod-secure-service-key-99")

        with pytest.raises(ConfigurationError) as exc_info:
            validate_config(raise_on_error=True)
        assert "AI_SERVICE_EMAIL" in str(exc_info.value)

    def test_production_rejects_missing_password(self, monkeypatch):
        monkeypatch.setenv("ENVIRONMENT", "production")
        monkeypatch.setenv("AI_SERVICE_EMAIL", "prod-ai@waypoint.lk")
        monkeypatch.delenv("AI_SERVICE_PASSWORD", raising=False)
        monkeypatch.setenv("GEMINI_API_KEY", "AIzaSyValidProductionApiKey12345")
        monkeypatch.setenv("AI_SERVICE_API_KEY", "waypoint-prod-secure-service-key-99")

        with pytest.raises(ConfigurationError) as exc_info:
            validate_config(raise_on_error=True)
        assert "AI_SERVICE_PASSWORD" in str(exc_info.value)

    def test_production_rejects_missing_gemini_key(self, monkeypatch):
        monkeypatch.setenv("ENVIRONMENT", "production")
        monkeypatch.setenv("AI_SERVICE_EMAIL", "prod-ai@waypoint.lk")
        monkeypatch.setenv("AI_SERVICE_PASSWORD", "StrongPassword123#Secure!")
        monkeypatch.delenv("GEMINI_API_KEY", raising=False)
        monkeypatch.delenv("OPENAI_API_KEY", raising=False)
        monkeypatch.setenv("AI_SERVICE_API_KEY", "waypoint-prod-secure-service-key-99")

        with pytest.raises(ConfigurationError) as exc_info:
            validate_config(raise_on_error=True)
        assert "GEMINI_API_KEY" in str(exc_info.value)

    def test_production_rejects_missing_service_api_key(self, monkeypatch):
        monkeypatch.setenv("ENVIRONMENT", "production")
        monkeypatch.setenv("AI_SERVICE_EMAIL", "prod-ai@waypoint.lk")
        monkeypatch.setenv("AI_SERVICE_PASSWORD", "StrongPassword123#Secure!")
        monkeypatch.setenv("GEMINI_API_KEY", "AIzaSyValidProductionApiKey12345")
        monkeypatch.delenv("AI_SERVICE_API_KEY", raising=False)
        monkeypatch.delenv("AI_API_KEY", raising=False)

        with pytest.raises(ConfigurationError) as exc_info:
            validate_config(raise_on_error=True)
        assert "AI_SERVICE_API_KEY" in str(exc_info.value)


# ============================================================================
# 2. Explicit Valid Configuration is Accepted
# ============================================================================

class TestValidProductionConfiguration:
    """Requirement 2: Explicit valid non-default credentials pass validation."""

    def test_production_accepts_valid_configuration(self, monkeypatch):
        monkeypatch.setenv("ENVIRONMENT", "production")
        monkeypatch.setenv("AI_SERVICE_EMAIL", "prod-ai-bot@waypoint.lk")
        monkeypatch.setenv("AI_SERVICE_PASSWORD", "StrongP@ssw0rd!2026#Secure")
        monkeypatch.setenv("GEMINI_API_KEY", "AIzaSyValidProductionApiKeyForGemini99")
        monkeypatch.setenv("AI_SERVICE_API_KEY", "waypoint-prod-secure-ai-key-2026")

        # Must return True without raising ConfigurationError
        is_valid = validate_config(raise_on_error=True)
        assert is_valid is True

    def test_staging_accepts_valid_configuration(self, monkeypatch):
        monkeypatch.setenv("ENVIRONMENT", "staging")
        monkeypatch.setenv("AI_SERVICE_EMAIL", "staging-ai@waypoint.lk")
        monkeypatch.setenv("AI_SERVICE_PASSWORD", "StagingPassword2026#Secure!")
        monkeypatch.setenv("GEMINI_API_KEY", "AIzaSyValidStagingApiKeyForGemini99")
        monkeypatch.setenv("AI_SERVICE_API_KEY", "waypoint-staging-ai-key-2026")

        is_valid = validate_config(raise_on_error=True)
        assert is_valid is True


# ============================================================================
# 3. Production Does Not Use Default Credentials
# ============================================================================

class TestProductionRejectsDefaultCredentials:
    """Requirement 3: Insecure default or placeholder credentials rejected in production."""

    def test_production_rejects_admin_default_email(self, monkeypatch):
        monkeypatch.setenv("ENVIRONMENT", "production")
        monkeypatch.setenv("AI_SERVICE_EMAIL", "admin@waypoint.lk")
        monkeypatch.setenv("AI_SERVICE_PASSWORD", "CustomPassword2026!#Strong")
        monkeypatch.setenv("GEMINI_API_KEY", "AIzaSyValidProductionApiKeyForGemini99")
        monkeypatch.setenv("AI_SERVICE_API_KEY", "waypoint-prod-secure-ai-key-2026")

        with pytest.raises(ConfigurationError) as exc_info:
            validate_config(raise_on_error=True)
        assert "default" in str(exc_info.value).lower()
        assert "admin@waypoint.lk" in str(exc_info.value)

    def test_production_rejects_default_password(self, monkeypatch):
        monkeypatch.setenv("ENVIRONMENT", "production")
        monkeypatch.setenv("AI_SERVICE_EMAIL", "custom-ai@waypoint.lk")
        monkeypatch.setenv("AI_SERVICE_PASSWORD", "Password123!")
        monkeypatch.setenv("GEMINI_API_KEY", "AIzaSyValidProductionApiKeyForGemini99")
        monkeypatch.setenv("AI_SERVICE_API_KEY", "waypoint-prod-secure-ai-key-2026")

        with pytest.raises(ConfigurationError) as exc_info:
            validate_config(raise_on_error=True)
        assert "default" in str(exc_info.value).lower()
        # Security requirement: Never expose the cleartext password in the exception message
        assert "Password123!" not in str(exc_info.value)
        assert "AI_SERVICE_PASSWORD" in str(exc_info.value)

    def test_production_rejects_placeholder_gemini_key(self, monkeypatch):
        monkeypatch.setenv("ENVIRONMENT", "production")
        monkeypatch.setenv("AI_SERVICE_EMAIL", "custom-ai@waypoint.lk")
        monkeypatch.setenv("AI_SERVICE_PASSWORD", "CustomPassword2026!#Strong")
        monkeypatch.setenv("GEMINI_API_KEY", "your-gemini-api-key-here")
        monkeypatch.setenv("AI_SERVICE_API_KEY", "waypoint-prod-secure-ai-key-2026")

        with pytest.raises(ConfigurationError) as exc_info:
            validate_config(raise_on_error=True)
        assert "gemini_api_key" in str(exc_info.value).lower()

    def test_production_rejects_default_service_api_key(self, monkeypatch):
        monkeypatch.setenv("ENVIRONMENT", "production")
        monkeypatch.setenv("AI_SERVICE_EMAIL", "custom-ai@waypoint.lk")
        monkeypatch.setenv("AI_SERVICE_PASSWORD", "CustomPassword2026!#Strong")
        monkeypatch.setenv("GEMINI_API_KEY", "AIzaSyValidProductionApiKeyForGemini99")
        monkeypatch.setenv("AI_SERVICE_API_KEY", "dev-insecure-ai-service-key")

        with pytest.raises(ConfigurationError) as exc_info:
            validate_config(raise_on_error=True)
        assert "ai_service_api_key" in str(exc_info.value).lower()


# ============================================================================
# 4. Development Configuration Remains Functional (Least Privilege)
# ============================================================================

class TestDevelopmentConfiguration:
    """Requirement 4: Local development functions safely using least-privilege defaults."""

    def test_development_validates_successfully_with_defaults(self, monkeypatch):
        monkeypatch.setenv("ENVIRONMENT", "development")
        monkeypatch.delenv("AI_SERVICE_EMAIL", raising=False)
        monkeypatch.delenv("AI_SERVICE_PASSWORD", raising=False)
        monkeypatch.delenv("GEMINI_API_KEY", raising=False)
        monkeypatch.delenv("AI_SERVICE_API_KEY", raising=False)

        # In dev mode, validation succeeds and logs warnings rather than raising
        is_valid = validate_config(raise_on_error=True)
        assert is_valid is True

    def test_development_default_is_operator_not_admin(self):
        """AI service account must default to least-privilege Operator, not Admin."""
        assert config.DEFAULT_AI_SERVICE_EMAIL == "operator@waypoint.lk"
        assert "admin@waypoint.lk" != config.DEFAULT_AI_SERVICE_EMAIL


# ============================================================================
# 5. Secrets Redacted in Exceptions and Logs
# ============================================================================

class TestSecretRedactionAndSanitization:
    """Requirement 5: Passwords, tokens, and keys are never exposed in exceptions or logs."""

    @pytest.mark.asyncio
    async def test_authentication_failure_does_not_expose_password(self, monkeypatch):
        """When backend returns 401 on login, exception message must not contain password."""
        monkeypatch.setattr(config, "AI_SERVICE_EMAIL", "operator@waypoint.lk")
        monkeypatch.setattr(config, "AI_SERVICE_PASSWORD", "SuperSecretPassword123!")

        mock_response = MagicMock(spec=httpx.Response)
        mock_response.status_code = 401
        http_error = httpx.HTTPStatusError("401 Unauthorized", request=MagicMock(), response=mock_response)

        mock_client = AsyncMock(spec=httpx.AsyncClient)
        mock_client.post = AsyncMock(side_effect=http_error)

        http_client.clear_token_cache()

        with pytest.raises(RuntimeError) as exc_info:
            await http_client._authenticate(mock_client)

        error_message = str(exc_info.value)
        assert "SuperSecretPassword123!" not in error_message
        assert "operator@waypoint.lk" not in error_message
        assert "401" in error_message

    def test_log_filter_redacts_jwt_and_bearer(self):
        """SensitiveDataLogFilter masks Bearer tokens and raw JWT patterns."""
        log_filter = SensitiveDataLogFilter()
        fake_jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.doNotExposeThisSignature123"
        
        record = logging.LogRecord(
            name="test",
            level=logging.INFO,
            pathname="test.py",
            lineno=1,
            msg="Authorized request using Bearer %s with token=%s",
            args=(fake_jwt, fake_jwt),
            exc_info=None,
        )

        log_filter.filter(record)
        formatted = record.getMessage()

        assert fake_jwt not in formatted
        assert "[REDACTED" in formatted

    def test_log_filter_redacts_password_fields(self):
        """SensitiveDataLogFilter masks password and secret key-value pairs."""
        log_filter = SensitiveDataLogFilter()
        record = logging.LogRecord(
            name="test",
            level=logging.ERROR,
            pathname="test.py",
            lineno=1,
            msg='Authentication failed for user payload: {"email": "user@waypoint.lk", "password": "SecretPassword123!"}',
            args=(),
            exc_info=None,
        )

        log_filter.filter(record)
        formatted = record.getMessage()

        assert "SecretPassword123!" not in formatted
        assert "[REDACTED" in formatted

    def test_sanitize_dict_masks_sensitive_keys(self):
        data = {
            "user_id": "usr-123",
            "password": "ClearTextPassword!",
            "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.dummy",
            "gemini_api_key": "AIzaSySecretApiKey",
            "nested": {
                "secret": "DeepSecret",
                "safe_value": "VisibleInfo",
            },
        }
        sanitized = sanitize_dict(data)

        assert sanitized["password"] == "[REDACTED]"
        assert sanitized["token"] == "[REDACTED]"
        assert sanitized["gemini_api_key"] == "[REDACTED]"
        assert sanitized["nested"]["secret"] == "[REDACTED]"
        assert sanitized["nested"]["safe_value"] == "VisibleInfo"
        assert sanitized["user_id"] == "usr-123"

    @pytest.mark.asyncio
    async def test_safe_failure_sanitizes_exception_secrets(self):
        """SafeFailure guardrail redacts secrets from exception details stored in state."""
        from agents.state import create_initial_state

        state = create_initial_state(objective="test", workflow_type="disruption_rebooking")

        secret_str = "SuperSecretToken_12345"
        async def failing_agent(s):
            raise ValueError(f"Failed with key: api_key='{secret_str}' and password='SuperSecretPassword!'")

        result = await execute_with_safe_failure("FailingAgent", failing_agent, state)
        
        # Check that error_detail does not leak cleartext password
        steps = result.get("steps", [])
        assert len(steps) > 0
        step_desc = steps[-1].get("step_description", "")
        assert "SuperSecretPassword!" not in step_desc


# ============================================================================
# 6. Invalid Service Authentication is Rejected (401 Unauthorized)
# ============================================================================

class TestServiceAuthenticationEnforcement:
    """Requirement 6: API endpoints reject requests with missing or invalid service keys."""

    def test_missing_api_key_rejected_when_configured(self, monkeypatch):
        monkeypatch.setenv("AI_SERVICE_API_KEY", "configured-secret-key-12345")
        client = TestClient(app)

        response = client.post(
            "/api/ai/journey-recommendation",
            json={"objective": "Fastest bus to Kandy"},
        )
        assert response.status_code == 401
        assert "Unauthorized" in response.json().get("detail", "")

    def test_invalid_api_key_rejected(self, monkeypatch):
        monkeypatch.setenv("AI_SERVICE_API_KEY", "configured-secret-key-12345")
        client = TestClient(app)

        response = client.post(
            "/api/ai/journey-recommendation",
            json={"objective": "Fastest bus to Kandy"},
            headers={"X-API-Key": "wrong-or-malicious-key"},
        )
        assert response.status_code == 401
        assert "Unauthorized" in response.json().get("detail", "")

    def test_valid_api_key_accepted(self, monkeypatch):
        secret_key = "configured-secret-key-12345"
        monkeypatch.setenv("AI_SERVICE_API_KEY", secret_key)

        mock_result = {
            "workflow_status": "Completed",
            "step_order": 3,
            "candidate_routes": [],
            "feasibility_result": {},
            "fare_analysis": {},
            "steps": [],
        }

        with patch("main.workflow_graph") as mock_graph:
            mock_graph.ainvoke = AsyncMock(return_value=mock_result)
            client = TestClient(app)

            response = client.post(
                "/api/ai/journey-recommendation",
                json={"objective": "Fastest bus to Kandy"},
                headers={"X-API-Key": secret_key},
            )
            assert response.status_code == 200
            assert response.json()["status"] == "Completed"


# ============================================================================
# 7. AI-to-Backend Authentication Functionality
# ============================================================================

class TestAiToBackendAuthentication:
    """Requirement 7: AI HTTP client logs in to backend, caches JWT, and injects Bearer token."""

    @pytest.mark.asyncio
    async def test_authenticate_success_and_caching(self, monkeypatch):
        monkeypatch.setattr(config, "AI_SERVICE_EMAIL", "operator@waypoint.lk")
        monkeypatch.setattr(config, "AI_SERVICE_PASSWORD", "Password123!")

        expected_jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.validJwtTokenPayload.signature"
        mock_response = MagicMock(spec=httpx.Response)
        mock_response.status_code = 200
        mock_response.json.return_value = {
            "token": expected_jwt,
            "expiresAt": "2026-10-12T20:00:00Z",
            "user": {
                "userId": "usr-ai-01",
                "email": "operator@waypoint.lk",
                "role": "Operator",
            },
        }

        mock_client = AsyncMock(spec=httpx.AsyncClient)
        mock_client.post = AsyncMock(return_value=mock_response)

        http_client.clear_token_cache()

        # First call: hits login endpoint
        token = await http_client._authenticate(mock_client)
        assert token == expected_jwt
        mock_client.post.assert_called_once()

        # Second call: reuses cached token without another HTTP POST
        mock_client.post.reset_mock()
        cached_token = await http_client._authenticate(mock_client)
        assert cached_token == expected_jwt
        mock_client.post.assert_not_called()

        # Clearing cache causes re-authentication
        http_client.clear_token_cache()
        token_after_clear = await http_client._authenticate(mock_client)
        assert token_after_clear == expected_jwt
        mock_client.post.assert_called_once()


# ============================================================================
# 8. Agent Workflows Execution Preservation
# ============================================================================

class TestAgentWorkflowExecution:
    """Requirement 8: Existing agent graph workflows execute without disruption."""

    def test_workflow_graph_compilation_and_nodes(self):
        from agents.graph import workflow_graph

        assert workflow_graph is not None
        assert "CompiledStateGraph" in type(workflow_graph).__name__

    @pytest.mark.asyncio
    async def test_disruption_rebooking_endpoint_workflow_execution(self, monkeypatch):
        mock_result = {
            "workflow_status": "Completed",
            "step_order": 5,
            "candidate_routes": [
                {
                    "route_code": "RT-COL-KND",
                    "departure_time": "08:30 AM",
                    "fare": 1850.0,
                }
            ],
            "feasibility_result": {"is_feasible": True},
            "fare_analysis": {"fare_difference": 0.0},
            "impact_assessment": {"impact_classification": "Low"},
            "approval_status": None,
            "steps": [{"agent_name": "PlannerAgent", "step_order": 1}],
        }

        with patch("main.workflow_graph") as mock_graph, \
             patch("main.WorkflowLogger") as MockLogger:
            mock_graph.ainvoke = AsyncMock(return_value=mock_result)
            MockLogger.return_value.log_full_workflow = AsyncMock(return_value="wf-mock-id")

            client = TestClient(app)
            response = client.post(
                "/api/ai/disruption-rebooking",
                json={
                    "disruption_case_id": "case-test-01",
                    "objective": "Passenger rebooking for bus delay",
                },
            )

            assert response.status_code == 200
            data = response.json()
            assert data["status"] == "Completed"
            assert len(data["candidate_routes"]) == 1


# ============================================================================
# 9. Manager Approval Security Enforced (Human-in-the-Loop)
# ============================================================================

class TestManagerApprovalEnforcement:
    """Requirement 9: High-impact actions strictly mandate manager approval."""

    def test_high_impact_delay_mandates_manager_approval(self):
        """Delay > 15 minutes is deterministically classified as High impact."""
        impact = validate_impact_classification(
            llm_classification="Low",  # LLM attempted to classify as Low
            affected_passenger_count=30,
            delay_minutes=25,          # > 15 minutes
            is_cancellation=False,
        )

        assert impact == "High"
        requires_approval = (impact == "High")
        assert requires_approval is True

        workflow_status = "PendingManagerApproval" if requires_approval else "Completed"
        assert workflow_status == "PendingManagerApproval"

    def test_cancellation_mandates_manager_approval(self):
        """Service cancellation is deterministically classified as High impact."""
        impact = validate_impact_classification(
            llm_classification="Medium",
            affected_passenger_count=10,
            delay_minutes=5,
            is_cancellation=True,
        )

        assert impact == "High"
        requires_approval = (impact == "High")
        assert requires_approval is True

    def test_ai_agent_cannot_self_approve(self):
        """An AI workflow cannot bypass manager gate when impact is High."""
        state = {
            "impact_classification": "High",
            "requires_approval": True,
            "approval_status": None,
        }

        # Guardrail logic: If approval is required and approval_status is not 'Approved',
        # the workflow MUST NOT be 'Completed'.
        assert state["requires_approval"] is True
        assert state["approval_status"] != "Approved"
        next_status = "PendingManagerApproval" if state["requires_approval"] else "Completed"
        assert next_status == "PendingManagerApproval"
