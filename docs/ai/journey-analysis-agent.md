# Journey Analysis Agent — AI Architecture & Evaluation Report Section

**Student 1 / Sethum**  
**Component**: Component 1 — Journey Planning, Routes & Timetable Catalogue  
**Agent**: JourneyAnalysisAgent (Node 2 of 5)  

---

## 1. Agent Architecture

The **Journey Analysis Agent** is the second node in WayPoint's five-stage LangGraph multi-agent disruption recovery and journey planning workflow:

```text
Planner → [Journey Analysis] → Resource Feasibility → Booking & Policy → Validation & Safety
  (1)            (2)                    (3)                    (4)                 (5)
```

### Position in Workflow

| Property | Value |
|:---------|:------|
| **Node Position** | 2 of 5 |
| **Agent Name** | `JourneyAnalysisAgent` |
| **Input Context** | `objective`, `workflow_type`, `disruption_case_id` (from Planner Node) |
| **Output State** | `candidate_routes` (passed to Resource Feasibility Agent) |
| **Framework** | LangGraph StateGraph (`ai/agents/graph.py`, [ADR-003](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/docs/adr/ADR-003-ai-orchestration.md)) |
| **LLM Model** | Google Gemini 1.5 Flash via `langchain-google-genai` |
| **Safe Failure** | Wrapped with `execute_with_safe_failure` (`FR-AI-004`, `ai/guardrails/safe_failure.py`) |

### Core Pipeline (`_journey_core`)

The agent executes a deterministic 6-step lifecycle within [ai/agents/journey_agent.py](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/ai/agents/journey_agent.py):

1. **Input Sanitization**: Cleans and wraps user natural-language queries using `sanitize_user_input()` and `wrap_user_input()` to eliminate prompt injection risks (`FR-AI-008`).
2. **Context Aggregation & Prompting**: Constructs system messages with `JOURNEY_AGENT_PROMPT` and binds the student's allow-listed tool subset (`JOURNEY_AGENT_TOOLS`).
3. **Iterative Tool Execution Loop**: Executes LLM-requested tool calls up to `_MAX_TOOL_ROUNDS = 3`. Each tool execution measures execution latency (`duration_ms`), extracts structured JSON, and returns the output via `ToolMessage`.
4. **Structured JSON Extraction**: Employs robust extraction (`_extract_json_from_text`) handling markdown fences, bracket delimiters, and raw text objects.
5. **Deterministic Connecting Transfer Validation**: Programmatically validates connecting travel legs against `BR-TRANSFER-001` (minimum 20-minute transfer buffer).
6. **Telemetry & Audit State Generation**: Records execution duration, tool inputs/outputs, and rule validation outcomes in `AiWorkflowStep` and `AiToolCall` state records for PostgreSQL persistence ([ADR-004](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/docs/adr/ADR-004-ai-workflow-persistence.md)).

---

## 2. Tool Integration & Allow-List Enforcement

The Journey Analysis Agent binds **exactly 3 tools** from the mandatory 10 allow-listed tools (`BR-AITOOL-001`), completely isolating route and schedule analysis:

### Tool 1: SearchRoutes

