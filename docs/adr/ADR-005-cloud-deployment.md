# ADR-005: Cloud Infrastructure & Deployment Platform

## Title
ADR-005: Selection of Cloud Hosting Infrastructure and Free-Tier Deployment Platform Strategy

## Status
`Accepted` (Confirmed: Railway for ASP.NET Core Web API & PostgreSQL database, and Vercel for React Web frontend)

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

## Decision
We select **Railway** for hosting the containerized ASP.NET Core Web API and Managed PostgreSQL Database, combined with **Vercel** for hosting the compiled React Web SPA, and local compilation of the Flutter Android APK (`.apk`) for mobile distribution.

---

## Decision Options Evaluated

### Option A: Railway (API + PostgreSQL) + Vercel (React Web) [ACCEPTED]
- *Web API & DB*: ASP.NET Core API deployed as a Railway service using standard Docker containerization, connected to a Railway Managed PostgreSQL Database instance with high-speed private networking.
- *React Web*: Deployed to Vercel global edge CDN with automated GitHub CI/CD deployments and HTTPS.
- *Flutter*: Android `.apk` compiled locally via Flutter SDK and bundled with the assignment submission package.
- *Pros*: Eliminates Render's 15-minute inactivity spin-down delays; native Dockerfile support; private internal network connectivity between API and PostgreSQL; Vercel provides instant edge caching and automated preview deployments; predictable evaluation availability through the 21 October 2026 deadline.
- *Cons*: Requires initial setup of Railway project and environment variables.

### Option B: Render (API + PostgreSQL) + Vercel (React Web)
- *Web API & DB*: ASP.NET Core API deployed as a Render Web Service connected to Render PostgreSQL.
- *React Web*: Deployed to Vercel static hosting.
- *Flutter*: Android `.apk` compiled locally.
- *Pros*: Simple GitHub auto-deploy.
- *Cons*: Render free web services spin down after 15 minutes of inactivity (~30s cold start), which could disrupt rapid evaluator testing during viva.

### Option C: Microsoft Azure for Students (App Service + Azure PostgreSQL)
- *Web API*: Deployed to Azure App Service (Linux .NET 8 runtime).
- *Database*: Azure Database for PostgreSQL Flexible Server.
- *React Web*: Azure Static Web Apps.
- *Pros*: Enterprise cloud ecosystem, native Azure .NET tools.
- *Cons*: Consumes finite SLIIT student Azure credits ($100 cap); high risk of premature suspension before the 21 October 2026 evaluation period.

---

## Evaluation Comparison

| Evaluation Metric | Option A: Railway + Vercel [ACCEPTED] | Option B: Render + Vercel | Option C: Azure for Students |
| :--- | :--- | :--- | :--- |
| **Hosting Cost** | **Free / Starter Tier** | Free Tier | Finite Student Credit ($100 cap) |
| **Cold Start Latency** | **Zero / Low (Always Active)** | ~30s delay after 15m idle | Zero (Always Active) |
| **Setup Complexity** | **Low** (Docker + GitHub auto-deploy) | Low (GitHub auto-deploy) | Medium (Azure Portal / CLI) |
| **Evaluation Reliability** | **High** (Guaranteed through Oct 2026) | Medium (Cold start risks) | High Risk (Credit exhaustion) |
| **Database Networking** | **High-speed Private Network** | Internal Network | Virtual Network / Public IP |
| **Frontend CDN** | **Vercel Global Edge** | Vercel Global Edge | Azure Edge CDN |

---

## Reasons for Decision
1. **Zero Cold-Start Interruption**: Railway services remain reliably responsive during evaluator inspection, avoiding the 30-second spinning latency that occurs with Render's free tier.
2. **Credit Exhaustion Immunity**: Utilizing Railway alongside Vercel protects the team from running out of finite Azure student credits prior to the mandatory 21 October 2026 evaluation cut-off (`REQ-ASSIGN-06`).
3. **Seamless Docker & CI/CD Integration**: Both Railway and Vercel automatically trigger build-and-deploy pipelines upon git push to `main`, matching the repository CI/CD workflow.
4. **Isolated Frontend & Backend**: Vercel handles global edge distribution of the React SPA assets, offloading static traffic from the authoritative ASP.NET Core API on Railway.

---

## Consequences

### Positive
- High-availability deployment accessible to examiners 24/7 through October 2026.
- Fast API response times for health checks and Swagger UI.
- Secure separation between public HTTPS traffic and internal database communication.

### Negative
- Team must configure environment variables (`DATABASE_URL`, `JWT_SECRET`) in Railway project settings.
- Vercel must be supplied with the production Railway API base URL (`VITE_API_URL`).

---

## Deployment Checklist & Verification
1. Verify `GET https://<railway-api-host>/health` returns `200 OK` with database connection verified.
2. Verify `GET https://<railway-api-host>/swagger` displays the interactive OpenAPI documentation.
3. Verify Vercel production URL successfully authenticates and communicates with the Railway backend.
4. Verify local Android `.apk` build executes and connects to the live Railway API endpoints.
