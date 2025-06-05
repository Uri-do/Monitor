# Multi-stage build for MonitoringGrid application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Install necessary packages for production
RUN apt-get update && apt-get install -y \
    curl \
    wget \
    unzip \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["MonitoringGrid.Api/MonitoringGrid.Api.csproj", "MonitoringGrid.Api/"]
COPY ["MonitoringGrid.Core/MonitoringGrid.Core.csproj", "MonitoringGrid.Core/"]
COPY ["MonitoringGrid.Infrastructure/MonitoringGrid.Infrastructure.csproj", "MonitoringGrid.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "MonitoringGrid.Api/MonitoringGrid.Api.csproj"

# Copy source code
COPY . .

# Build application
WORKDIR "/src/MonitoringGrid.Api"
RUN dotnet build "MonitoringGrid.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MonitoringGrid.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build frontend
FROM node:18-alpine AS frontend-build
WORKDIR /app/frontend

# Copy package files
COPY MonitoringGrid.Frontend/package*.json ./
RUN npm ci --only=production

# Copy frontend source
COPY MonitoringGrid.Frontend/ ./

# Build frontend
RUN npm run build

# Final stage
FROM base AS final
WORKDIR /app

# Copy backend
COPY --from=publish /app/publish .

# Copy frontend build
COPY --from=frontend-build /app/frontend/dist ./wwwroot

# Create directories for logs and data
RUN mkdir -p /app/logs /app/data && \
    chown -R appuser:appuser /app

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Switch to non-root user
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

ENTRYPOINT ["dotnet", "MonitoringGrid.Api.dll"]
