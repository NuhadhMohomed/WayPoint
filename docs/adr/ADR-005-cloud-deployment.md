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
- **Full-Stack Presentation Tier Deployment**: In addition to backend services, the React Single Page Application is deployed on **Render** (`https://waypoint-web.onrender.com`), and a runnable Android APK is built and distributed for the Flutter mobile application (`REQ-DEP-03`, `REQ-DEP-04`).

---

## Decision
We select **Railway** for hosting the containerized ASP.NET Core Web API and Managed PostgreSQL Database, **Render** for hosting the React Web Application static site, and compiled **Android APK** distribution for the Flutter Mobile Application.

---

## Decision Options Evaluated

### Option A: Railway (API + PostgreSQL) & Render (React Web) [ACCEPTED]
- *Web API & DB*: ASP.NET Core API deployed as a Railway service using standard Docker containerization, connected to a Railway Managed PostgreSQL Database instance with high-speed private networking.
- *Web Frontend*: React 18 + Vite SPA deployed as a Render Static Site (`render.yaml`) with automated SPA routing rewrites.
- *Mobile*: Runnable Android APK compiled via Flutter SDK and hosted in release artifacts.
- *Pros*: Eliminates spin-down delays on critical database transactions; private internal network connectivity between API and PostgreSQL; Npgsql 9 Direct SSL and ALPN support; predictable evaluation availability through the 21 October 2026 deadline.
- *Cons*: Requires coordinating two zero-cost cloud dashboards (Railway and Render).

### Option B: Render (API + PostgreSQL)
- *Web API & DB*: ASP.NET Core API deployed as a Render Web Service connected to Render PostgreSQL.
- *Pros*: Simple single-dashboard GitHub auto-deploy.
- *Cons*: Render free web services spin down after 15 minutes of inactivity (~30s cold start), which could disrupt rapid evaluator testing during viva.

### Option C: Microsoft Azure for Students (App Service + Azure PostgreSQL)
- *Web API*: Deployed to Azure App Service (Linux .NET 8 runtime).
- *Database*: Azure Database for PostgreSQL Flexible Server.
- *Pros*: Enterprise cloud ecosystem, native Azure .NET tools.
- *Cons*: Consumes finite SLIIT student Azure credits ($100 cap); high risk of premature suspension before the 21 October 2026 evaluation period.

---

## Evaluation Comparison

| Evaluation Metric | Option A: Railway + Render [ACCEPTED] | Option B: Render All | Option C: Azure for Students |
| :--- | :--- | :--- | :--- |
| **Hosting Cost** | **Free / Starter Tier** | Free Tier | Finite Student Credit ($100 cap) |
| **Cold Start Latency** | **Zero / Low (Always Active)** | ~30s delay after 15m idle | Zero (Always Active) |
| **Setup Complexity** | **Low** (Docker + GitHub auto-deploy) | Low (GitHub auto-deploy) | Medium (Azure Portal / CLI) |
| **Evaluation Reliability** | **High** (Guaranteed through Oct 2026) | Medium (Cold start risks) | High Risk (Credit exhaustion) |
| **Database Encryption** | **Direct SSL / ALPN with Npgsql 9** | Standard SSL | Standard SSL |

---

## Consequences
- Unified backend and database deployment on Railway with high-performance Direct SSL connection pooling configured in `Program.cs` and `appsettings.json`.
- React Web App deployed with automated SPA redirect rules on Render, communicating via HTTPS with Railway API.
- Android APK compiled and available for evaluator testing on Android devices or emulators.
- Evaluators can inspect, test, and execute all endpoints directly through the interactive Swagger UI (`https://<railway-host>/swagger`) or via live web and mobile client interfaces.
