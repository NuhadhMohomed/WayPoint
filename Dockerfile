# ==============================================================================
# WayPoint ASP.NET Core 8 Web API - Production Dockerfile
# Optimized for Railway Cloud Deployment (Zero Code Changes Required)
# ==============================================================================

# --- Stage 1: Build & Publish ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1. Copy solution and directory build properties
COPY backend/Directory.Build.props backend/
COPY backend/WayPoint.sln backend/

# 2. Copy project definitions to maximize Docker layer caching
COPY backend/WayPoint.Domain/WayPoint.Domain.csproj backend/WayPoint.Domain/
COPY backend/WayPoint.Application/WayPoint.Application.csproj backend/WayPoint.Application/
COPY backend/WayPoint.Infrastructure/WayPoint.Infrastructure.csproj backend/WayPoint.Infrastructure/
COPY backend/WayPoint.API/WayPoint.API.csproj backend/WayPoint.API/
COPY backend/WayPoint.Tests/WayPoint.Tests.csproj backend/WayPoint.Tests/

# 3. Restore all project dependencies
RUN dotnet restore "backend/WayPoint.sln"

# 4. Copy backend source code
COPY backend/ backend/

# 5. Compile and publish the Web API
WORKDIR /src/backend/WayPoint.API
RUN dotnet publish "WayPoint.API.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

# --- Stage 2: Runtime Image ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published application from build stage
COPY --from=build /app/publish .

# Expose default backend port
EXPOSE 5010

# Railway provides dynamic $PORT variable; default to 5010 if unset
ENV ASPNETCORE_URLS=http://0.0.0.0:5010
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "WayPoint.API.dll"]
