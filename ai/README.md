# WayPoint AI Subsystem (FastAPI / LangGraph / Gemini)

> **Status: Active Development (`v0.1.0` / WIP)**  
> The WayPoint Agentic AI microservice is actively developing. The 5-node LangGraph workflow, 10 allow-listed HTTP tools, guardrails, safe-failure handlers, and backend workflow logging are implemented, while prompt calibration, live end-to-end tool resilience, and evaluation coverage continue to evolve.

---

## 1. Overview

The AI subsystem provides advisory multi-agent reasoning for two primary workflows exposed in `main.py`:
- `GET /api/ai/health` — Service health & timeout configuration check
- `POST /api/ai/disruption-rebooking` — Runs `planner → journey → resource → booking → safety`
- `POST /api/ai/journey-recommendation` — Runs `planner → journey → resource → booking` (skips `safety`)

### Core AI Rules (`AGENTS.md`)
1. **No Direct Database Access**: Communicates with the system exclusively via HTTP calls to the ASP.NET Core Web API (`tools/http_client.py`).
2. **Allow-Listed Tools Only (`BR-AITOOL-001`)**: Only the 10 tools registered in `tools/registry.py` are permitted (`ToolNotAllowedError` otherwise).
3. **Structured Schemas & Deterministic Guardrails**: Pydantic schemas (`schemas/`) and deterministic validators (`guardrails/output_validator.py`) verify capacity, driver rest, fare arithmetic, refunds, and approval gates.
4. **Human Manager Gate**: Disruption workflows transition to `PendingManagerApproval` (`BR-APPROVAL-001`) rather than auto-mutating schedules or confirming payments.
5. **Safe Failure**: Timeouts, invalid inputs (`guardrails/input_sanitizer.py`), or LLM/API errors degrade via `guardrails/safe_failure.py`.

---

## 2. Workflow Graph & Agent Nodes (`agents/graph.py`)

```text
START ──▶ planner ──▶ journey ──▶ resource ──▶ booking ──┬── (journey_recommendation) ──▶ END
                                                         │
                                                         └── (disruption_rebooking) ──▶ safety ──▶ END
```

| Node | File | Student Owner | Responsibility |
| :--- | :--- | :--- | :--- |
| `planner` | `agents/planner.py` | Shared Orchestrator | Decomposes inbound objective into workflow state |
| `journey` | `agents/journey_agent.py` | **Sethum** (Student 1) | Route discovery & transfer window checks (`>= 20` min) |
| `resource` | `agents/resource_agent.py` | **Nuhadh** (Student 2) | Seat availability & driver/bus resource feasibility |
| `booking` | `agents/booking_agent.py` | **Mithila** (Student 3) | Fare delta & cancellation refund policy analysis |
| `safety` | `agents/safety_agent.py` | **Dineth** (Student 4) | Impact assessment, rebooking proposals & manager approval gate |

---

## 3. Allow-Listed Tools (`tools/registry.py`)

1. `SearchRoutes` (`tools/journey_tools.py`)
2. `GetBoardingPoints` (`tools/journey_tools.py`)
3. `CheckTransferFeasibility` (`tools/journey_tools.py`)
4. `CheckSeatAvailability` (`tools/resource_tools.py`)
5. `CalculateFareDifference` (`tools/booking_tools.py`)
6. `SendPassengerNotification` (`tools/booking_tools.py`)
7. `CreateRebookingProposal` (`tools/disruption_tools.py`)
8. `CalculatePassengerImpact` (`tools/disruption_tools.py`)
9. `RequestManagerApproval` (`tools/disruption_tools.py`)
10. `ApplyApprovedOperationalChange` (`tools/disruption_tools.py`)

*(Additional helper tool modules such as `tools/api_tools.py` support API integration patterns during development.)*

---

## 4. Directory Structure

```text
ai/
├── agents/                 # graph.py, state.py & 5 agent nodes
├── tools/                  # registry.py, http_client.py & domain tool modules
├── guardrails/             # input_sanitizer.py, output_validator.py, safe_failure.py
├── persistence/            # workflow_logger.py (ADR-004 backend trace persistence)
├── prompts/                # agent_prompts.py, planner_prompt.py, journey_system_prompt.txt
├── schemas/                # journey_schemas.py, tools.py, workflow.py
├── tests/                  # Pytest unit, golden, guardrail, registry & persistence tests
├── config.py               # Environment config loader (ai/.env then root .env)
├── main.py                 # FastAPI entry point (port 8000)
├── Dockerfile              # Container build (Python 3.11-slim + uvicorn)
├── .env.example            # AI-specific environment template
└── requirements.txt        # Python dependencies (LangGraph, LangChain, FastAPI, httpx)
```

---

## 5. Setup & Running Locally

### Environment Variables (`config.py`)
Copy `ai/.env.example` to `ai/.env` (or configure in root `.env`):
```env
API_BASE_URL=http://localhost:5010/api/v1
GEMINI_API_KEY=your_gemini_api_key_here
LLM_MODEL=gemini-3.5-flash
LLM_TEMPERATURE=0.1
AI_SERVICE_EMAIL=admin@waypoint.lk
AI_SERVICE_PASSWORD=Password123!
BACKEND_ORIGIN=http://localhost:5010
```

### Startup
```bash
cd ai
python -m venv venv
# Windows: .\venv\Scripts\Activate.ps1
# Linux/macOS: source venv/bin/activate
pip install -r requirements.txt
python main.py
```
- **Service URL**: `http://localhost:8000`
- **Swagger / OpenAPI**: `http://localhost:8000/docs`

---

## 6. Automated Tests

```bash
cd ai
pytest tests/ -v
```

Includes tests for agents, conditional graph routing, tool allow-list registry, input sanitization, safe-failure fallbacks, golden scenarios, and workflow persistence integration.

---

## 7. In Progress / Upcoming Work

- [ ] Harmonize default `AI_SERVICE_EMAIL` / `AI_SERVICE_PASSWORD` across `ai/.env.example`, `ai/config.py`, and `DbSeeder.cs`.
- [ ] Expand live end-to-end LLM + backend tool integration benchmarks without mocked responses.
- [ ] Refine prompt templates and structured output recovery for edge-case multi-leg queries.
