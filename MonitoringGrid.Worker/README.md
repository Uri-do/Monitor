# MonitoringGrid.Worker

Background service for the MonitoringGrid system, providing automated Indicator monitoring, scheduled tasks, and system maintenance.

## 🏗️ Architecture

This project implements the **Worker Service** pattern and can run as either a Windows Service or console application. It handles all background processing for the MonitoringGrid system.

### Key Responsibilities:
- **Indicator Monitoring**: Automated execution of Indicators based on schedules
- **Scheduled Tasks**: Data cleanup, maintenance, and housekeeping
- **Health Monitoring**: System health checks and status reporting
- **Real-time Updates**: SignalR integration for live dashboard updates
- **Alert Processing**: Background alert generation and delivery

## 📁 Project Structure

```
MonitoringGrid.Worker/
├── Services/                        # Background services
│   ├── IndicatorMonitoringWorker.cs # Main Indicator execution service
│   ├── ScheduledTaskWorker.cs       # Cleanup and maintenance tasks
│   └── HealthCheckWorker.cs         # System health monitoring
├── Configuration/                   # Configuration models
│   └── WorkerConfiguration.cs       # Worker settings and validation
├── Scripts/                         # PowerShell test scripts
│   └── test-worker.ps1             # Worker testing and validation
├── appsettings.json                # Configuration settings
└── Program.cs                      # Service host configuration
```

## 🚀 Key Features

### Indicator Monitoring
- **Automated Execution**: Runs Indicators based on their configured schedules
- **Parallel Processing**: Configurable parallel execution with throttling
- **Real-time Updates**: Live countdown timers and status updates
- **Error Handling**: Comprehensive error handling and retry logic
- **Performance Metrics**: Execution time tracking and statistics

### Scheduled Tasks
- **Data Cleanup**: Automatic cleanup of old execution history
- **System Maintenance**: Database optimization and housekeeping
- **Configurable Schedules**: Cron-based scheduling with Quartz.NET
- **Retention Policies**: Configurable data retention periods

### Health Monitoring
- **System Health**: Database connectivity and performance checks
- **Indicator Health**: Monitoring of Indicator execution status
- **Alert Processing**: Health check for alert delivery systems
- **Metrics Collection**: Performance and health metrics

### Real-time Communication
- **SignalR Integration**: Live updates to dashboard
- **Countdown Timers**: Real-time countdown to next Indicator execution
- **Status Broadcasting**: Worker status and progress updates
- **Error Notifications**: Real-time error and alert notifications

## 🔧 Configuration

### Worker Configuration (appsettings.json)
```json
{
  "Worker": {
    "IndicatorMonitoring": {
      "IntervalSeconds": 60,
      "MaxParallelIndicators": 5,
      "ExecutionTimeoutSeconds": 300,
      "ProcessOnlyActiveIndicators": true,
      "SkipRunningIndicators": true
    },
    "ScheduledTasks": {
      "Enabled": true,
      "CleanupCronExpression": "0 0 2 * * ?",
      "MaintenanceCronExpression": "0 0 3 ? * SUN",
      "HistoricalDataRetentionDays": 90,
      "LogRetentionDays": 30
    },
    "HealthChecks": {
      "IntervalSeconds": 300,
      "CheckDatabase": true,
      "CheckIndicatorExecution": true,
      "CheckAlertProcessing": true,
      "TimeoutSeconds": 30
    },
    "AlertProcessing": {
      "IntervalSeconds": 30,
      "BatchSize": 100,
      "EnableEscalation": true,
      "EscalationTimeoutMinutes": 60,
      "EnableAutoResolution": true,
      "AutoResolutionTimeoutMinutes": 120
    },
    "ApiBaseUrl": "https://localhost:7001"
  }
}
```

