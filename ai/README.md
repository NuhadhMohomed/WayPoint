# WayPoint AI Subsystem — Multi-Agent Disruption Mitigation & Journey Optimization

> **Component Owners**: Sethum (Student 1), Nuhadh (Student 2), Mithila (Student 3), Dineth (Student 4)  
> **Architecture Decisions**: [ADR-003 — AI Orchestration](../docs/adr/ADR-003-ai-orchestration.md) | [ADR-004 — Workflow Persistence](../docs/adr/ADR-004-ai-workflow-persistence.md)  
> **Frameworks**: Python 3.11, LangGraph, LangChain, Google Gemini 1.5 Flash, FastAPI, httpx  

---

## 1. Overview

The **WayPoint AI Subsystem** is an autonomous multi-agent microservice engineered to resolve transit disruptions, optimize multi-leg journey planning, assess fleet resource feasibility, calculate tiered refund policies, and enforce safety guardrails across Sri Lankan intercity bus networks.

The subsystem operates strictly as an untrusted advisory layer adhering to the core agentic AI rules:
1. **Zero Direct Database Access**: Agents communicate exclusively with the authoritative ASP.NET Core API via HTTP tool calls.
2. **Allow-Listed Tools Only (`BR-AITOOL-001`)**: Agents bind strictly to an approved set of 10 tools.
3. **Deterministic Output Validation (`FR-AI-003`)**: All AI reasoning is verified by mathematical and business rule validators before any action is proposed.
4. **Safe Failure Degradation (`FR-AI-004`)**: Unhandled exceptions or LLM service outages trigger safe recovery pathways.
5. **Human-in-the-Loop Approval (`BR-APPROVAL-001`)**: High-impact operational schedule changes require explicit Transport Manager sign-off.
6. **Execution Telemetry Persistence ([ADR-004](../docs/adr/ADR-004-ai-workflow-persistence.md))**: Every agent step, tool invocation latency, and validation result is persisted to PostgreSQL for full observability.

---

## 2. Multi-Agent Workflow Architecture

WayPoint organizes its agents into a 5-stage sequential LangGraph StateGraph (`agents/graph.py`):

```text
┌─────────────┐     ┌──────────────────────┐     ┌────────────────────────┐
│   Planner   │ ──> │   Journey Analysis   │ ──> │  Resource Feasibility  │
│   (Node 1)  │     │   (Node 2 / Sethum)  │     │   (Node 3 / Nuhadh)    │
└─────────────┘     └──────────────────────┘     └────────────────────────┘
                                                              │
                                                              ▼
┌─────────────────────────┐     ┌────────────────────────┐    │
│   Validation & Safety   │ <── │    Booking & Policy    │ <──┘
│    (Node 5 / Dineth)    │     │   (Node 4 / Mithila)   │
└───────────┬─────────────┘     └────────────────────────┘
            │
            ▼
┌─────────────────────────────────┐
│ State Recorded to PostgreSQL    │
│ AiWorkflow, Steps & ToolCalls   │
└─────────────────────────────────┘
```

### Agent Roles & Student Ownership

| Node | Agent Name | Student Owner | Core Responsibilities & Domain Invariants |
| :--- | :--- | :--- | :--- |
| **Node 1** | `Planner` | Orchestrator | Decomposes travel requests and disruption incidents into workflow objectives |
| **Node 2** | `JourneyAnalysisAgent` | **Sethum** (Student 1) | Discovers candidate routes and enforces connecting transfer buffers (`BR-TRANSFER-001` $\ge 20$ min) |
| **Node 3** | `ResourceFeasibilityAgent` | **Nuhadh** (Student 2) | Evaluates bus seating capacity, driver rest compliance ($>8$ hrs), and replacement vehicle feasibility |
| **Node 4** | `BookingPolicyAgent` | **Mithila** (Student 3) | Calculates fare deltas and applies tiered Sri Lankan cancellation refund policies (`BR-REFUND-001`) |
| **Node 5** | `ValidationSafetyAgent` | **Dineth** (Student 4) | Validates blast-radius impact, gates high-impact changes with Manager approval (`BR-APPROVAL-001`), and notifies conductors |

---

## 3. The 10 Mandatory Allow-Listed Tools (`BR-AITOOL-001`)

The subsystem exposes exactly 10 allow-listed tools in `tools/registry.py`:

| Tool Name | Assigned Agent / Student | Backend Endpoint / Logic | Schema Definition |
| :--- | :--- | :--- | :--- |
| **`SearchRoutes`** | Journey Agent (Sethum) | `GET /api/v1/routes` | `SearchRoutesInput / Output` |
| **`GetBoardingPoints`** | Journey Agent (Sethum) | `GET /api/v1/boarding-points` | `GetBoardingPointsInput / Output` |
| **`CheckTransferFeasibility`** | Journey Agent (Sethum) | Local datetime calculation ($\ge 20$ min) | `CheckTransferFeasibilityInput / Output` |
| **`CheckSeatAvailability`** | Resource Agent (Nuhadh) | `GET /api/v1/services/{id}/seats` | `CheckSeatAvailabilityInput / Output` |
| **`CalculateFareDifference`** | Policy Agent (Mithila) | `GET /api/v1/services/{id}` | `CalculateFareDifferenceInput / Output` |
| **`SendPassengerNotification`**| Policy Agent (Mithila) | `POST /api/v1/notifications` | `SendPassengerNotificationInput / Output` |
| **`CreateRebookingProposal`** | Safety Agent (Dineth) | `POST /api/v1/rebooking-proposals` | `CreateRebookingProposalInput / Output` |
| **`CalculatePassengerImpact`** | Safety Agent (Dineth) | `GET /api/v1/services/{id}/manifest` | `CalculatePassengerImpactInput / Output` |
| **`RequestManagerApproval`** | Safety Agent (Dineth) | `POST /api/v1/approvals` | `RequestManagerApprovalInput / Output` |
| **`ApplyApprovedOperationalChange`** | Safety Agent (Dineth) | `POST /api/v1/approvals/{id}/apply` | `ApplyApprovedChangeInput / Output` |

