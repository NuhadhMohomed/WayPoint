# ADR-005: Cloud Infrastructure & Deployment Platform

## Title
ADR-005: Selection of Cloud Hosting Infrastructure and Free-Tier Deployment Platform Strategy

## Status
`DECISION REQUIRED` (Team must confirm choice between Option A: Render + Vercel vs. Option B: Microsoft Azure for Students)

---

## Context
SE3090 Assignment 1 requires deploying the complete WayPoint platform:
- **ASP.NET Core Web API**: Deployed with a live `/health` endpoint and Swagger URL.
- **PostgreSQL Database**: Deployed securely with applied EF Core migrations.
- **React Web App**: Deployed as a working live web application targeting the cloud API.
- **Flutter Mobile App**: Delivered as a compiled, runnable Android APK (`.apk`).
- **Agentic AI Subsystem**: Deployed to cloud or documented with clear startup order.
- **Access Window**: All live URLs must remain accessible to evaluators until at least **Wednesday, 21 October 2026** (`REQ-ASSIGN-06`).
- **Cost Policy**: Must strictly use no-cost or free-tier cloud services (`REQ-DEP-06`).

---

## Decision Options Under Team Consideration

### Option A: Render (API + PostgreSQL) + Vercel (React Web) [RECOMMENDED FREE STACK]
- *Web API & DB*: ASP.NET Core API deployed as a Render Web Service (Docker container) connected to a Render Managed PostgreSQL Database.
- *React Web*: Deployed to Vercel / Netlify static hosting.
- *Flutter*: Android `.apk` compiled locally and uploaded with submission package.
- *Pros*: 100% free tier available, automatic Git integration on push to `main`, simple environment variable configuration.
- *Cons*: Render free web services spin down after 15 minutes of inactivity (30s cold start).

### Option B: Microsoft Azure for Students (App Service + Azure PostgreSQL)
- *Web API*: Deployed to Azure App Service (Linux .NET 8 runtime).
- *Database*: Azure Database for PostgreSQL Flexible Server (Free student credit).
- *React Web*: Azure Static Web Apps.
- *Pros*: Enterprise cloud platform, native Azure .NET integration, zero cold-start delays if app service kept warm.
- *Cons*: Consumes finite SLIIT student Azure credits; requires managing subscription caps.

---

## Evaluation Comparison

| Criteria | Option A: Render + Vercel | Option B: Azure for Students |
| :--- | :--- | :--- |
| **Cost** | **100% Free** (No credit limits) | Free Student Credit ($100 cap) |
| **Setup Complexity** | **Very Low** (GitHub auto-deploy) | Medium (Azure Portal / CLI) |
| **Cold Start Behavior** | ~30s on first request | Instantly active |
| **Evaluation Reliability**| High (accessible through Oct 2026) | Risk of credit exhaustion |
| **Docker Support** | Native Dockerfile build | Native App Service build |

---

## Consequences

### If Option A (Render + Vercel) Selected
- Zero risk of running out of cloud credits before the 21 October 2026 evaluation deadline.
- Cold start delay on initial evaluator click can be mitigated by pinging the `/health` endpoint prior to demonstration.

### If Option B (Azure) Selected
- Instant API response during live demonstration.
- Requires monitoring Azure credit usage carefully.

---

## Deployment Checklist & Verification
1. Verify `GET https://<api-host>/health` returns `200 OK`.
2. Verify `GET https://<api-host>/swagger` renders live OpenAPI UI.
3. Verify React app live URL interacts cleanly with cloud API.
4. Verify Android APK installs and runs on physical/emulator device.
