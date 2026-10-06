# WayPoint — Full-Stack Cloud Deployment Guide

**Project**: WayPoint — Integrated Full-Stack & Agentic AI Transit Platform  
**Course**: SE3090 — Integrated Full-Stack and Agentic AI Application Development (Assignment 1)  
**Standard**: SE3090 Deployment, DevOps & Infrastructure Marking Criteria (10 Group Marks)  

---

## 1. Cloud Architecture & Deployment Topology

WayPoint is deployed as an integrated multi-tier enterprise architecture across resilient cloud infrastructure platforms:

```mermaid
graph TD
    subgraph ClientTiers["Client Presentation Tiers"]
        ReactWeb["React 18 Web App (Vite / Tailwind / Zustand)<br/>Hosted on Render (Static Site)<br/>https://waypoint-web.onrender.com"]
        FlutterMobile["Flutter 3.x Mobile App (Dart 3 / BLoC)<br/>Android Release APK<br/>mobile/.../app-release.apk"]
    end

    subgraph CloudBackend["Authoritative Cloud Backend (Railway)"]
        RailwayAPI["ASP.NET Core 8 Web API (.NET 8)<br/>Containerized Service on Railway<br/>https://waypoint-api-production.up.railway.app"]
        RailwayDB["PostgreSQL 18.6 Cloud Database<br/>Managed Container on Railway<br/>Direct SSL + ALPN 'postgresql'"]
    end

    subgraph AISubsystem["Agentic AI Subsystem"]
        PythonAI["LangGraph Multi-Agent Service (FastAPI)<br/>Python 3.11 Microservice<br/>Autonomous 5-Node Workflow"]
        GeminiAPI["Google Gemini 1.5 Flash API<br/>External LLM Engine"]
    end

    ReactWeb -->|HTTPS / REST / JWT| RailwayAPI
    FlutterMobile -->|HTTPS / REST / JWT| RailwayAPI
    RailwayAPI -->|Direct SSL ALPN| RailwayDB
    RailwayAPI <-->|HTTP / REST (Port 8000)| PythonAI
    PythonAI -->|REST Allow-Listed Tools Only| RailwayAPI
    PythonAI -->|HTTPS / JSON Prompts| GeminiAPI
```

---

## 2. Infrastructure Summary & Live Endpoints

| Tier / Subsystem | Technology Stack | Hosting Platform | URL / Deployment Artifact | Status |
| :--- | :--- | :--- | :--- | :--- |
| **Web Frontend** | React 18, Vite, Tailwind CSS, Zustand | **Render** | `https://waypoint-web.onrender.com` | Production Active |
| **Backend API** | ASP.NET Core 8, EF Core 9, Npgsql 9 | **Railway** | `https://waypoint-api-production.up.railway.app` | Production Active |
| **Swagger UI** | OpenAPI 3.0 / Swashbuckle | **Railway** | `https://waypoint-api-production.up.railway.app/swagger` | Production Active |
| **Database** | PostgreSQL 18.6 (Debian 64-bit) | **Railway** | `proxy.rlwy.net:PORT/railway` (Direct SSL) | Production Active |
| **Mobile Client** | Flutter 3.x, Dart 3, flutter_bloc | **Android APK** | `mobile/build/app/outputs/flutter-apk/app-release.apk` | Release Artifact |
| **Agentic AI** | LangGraph, FastAPI, Python 3.11 | **Railway / Local** | `http://localhost:8000` / Cloud Container | Active |

---

## 3. Tier 1: Cloud Database Deployment (Railway PostgreSQL 18)

WayPoint provisions a managed PostgreSQL 18.6 container on Railway.

### 3.1 Connection String & ALPN Direct SSL Requirement
Railway provisions PostgreSQL 18 with Direct SSL negotiation. WayPoint configures **Npgsql 9.0.4** with `SslNegotiation = SslNegotiation.Direct` and ALPN `postgresql` ([ADR-005](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/docs/adr/ADR-005-cloud-deployment.md), [database-railway-guide.md](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/docs/deployment/database-railway-guide.md)):

```env
DATABASE_URL=postgresql://postgres:<password>@<host>:<port>/railway?sslmode=Require
```

### 3.2 Applying EF Core Migrations in the Cloud
To migrate and seed the database schema:

```bash
# Apply EF Core migrations to Railway database
dotnet ef database update --project backend/WayPoint.Infrastructure --startup-project backend/WayPoint.API

# Seed initial Sri Lankan routes, operators, and bus fleet
dotnet run --project backend/WayPoint.API -- --seed
```

---

## 4. Tier 2: Backend Cloud Deployment (Railway ASP.NET Core)