| Property | Value |
|:---------|:------|
| **Registry Name** | `SearchRoutes` |
| **LangChain Tool** | `search_routes` ([ai/tools/journey_tools.py](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/ai/tools/journey_tools.py#L24-L85)) |
| **Backend Endpoint** | `GET /api/v1/routes?originCity={origin}&destinationCity={destination}` |
| **Input Schema** | `SearchRoutesInput(origin_city: str, destination_city: str, travel_date: Optional[str])` |
| **Output Schema** | `SearchRoutesOutput(routes: List[RouteInfo], total_count: int)` |
| **Business Rule** | Filters active routes and returns distances and city coordinates |

### Tool 2: GetBoardingPoints

| Property | Value |
|:---------|:------|
| **Registry Name** | `GetBoardingPoints` |
| **LangChain Tool** | `get_boarding_points` ([ai/tools/journey_tools.py](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/ai/tools/journey_tools.py#L88-L121)) |
| **Backend Endpoint** | `GET /api/v1/boarding-points?routeId={routeId}` |
| **Input Schema** | `GetBoardingPointsInput(route_id: str)` |
| **Output Schema** | `GetBoardingPointsOutput(boarding_points: List[BoardingPointInfo])` |
| **Business Rule** | Retrieves intermediate pickup stops, landmarks, and GPS coordinates |

### Tool 3: CheckTransferFeasibility

| Property | Value |
|:---------|:------|
| **Registry Name** | `CheckTransferFeasibility` |
| **LangChain Tool** | `check_transfer_feasibility` ([ai/tools/journey_tools.py](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/ai/tools/journey_tools.py#L124-L174)) |
| **Backend Endpoint** | Local deterministic calculation (`BR-TRANSFER-001`) |
| **Input Schema** | `CheckTransferFeasibilityInput(leg1_arrival_time, leg2_departure_time, transfer_stop_id)` |
| **Output Schema** | `CheckTransferFeasibilityOutput(is_feasible: bool, transfer_minutes: float, reason: str)` |
| **Business Rule** | Enforces minimum 20-minute transfer window between connecting bus legs |

---

## 3. Tool Execution & Audit Recording (ADR-004)

Every tool invocation dispatched by `JourneyAnalysisAgent` generates structured execution telemetry:

```python
record = {
    "tool_name": tool_name,
    "arguments_json": json.dumps(tool_args),
    "result_json": result_str if isinstance(result_str, str) else json.dumps(result_str),
    "duration_ms": duration_ms,
}
```

This telemetry is mapped directly to `AiToolCall` database records in PostgreSQL via `workflow_persistence.py`, enabling transparent auditability via the **AI Multi-Agent Observability API (`GET /api/v1/ai/workflows/{id}`)** and the React Admin AI Observability Dashboard (`AiObservabilityPage.jsx`).

---

## 4. Deterministic Guardrails & Output Validation (FR-AI-003)

To prevent LLM hallucination and enforce rigid transit safety constraints:

1. **`AllowListedToolBoundary`**:
   Verifies that every tool call executed strictly belongs to `{SearchRoutes, GetBoardingPoints, CheckTransferFeasibility}`. Any attempt to access unauthorized tools triggers an immediate `ToolNotAllowedError`.

2. **`ZeroDirectDbAccess`**:
   Ensures the agent makes zero direct SQL queries to PostgreSQL. All route queries route through the authoritative ASP.NET Core REST API over HTTP.

3. **`TransferWindowEnforcement` (`BR-TRANSFER-001`)**:
   For any connecting route candidate with two or more legs:
   $$\Delta t = \text{departure\_time}(\text{leg}_2) - \text{arrival\_time}(\text{leg}_1)$$
   $$\text{passed} = \Delta t \ge 20.0 \text{ minutes}$$
   If $\Delta t < 20$ minutes, the transfer is marked infeasible (`passed = False`), and the route candidate is disqualified or flagged to prevent missed connections.

---

## 5. Safe Failure & Error Recovery (FR-AI-004)

The agent node is wrapped by `execute_with_safe_failure()`:

```python
async def journey_agent_node(state: WorkflowState) -> dict:
    return await execute_with_safe_failure(
        agent_name="JourneyAnalysisAgent",
        core_fn=_journey_core,
        state=state,
    )
```

- **LLM Rate-Limit / Quota Exhaustion**: In case of Gemini API timeouts or network outages, the agent executes safe failure fallback logic, returning an empty candidate set with descriptive error diagnostics rather than crashing the multi-agent pipeline.
- **Backend API Unavailability**: If ASP.NET Core route endpoints return 500 or timeout, the error is captured in `result_json`, logged to `AiToolCall`, and the workflow proceeds to safe degradation.

---

## 6. Verification & Automated Testing

Student 1's Journey Analysis Agent is covered by unit tests, mock tool verification, and golden query benchmarks:

```bash
# Run AI subsystem tests focusing on journey agent
cd ai
pytest tests/test_journey_agent.py -v
pytest tests/test_agents.py -k "journey" -v
```

### Key Verified Scenarios

1. **Direct Route Discovery**: Natural language query *"direct buses from Colombo to Kandy"* resolves to direct candidate routes with exact timing.
2. **Connecting Transfer Feasibility (`BR-TRANSFER-001`)**:
   - Leg 1 arrives at `10:00`, Leg 2 departs at `10:25` $\rightarrow$ 25 min transfer $\rightarrow$ **Feasible (Passed)**.
   - Leg 1 arrives at `10:00`, Leg 2 departs at `10:15` $\rightarrow$ 15 min transfer $\rightarrow$ **Infeasible (Rejected)**.
3. **Tool Allow-List Isolation**: Verification that attempts to execute booking or disruption tools from this node raise an exception.
4. **PostgreSQL Audit Persistence**: Confirmed persistence of `AiWorkflowStep` and `AiToolCall` records linked to the parent workflow ID.
