# ADR-003: Agentic AI Subsystem Framework & Orchestration

## Title
ADR-003: Selection of Agentic AI Framework and Multi-Agent Orchestration Architecture

## Status
`Accepted` (Option B: Python LangGraph / FastAPI Microservice in `ai/`)

---

## Context
SE3090 Assignment 1 mandates a **Level 4 Agentic AI Subsystem**. The system must:
- Solve a domain-relevant multi-step problem (disruption recovery, passenger rebooking).
- Coordinate at least **4 distinct specialized agents** (Planner/Coordinator, Journey Analysis, Resource & Booking, Validation & Safety Agents).
- Execute strictly via **10 allow-listed tools**.
- Persist workflow execution state, tool logs, timings, and validation outcomes in PostgreSQL.
- Enforce the human **Manager Approval Boundary** for high-impact operational changes.
- Integrate strictly through the **ASP.NET Core Web API** (clients must never call AI directly).

---

## Decision
We select **Option B: Python LangGraph & FastAPI Microservice in `ai/`** for the Level 4 Agentic AI Subsystem. The AI microservice communicates exclusively with the ASP.NET Core Web API via internal HTTP allow-listed tool endpoints, maintaining zero direct database connectivity.

---

## Decision Options Evaluated

### Option A: Microsoft Semantic Kernel (Native C# inside ASP.NET Core API)
- *Architecture*: Multi-agent orchestration implemented natively in C# within the ASP.NET Core project using Microsoft Semantic Kernel framework.
- *Pros*: Single unified codebase, zero separate microservice deployment overhead, direct C# DTO tool execution without inter-process HTTP calls, native EF Core DbContext access for state persistence.
- *Cons*: Less flexible prompt orchestration ecosystem compared to Python.

### Option B: Python LangGraph / FastAPI Microservice [ACCEPTED - SE3090 Lab Stack]
- *Architecture*: Python FastAPI microservice using LangGraph framework for multi-agent graph orchestration. Communicates internally with ASP.NET Core API via private REST endpoints.
- *Pros*: Directly uses the Python LangGraph stack practiced in SE3090 lab sessions; state-of-the-art graph state machine controls (`LangGraph`); rich prompt and agent evaluation tooling.
- *Cons*: Requires managing a secondary Python service in `ai/`; inter-service communication overhead.

---

## Reasons for Decision Comparison

| Evaluation Metric | Option A: C# Semantic Kernel | Option B: Python LangGraph Microservice |
| :--- | :--- | :--- |
| **Monorepo Complexity** | **Low** (Single C# solution) | **Medium** (C# Web API + Python service) |
| **Lab Alignment** | Low (New framework) | **High** (Matches SE3090 lab stack) |
| **Deployment Feasibility** | **High** (Single Docker container) | **Medium** (Multi-container setup) |
| **State Persistence** | **Native** (Direct EF Core DB writes) | **API Proxy** (Posts state back to C# API) |
| **Tool Integration** | **Direct C# Methods** | **REST HTTP Tool Callbacks** |

---

## Consequences

### Positive
- Team directly leverages lab experience with LangGraph agent state graphs, checkpoints, and multi-agent coordination.
- Clean separation between business domain backend (C#) and AI prompt engineering/evaluation graphs (Python).
- Rich ecosystem for agent evaluation metrics, tracing, and golden dataset testing.

### Negative
- Requires maintaining Python virtual environment and dependencies in `ai/`.
- Inter-service HTTP communication introduces serialization overhead during backend tool callbacks.

---

## Mandatory Guardrail Requirements (Applies to Either Choice)
1. **Zero Direct DB Access**: AI subsystem (C# or Python) CANNOT connect directly to PostgreSQL (`REQ-TECH-06`).
2. **10 Allow-Listed Tools**: Agents interact ONLY via the 10 approved backend tool wrappers (`BR-AITOOL-001`).
3. **Manager Approval Gate**: High-impact proposals MUST pause in `PendingManagerApproval` state (`BR-APPROVAL-001`).
4. **Safe Failure**: Validation failures or LLM timeouts MUST gracefully trigger `SafeFailure` fallback.