### 4.1 Railway Deployment Configuration
Railway automatically builds and executes the Dockerized ASP.NET Core backend from the repository root or `backend/` directory.

### 4.2 Required Environment Variables on Railway
Configure these environment variables in the Railway dashboard:

```env
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:5010
DATABASE_URL=postgresql://postgres:PASSWORD@proxy.rlwy.net:PORT/railway?sslmode=Require
JWT_SECRET=WayPoint_Super_Secret_Key_For_Jwt_Token_Authentication_2026_SE3090_Cloud
AI_SERVICE_URL=http://localhost:8000
```

### 4.3 Smoke Testing the Deployed API
```bash
# Health check endpoint
curl -i https://waypoint-api-production.up.railway.app/health

# Verify Swagger OpenAPI documentation
curl -i https://waypoint-api-production.up.railway.app/swagger/v1/swagger.json
```

---

## 5. Tier 3: React Web Frontend Deployment (Render Static Site)

The React web application ([web/](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/web/)) is deployed as a high-performance static site on **Render**.

### 5.1 Render Build Settings
- **Service Type**: Static Site
- **Repository**: `https://github.com/NuhadhMohomed/WayPoint`
- **Root Directory**: `web`
- **Build Command**: `npm install && npm run build`
- **Publish Directory**: `dist`

### 5.2 Environment Variables on Render
```env
VITE_API_BASE_URL=https://waypoint-api-production.up.railway.app/api/v1
```

### 5.3 Single Page Application (SPA) Routing Rule
Render requires a rewrite rule to redirect all client-side route paths to `index.html`:
- **Source**: `/*`
- **Destination**: `/index.html`
- **Action**: `Rewrite`

---

## 6. Tier 4: Flutter Mobile Client Release Build (Android APK)

The Flutter mobile application ([mobile/](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/mobile/)) compiles into a standalone Android APK.

### 6.1 Prerequisites
- Flutter SDK (v3.24+)
- Android SDK Platform 34 / Java 17

### 6.2 Building the Release APK
From the `mobile/` directory, execute:

```bash
cd mobile

# Fetch Flutter dependencies
flutter pub get

# Compile release APK with production API endpoint
flutter build apk --release --dart-define=API_BASE_URL=https://waypoint-api-production.up.railway.app/api/v1
```

### 6.3 Deployment Artifact
Upon build completion, the distributable APK resides at:
```text
mobile/build/app/outputs/flutter-apk/app-release.apk
```

### 6.4 Installing APK onto Physical Device or Emulator
```bash
adb install -r mobile/build/app/outputs/flutter-apk/app-release.apk
```

---

## 7. Tier 5: Agentic AI Subsystem Deployment (FastAPI / LangGraph)

The Python AI subsystem ([ai/](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/ai/)) runs as an autonomous microservice.

### 7.1 Configuration
```env
API_BASE_URL=https://waypoint-api-production.up.railway.app/api/v1
GEMINI_API_KEY=<Google_Gemini_API_Key>
LLM_MODEL=gemini-1.5-flash
LLM_TEMPERATURE=0.2
PORT=8000
```

### 7.2 Execution
```bash
cd ai
python -m venv venv
source venv/bin/activate  # Or .\venv\Scripts\Activate.ps1 on Windows
pip install -r requirements.txt
uvicorn main:app --host 0.0.0.0 --port 8000
```

---

## 8. Verification & Live Grading Demonstration Checklist

During grading evaluation, verify connectivity across all integrated tiers:

1. **Database & API Connectivity**:
   - Access Swagger at `https://waypoint-api-production.up.railway.app/swagger`.
   - Execute `GET /api/v1/routes` to confirm active Sri Lankan routes populate from PostgreSQL.
2. **Web Application Operational Dashboards**:
   - Navigate to `https://waypoint-web.onrender.com`.
   - Log in using Operator credentials (`operator@waypoint.lk` / `Operator@123`).
   - Confirm active fleet inventory, seat layouts, and booking manifests load live.
3. **Mobile Passenger Experience**:
   - Launch Flutter App on device/emulator.
   - Search route: `Colombo` $\rightarrow$ `Kandy`.
   - Select Seat `12A` $\rightarrow$ confirm 10-minute hold countdown timer begins.
   - Complete checkout $\rightarrow$ confirm QR ticket renders in passenger wallet.
4. **Autonomous AI Multi-Agent Observability**:
   - Log in to Web App as Transport Manager (`manager@waypoint.lk` / `Manager@123`).
   - Open AI Observability page $\rightarrow$ review LangGraph multi-agent execution steps, tool latencies, and deterministic validation outputs stored in PostgreSQL.