### Database Configuration
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=192.168.166.11,1433;Database=PopAI;User Id=conexusadmin;Password=***;TrustServerCertificate=true;"
  }
}
```

### Logging Configuration
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "MonitoringGrid.Worker": "Debug",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

## 🔄 Background Services

### IndicatorMonitoringWorker
**Primary service for Indicator execution**

**Features:**
- Monitors all active Indicators
- Executes Indicators based on their frequency settings
- Handles parallel execution with configurable limits
- Provides real-time countdown updates
- Integrates with SignalR for live dashboard updates

**Configuration:**
- `IntervalSeconds`: How often to check for due Indicators (default: 60s)
- `MaxParallelIndicators`: Maximum concurrent executions (default: 5)
- `ExecutionTimeoutSeconds`: Timeout for individual executions (default: 300s)

### ScheduledTaskWorker
**Handles maintenance and cleanup tasks**

**Features:**
- Data cleanup based on retention policies
- Database maintenance and optimization
- Cron-based scheduling with Quartz.NET
- Configurable cleanup and maintenance schedules

**Tasks:**
- **Cleanup Job**: Removes old execution history and logs
- **Maintenance Job**: Database optimization and statistics updates

### HealthCheckWorker
**System health monitoring and reporting**

**Features:**
- Database connectivity monitoring
- Indicator execution health checks
- Performance metrics collection
- Health status reporting

**Health Checks:**
- **Database**: Connection and query performance
- **Indicator Execution**: Recent execution success rates
- **System Resources**: Memory and CPU usage

## 📊 Monitoring & Observability

### Metrics
- **Indicator Metrics**: Execution counts, success rates, duration
- **System Metrics**: Memory usage, CPU utilization
- **Health Metrics**: Health check status and response times
- **Performance Metrics**: Database query performance

### Logging
- **Structured Logging**: JSON-formatted logs with correlation IDs
- **Performance Logging**: Execution time tracking
- **Error Logging**: Comprehensive error details and stack traces
- **Audit Logging**: Indicator execution history

### Tracing
- **OpenTelemetry**: Distributed tracing support
- **Activity Sources**: Custom activity sources for monitoring
- **Correlation IDs**: Request correlation across services

## 🚀 Deployment

### Windows Service
```bash
# Install as Windows Service
sc create "MonitoringGrid Worker" binPath="C:\Path\To\MonitoringGrid.Worker.exe"
sc start "MonitoringGrid Worker"

# Configure service
sc config "MonitoringGrid Worker" start=auto
sc description "MonitoringGrid Worker" "MonitoringGrid background processing service"
```

### Console Application
```bash
# Run as console application (development)
dotnet run --project MonitoringGrid.Worker

# Run published version
dotnet MonitoringGrid.Worker.dll
```

### Docker Support
```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:8.0
COPY bin/Release/net8.0/publish/ App/
WORKDIR /App
ENTRYPOINT ["dotnet", "MonitoringGrid.Worker.dll"]
```

## 🧪 Testing

### Unit Testing
```bash
# Run unit tests
dotnet test MonitoringGrid.Worker.Tests
```

### Integration Testing
```bash
# Test worker configuration
.\test-worker.ps1

# Test database connectivity
dotnet run --project MonitoringGrid.Worker -- --test-db

# Test SignalR connectivity
dotnet run --project MonitoringGrid.Worker -- --test-signalr
```

### Health Check Testing
```bash
# Check health endpoints
curl http://localhost:5000/health
```

## 📋 Recent Improvements

### Phase 4: Worker Project Cleanup ✅
- **Legacy KPI References**: Updated configuration from KpiMonitoring to IndicatorMonitoring
- **Health Check Updates**: Renamed KpiExecutionHealthCheck to IndicatorExecutionHealthCheck
- **Configuration Updates**: Updated appsettings.json to use Indicator terminology
- **Test Script Updates**: Updated PowerShell test scripts with correct service names
- **Build Status**: Maintained clean build with 0 errors, 20 warnings (documentation only)

### Key Changes Made:
1. **appsettings.json**: Updated KpiMonitoring → IndicatorMonitoring configuration section
2. **HealthCheckWorker.cs**: Renamed KpiExecutionHealthCheck → IndicatorExecutionHealthCheck
3. **test-worker.ps1**: Updated configuration references and service names
4. **Configuration consistency**: All configuration now uses Indicator terminology

## 🔗 Dependencies

### Core Dependencies
- **.NET 8.0**: Runtime framework
- **Microsoft.Extensions.Hosting**: Background service hosting
- **Entity Framework Core**: Data access
- **Quartz.NET**: Job scheduling

### Monitoring Dependencies
- **OpenTelemetry**: Metrics and tracing
- **Microsoft.Extensions.Diagnostics.HealthChecks**: Health monitoring
- **Serilog**: Structured logging

### Communication Dependencies
- **SignalR Client**: Real-time communication with API
- **Microsoft.AspNetCore.SignalR.Client**: SignalR client library

### Resilience Dependencies
- **Polly**: Retry policies and circuit breakers
- **FluentValidation**: Configuration validation

## 📞 Support

### Configuration Issues
- Check appsettings.json for correct connection strings
- Verify database connectivity and permissions
- Ensure SignalR hub URL is accessible

### Performance Issues
- Adjust MaxParallelIndicators based on system capacity
- Monitor database query performance
- Check memory usage and garbage collection

### Deployment Issues
- Verify Windows Service permissions
- Check firewall settings for SignalR connectivity
- Ensure database server accessibility

For additional support, refer to the main project documentation or contact the development team.
