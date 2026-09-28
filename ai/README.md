# WayPoint AI Subsystem

> **Component Owner**: Student 1 (Sethum) — Journey Planning & Route Catalogue  
> **Architecture Decision**: [ADR-003 — AI Orchestration](../docs/adr/ADR-003-ai-orchestration.md) | [ADR-004 — Workflow Persistence](../docs/adr/ADR-004-ai-workflow-persistence.md)

## Overview

Python LangGraph + FastAPI microservice implementing the **Journey Analysis Agent** — one of four specialised AI agents in the WayPoint agentic AI subsystem.

The Journey Analysis Agent decomposes complex natural language travel queries (e.g., *"fastest route to Ella with AC"*) into viable, ranked journey candidates by calling the ASP.NET Core backend via **allow-listed tools only**.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    FastAPI (port 8000)                       │
│  POST /api/journey/analyze                                  │
│  GET  /health                                               │
├─────────────────────────────────────────────────────────────┤
│               LangGraph State Graph (9 nodes)               │
│  parse_query → search_routes → search_services              │
│  → get_boarding_info → evaluate_direct → evaluate_connecting│
│  → rank_candidates → validate_output → persist_workflow     │
├─────────────────────────────────────────────────────────────┤
│            Allow-Listed Backend Tools (httpx)                │
│  SearchRoutes | SearchServices | GetBoardingPoints           │
│  GetTimetable | CheckTransferFeasibility                     │
└──────────────────────┬──────────────────────────────────────┘
                       │ HTTP (REST)
                       ▼
┌─────────────────────────────────────────────────────────────┐
│              ASP.NET Core Web API (port 5010)               │
│         Authoritative Business Logic & PostgreSQL            │
└─────────────────────────────────────────────────────────────┘
```

## Directory Structure

```
ai/
├── agents/
│   └── journey_agent.py          # LangGraph journey analysis agent (9-node state graph)
├── tools/
│   ├── api_tools.py              # Allow-listed HTTP tool wrappers (5 tools)
│   └── workflow_persistence.py   # AI workflow state persistence client (ADR-004)
├── schemas/
│   └── journey_schemas.py        # Pydantic structured output models
├── prompts/
│   └── journey_system_prompt.txt # System prompt with guardrails
├── tests/
│   └── test_journey_agent.py     # Unit tests & evaluation benchmarks
├── requirements.txt              # Python dependencies
├── main.py                       # FastAPI entry point
└── README.md                     # This file
```

## Setup & Run

### Prerequisites

- Python 3.11+
- ASP.NET Core backend running on `http://localhost:5010`
- PostgreSQL database (used by the backend, NOT accessed directly by AI)

### Environment Variables

Copy the root `.env.example` or set these variables:

```bash
API_BASE_URL=http://localhost:5010/api/v1
OPENAI_API_KEY=your_openai_or_gemini_api_key
OPENAI_MODEL=gpt-4o-mini    # or any compatible model
```

### Install & Run

```bash
cd ai
python -m venv venv

# Windows
.\venv\Scripts\Activate.ps1

# Linux/macOS
source venv/bin/activate

pip install -r requirements.txt
uvicorn main:app --reload --port 8000
```

### Run Tests

```bash
cd ai
python -m pytest tests/ -v
```

## API Endpoint

### `POST /api/journey/analyze`

Accepts a structured journey query and returns ranked recommendations.

**Request:**
```json
{
  "query": "fastest bus to Ella with AC",
  "origin_city": "Colombo",
  "destination_city": "Ella",
  "travel_date": "2026-10-01",
  "passenger_count": 2,
  "preferences": {
    "direct_only": false,
    "require_ac": true,
    "max_fare": null,
    "arrive_before": "15:00"
  }
}
```

**Response:**
```json
{
  "workflow_id": "uuid",
  "status": "Completed",
  "recommendations": [
    {
      "service_id": "uuid",
      "service_code": "SRV-COL-ELLA-0800",
      "route_number": "EX-08",
      "origin": "Colombo",
      "destination": "Ella",
      "departure_time": "2026-10-01T08:00:00",
      "arrival_time": "2026-10-01T14:30:00",
      "total_fare": 2500.00,
      "duration_minutes": 390,
      "is_connecting": false,
      "match_score": 0.95,
      "available_seats": 12,
      "bus_class": "SemiLuxury"
    }
  ],
  "agent_reasoning": "Analysed 3 journey candidate(s)...",
  "tool_calls_summary": [...],
  "validation_summary": [...]
}
```

## Guardrails & Safety

| Rule | Enforcement |
|------|-------------|
| No direct DB access | Tools use `httpx` → ASP.NET API only |
| Allow-listed tools only | 5 tools hardcoded in `api_tools.py` |
| Structured output | Pydantic schemas validate every response |
| Transfer window ≥ 20 min | `CheckTransferFeasibility` enforces BR-TRANSFER-001 |
| Safe failure | Max 3 retries, then `SafeFailure` status (BR-AIVAL-002) |
| LLM timeout | 10-second cap on LLM calls |
| Workflow persistence | Every tool call and validation logged to PostgreSQL |

## Merging with Other Components

This AI subsystem is designed to be **independently deployable** and merge-safe:

- **Backend integration**: The `AiWorkflowController.cs` in `backend/WayPoint.API/Controllers/` handles workflow persistence. It uses the existing `IWayPointDbContext` and domain entities — no schema changes required.
- **Student 2 (Nuhadh)**: Fleet & seat tools can be added to `tools/api_tools.py` as new functions.
- **Student 3 (Mithila)**: Booking tools can extend the tool set similarly.
- **Student 4 (Dineth)**: Disruption agent can be added as a new agent in `agents/disruption_agent.py` using the same graph pattern.
- **Shared infrastructure**: `schemas/`, `tools/workflow_persistence.py`, and `main.py` are designed for extension.
