# WayPoint AI (Agentic AI Subsystem)

The autonomous intelligence and optimization engine for **WayPoint** (**SE3090 Assignment 1**). Built to deliver deterministic, auditable, and safe AI workflows for multimodal journey planning and proactive disruption mitigation.

---

## 1. Architectural Guardrails (`AGENTS.md`)

To ensure evaluation compliance and prevent system instability, the AI module strictly adheres to the core rules defined in `AGENTS.md`:

1. **No Direct Database Access**: AI workflows must **never** connect directly to PostgreSQL. All reads and writes must pass through the authoritative ASP.NET Core API via authenticated tool callbacks.
2. **Strict Tool Allow-Listing**: The agent only has access to explicit, allow-listed backend tool contracts.
3. **Structured Outputs**: All recommendations must adhere to typed JSON schemas (Pydantic models).
4. **Deterministic Backend Validation**: Every AI recommendation must be independently validated server-side by backend business rules before any operational action occurs.
5. **Manager Approval Gate**: High-impact operational actions (e.g. re-routing a service, bulk passenger rebooking, issuing mass refunds) require explicit manager sign-off via the approval workflow.
6. **No Direct Financial Operations**: AI cannot directly authorize payments or confirm transactions.
7. **Safe Failure**: If the AI model fails or experiences timeouts, the system safely falls back to standard deterministic transit routing without blocking passengers.

---

## 2. Directory Structure

```text
ai/
├── agents/                      # LangGraph / LangChain agent workflow definitions
│   ├── journey_agent.py         # Multi-criteria journey recommendation agent (Student 1)
│   └── disruption_agent.py      # Disruption mitigation & rebooking engine (Student 4)
├── tools/                       # Client HTTP wrappers for ASP.NET Core API tool endpoints
├── schemas/                     # Pydantic structured output models
├── prompts/                     # Version-controlled prompt templates & system instructions
├── tests/                       # Unit tests & evaluation benchmark scripts
├── requirements.txt             # Python dependencies
└── main.py                      # FastAPI microservice entry point
```

---

## 3. Environment Setup

Ensure Python 3.10+ is installed:

```bash
cd ai
python -m venv venv
# On Windows PowerShell:
.\venv\Scripts\Activate.ps1

pip install -r requirements.txt
```

### Configuration
Set the following variables in root `.env`:
```env
AI_SERVICE_URL=http://localhost:8000
API_BASE_URL=http://localhost:5010/api/v1
OPENAI_API_KEY=your_openai_or_gemini_api_key
```

### Running the Microservice
```bash
uvicorn main:app --reload --port 8000
```
