# Monitoring Grid Docker Image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Install SQL Server tools for health checks
RUN apt-get update && apt-get install -y \
    curl \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["MonitoringGrid.csproj", "."]
RUN dotnet restore "MonitoringGrid.csproj"

# Copy source code and build
COPY . .
RUN dotnet build "MonitoringGrid.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MonitoringGrid.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Create logs directory
RUN mkdir -p /app/logs

# Copy published application
COPY --from=publish /app/publish .

# Create non-root user for security
RUN groupadd -r monitoring && useradd -r -g monitoring monitoring
RUN chown -R monitoring:monitoring /app
USER monitoring

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "MonitoringGrid.dll"]