---

## 4. Deterministic Guardrails & Output Validation (`FR-AI-003`)

To guarantee safety and prevent LLM hallucination, output validator functions inspect the LLM output:

1. **`validate_bus_capacity`**: Rejects proposals where passengers assigned exceed bus capacity.
2. **`validate_driver_rest`**: Rejects driver assignments violating mandatory 8-hour intercity rest periods.
3. **`validate_fare_difference_arithmetic`**: Mathematically verifies that $\text{fare\_diff} = \text{new\_fare} - \text{old\_fare}$.
4. **`validate_cancellation_refund_schedule`**: Enforces Sri Lankan transit refund rules ($>24\text{h} \rightarrow 90\%$, $12\text{--}24\text{h} \rightarrow 50\%$, $<12\text{h} \rightarrow 0\%$).
5. **`validate_high_impact_approval_gate`**: Requires explicit Transport Manager approval for any timetable shift $>15$ minutes or service cancellation.

---

## 5. Directory Structure

```text
ai/
├── agents/                   # Multi-agent LangGraph node implementations
│   ├── planner.py            # Initial workflow decomposition
│   ├── journey_agent.py      # Student 1: Route discovery & transfer validation
│   ├── resource_agent.py     # Student 2: Fleet capacity & driver rest evaluation
│   ├── booking_agent.py      # Student 3: Fare difference & refund policies
│   ├── safety_agent.py       # Student 4: High-impact gate & manager approval
│   ├── graph.py              # LangGraph StateGraph connecting all 5 agents
│   └── state.py              # WorkflowState TypedDict schema
│
├── tools/                    # Tool definitions and allow-list registry
│   ├── registry.py           # BR-AITOOL-001 central registry enforcing 10 tools
│   ├── journey_tools.py      # Student 1 tool implementations
│   ├── resource_tools.py     # Student 2 tool implementations
│   ├── booking_tools.py      # Student 3 tool implementations
│   ├── disruption_tools.py   # Student 4 tool implementations
│   ├── http_client.py        # Authenticated HTTP client for backend REST API
│   └── workflow_persistence.py # PostgreSQL audit recorder via backend API
│
├── guardrails/               # Safety filters and deterministic rules
│   ├── input_sanitizer.py    # Prompt injection prevention (FR-AI-008)
│   ├── output_validator.py   # Mathematical and policy validation rules
│   └── safe_failure.py       # Safe-failure wrapper logic (FR-AI-004)
│
├── schemas/                  # Pydantic schemas for structured inputs/outputs
│   ├── tools.py              # Tool request and response models
│   └── state.py              # Workflow state models
│
├── prompts/                  # System prompts with role boundaries
│   └── agent_prompts.py      # Prompts for Planner, Journey, Resource, Policy, Safety
│
├── tests/                    # Pytest automated test suites
│   ├── test_agents.py        # Multi-agent pipeline integration tests
│   ├── test_guardrails.py    # Guardrail and deterministic rule validation tests
│   ├── test_tools.py         # Tool registry and allow-list enforcement tests
│   └── test_safe_failure.py  # Safe failure recovery tests
│
├── main.py                   # FastAPI application entry point
├── config.py                 # Configuration and environment variable loader
└── requirements.txt          # Python dependencies
```

---

## 6. Setup & Execution

### 6.1 Prerequisites
- Python 3.11+
- ASP.NET Core backend running on `http://localhost:5010` (or Railway cloud URL)
- Valid Google Gemini API Key

### 6.2 Environment Configuration
Create or configure `.env` in the repository root or `ai/` folder:
```env
API_BASE_URL=http://localhost:5010/api/v1
GEMINI_API_KEY=your_gemini_api_key_here
LLM_MODEL=gemini-1.5-flash
LLM_TEMPERATURE=0.2
PORT=8000
```

### 6.3 Installation & Startup
```bash
cd ai
python -m venv venv

# Windows
.\venv\Scripts\Activate.ps1
# Linux/macOS
source venv/bin/activate

pip install -r requirements.txt
python main.py
```
- **FastAPI Service**: `http://localhost:8000`
- **Swagger Docs**: `http://localhost:8000/docs`

---

## 7. Running Automated Tests

```bash
cd ai
# Run all multi-agent tests
pytest tests/ -v

# Run specific agent or guardrail tests
pytest tests/test_agents.py -v
pytest tests/test_guardrails.py -v
pytest tests/test_safe_failure.py -v
```

All agent tests validate complete LangGraph execution, tool invocation boundaries, and deterministic output verification.
