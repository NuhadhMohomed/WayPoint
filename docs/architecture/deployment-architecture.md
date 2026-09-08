# WayPoint Deployment Architecture Specification

This document defines the deployment topology, cloud infrastructure strategy, containerization, and evaluation availability specs for **WayPoint** (**SE3090 Assignment 1**).

---

## 1. Deployment Topology

WayPoint uses a cloud-hosted infrastructure topology. All public client requests flow into the ASP.NET Core API.

```mermaid
graph TB
    subgraph ClientDevices["Client Devices"]
        MobileDevice["Android Mobile Device (Flutter APK)"]
        Browser["Desktop Browser (React Web App)"]
    end

    subgraph CloudInfra["Production Cloud Infrastructure (Railway & Vercel)"]
        subgraph StaticHosting["Static Frontend Host (Vercel Edge CDN)"]
            ReactApp["Deployed React SPA (Vite Production Build)"]
        end

        subgraph ContainerHosting["Container Host (Railway)"]
            APIApp["ASP.NET Core Web API (.NET 8 Docker Container)"]
            AIModule["Agentic AI Service (FastAPI / LangGraph in ai/)"]
        end

        subgraph ManagedDB["Managed Relational DB (Railway)"]
            PostgresCloud[(Railway Managed PostgreSQL DB)]
        end
    end

    MobileDevice -->|HTTPS / REST| APIApp
    Browser -->|HTTP GET Static Assets| ReactApp
    ReactApp -->|HTTPS / REST| APIApp
    APIApp -->|Npgsql / EF Core (Private Network)| PostgresCloud
    APIApp -->|Internal HTTP Tool Wrappers| AIModule

    %% No direct access
    MobileDevice -. X Prohibited X .- PostgresCloud
    ReactApp -. X Prohibited X .- PostgresCloud
    AIModule -. X Prohibited X .- PostgresCloud
```

---

## 2. Component Deployment Details

### 2.1 ASP.NET Core Web API
- **Deployment Platform**: Railway (Linux Docker container running .NET 8 Kestrel).
- **Public Health Endpoint**: `GET https://<railway-host>/health` returning `200 OK` with database connectivity status (`REQ-DEP-01`).
- **Swagger Documentation URL**: `https://<railway-host>/swagger`.
- **Environment Configuration**: `DATABASE_URL` (private Railway database URL), `JWT_SECRET`, and `CORS_ORIGINS` loaded via Railway service variables.

### 2.2 PostgreSQL Managed Cloud Database
- **Deployment Platform**: Railway Managed PostgreSQL Database service (`waypoint` database).
- **Initialization**: Database schema initialized automatically via Entity Framework Core migrations applied on startup (`context.Database.MigrateAsync()`).
- **Access Credentials**: Private internal host networking within the Railway project environment.

### 2.3 React Web Application
- **Deployment Platform**: Vercel (Global Edge Network static hosting).
- **Configuration**: Compiled production build configured with environment variable `VITE_API_URL` pointing to the public Railway Web API domain.
- **Continuous Deployment**: Automated git-push integration on `main` branch with instant cache invalidation.

### 2.4 Flutter Mobile Application
- **Deliverable**: Compiled Android Application Package (`.apk`) file.
- **Build Command**: `flutter build apk --release`.
- **Distribution**: APK binary delivered alongside the submission package for physical/emulator evaluator installation (`REQ-TECH-05`).

### 2.5 Agentic AI Subsystem
- **Deployment Platform**: Internal Python container service in `ai/` or orchestrated tool runner connected via private internal HTTP to ASP.NET Core.
- **Startup Sequence**:
  1. Railway Managed PostgreSQL Database instance verified active.
  2. ASP.NET Core Web API instance started, migrations applied, baseline data seeded.
  3. Python LangGraph agent service initialized connected to internal backend tool callbacks.

---

## 3. Evaluation Accessibility & Cost Policy

- **No-Cost Policy Compliance**: All cloud hosting uses free-tier or institution-provided cloud resources (`REQ-DEP-06`).
- **Required Access Period**: All live URLs (API, React app, Swagger, DB) and demonstration video links MUST remain fully functional and accessible to evaluators until at least **Wednesday, 21 October 2026** (`REQ-ASSIGN-06`).
