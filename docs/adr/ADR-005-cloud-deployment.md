# ADR-005: Cloud Infrastructure & Deployment Platform

## Title
ADR-005: Selection of Cloud Hosting Infrastructure and Free-Tier Deployment Platform Strategy

## Status
`Accepted` (Confirmed: Railway for containerized ASP.NET Core Web API & Managed PostgreSQL 18 database with Direct SSL & ALPN routing)

---

## Context
SE3090 Assignment 1 requires deploying the authoritative WayPoint platform to a dependable cloud environment:
- **ASP.NET Core Web API**: Deployed with a live `/health` endpoint and interactive Swagger documentation URL (`/swagger`).
- **PostgreSQL Database**: Deployed securely with applied EF Core migrations and direct SSL/ALPN connectivity.
- **Agentic AI Subsystem**: Deployed alongside backend services or connected via private internal HTTP.
- **Access Window**: All live URLs must remain accessible to evaluators until at least **Wednesday, 21 October 2026** (`REQ-ASSIGN-06`).
- **Cost Policy**: Must strictly use no-cost or free-tier cloud services (`REQ-DEP-06`).
- **Headless Architecture Transition**: Per [ADR-006](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/docs/adr/ADR-006-headless-architecture.md), all frontend UI rendering layers have been decommissioned, eliminating static web CDN and mobile app compilation from cloud infrastructure requirements.

---

## Decision
We select **Railway** for hosting the containerized ASP.NET Core Web API and Managed PostgreSQL Database with high-speed private networking and direct SSL encryption.

---

## Decision Options Evaluated

### Option A: Railway (API + PostgreSQL) [ACCEPTED]
- *Web API & DB*: ASP.NET Core API deployed as a Railway service using standard Docker containerization, connected to a Railway Managed PostgreSQL Database instance with high-speed private networking.
- *Pros*: Eliminates Render's 15-minute inactivity spin-down delays; native Dockerfile support; private internal network connectivity between API and PostgreSQL; Npgsql 9 Direct SSL and ALPN support; predictable evaluation availability through the 21 October 2026 deadline.
- *Cons*: Requires initial setup of Railway project and environment variables.

### Option B: Render (API + PostgreSQL)
- *Web API & DB*: ASP.NET Core API deployed as a Render Web Service connected to Render PostgreSQL.
- *Pros*: Simple GitHub auto-deploy.
- *Cons*: Render free web services spin down after 15 minutes of inactivity (~30s cold start), which could disrupt rapid evaluator testing during viva.

### Option C: Microsoft Azure for Students (App Service + Azure PostgreSQL)
- *Web API*: Deployed to Azure App Service (Linux .NET 8 runtime).
- *Database*: Azure Database for PostgreSQL Flexible Server.
- *Pros*: Enterprise cloud ecosystem, native Azure .NET tools.
- *Cons*: Consumes finite SLIIT student Azure credits ($100 cap); high risk of premature suspension before the 21 October 2026 evaluation period.

---

## Evaluation Comparison

| Evaluation Metric | Option A: Railway [ACCEPTED] | Option B: Render | Option C: Azure for Students |
| :--- | :--- | :--- | :--- |
| **Hosting Cost** | **Free / Starter Tier** | Free Tier | Finite Student Credit ($100 cap) |
| **Cold Start Latency** | **Zero / Low (Always Active)** | ~30s delay after 15m idle | Zero (Always Active) |
| **Setup Complexity** | **Low** (Docker + GitHub auto-deploy) | Low (GitHub auto-deploy) | Medium (Azure Portal / CLI) |
| **Evaluation Reliability** | **High** (Guaranteed through Oct 2026) | Medium (Cold start risks) | High Risk (Credit exhaustion) |
| **Database Encryption** | **Direct SSL / ALPN with Npgsql 9** | Standard SSL | Standard SSL |

---

## Consequences
- Single cloud platform (Railway) manages both database and API services under a unified project namespace.
- High-performance Direct SSL connection pooling configured in `Program.cs` and `appsettings.json`.
- Evaluators can inspect, test, and execute all endpoints directly through the interactive Swagger UI (`https://<railway-host>/swagger`).
