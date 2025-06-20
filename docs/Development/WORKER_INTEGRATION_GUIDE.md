# MonitoringGrid Worker Integration Guide

## Overview
The MonitoringGrid API now supports optional integration with the dedicated Worker services, giving you flexibility in deployment architecture. You can choose between:

1. **Separate Services** (Recommended for Production): Run API and Worker as separate processes
2. **Integrated Services** (Convenient for Development): Run Worker services within the API process

## Configuration

### Enabling Worker Services in API
Set the `EnableWorkerServices` flag in your configuration:

```json
{
  "Monitoring": {
    "EnableWorkerServices": true
  }
}
```

### Configuration Sections

#### API Configuration (appsettings.json)
```json
{
  "Monitoring": {
    "EnableWorkerServices": false,  // Set to true to enable Worker services in API
    "MaxParallelExecutions": 5,
    "ServiceIntervalSeconds": 30
  },
  "Worker": {
    "IndicatorMonitoring": {
      "IntervalSeconds": 60,
      "MaxParallelIndicators": 5,
      "ExecutionTimeoutSeconds": 300
    },
    "ScheduledTasks": {
      "Enabled": true,
      "CleanupCronExpression": "0 0 2 * * ?",
      "HistoricalDataRetentionDays": 90
    },
    "HealthChecks": {
      "IntervalSeconds": 300,
      "TimeoutSeconds": 30
    },
    "AlertProcessing": {
      "IntervalSeconds": 30,
      "BatchSize": 100,
      "EnableAutoResolution": true
    }
  }
}
```

#### Development Configuration (appsettings.Development.json)
```json
{
  "Monitoring": {
    "EnableWorkerServices": true  // Enabled by default in development
  },
  "Worker": {
    "IndicatorMonitoring": {
      "IntervalSeconds": 30,      // Faster intervals for development
      "MaxParallelIndicators": 3
    },
    "ScheduledTasks": {
      "CleanupCronExpression": "0 */5 * * * ?",  // Every 5 minutes
      "HistoricalDataRetentionDays": 7
    }
  }
}
```

## Deployment Scenarios

### Scenario 1: Separate Services (Production Recommended)

**API Configuration:**
```json
{
  "Monitoring": {
    "EnableWorkerServices": false
  }
}
```

**Deployment:**
```bash
# Start API
dotnet run --project MonitoringGrid.Api

# Start Worker (separate process/server)
dotnet run --project MonitoringGrid.Worker
```

**Benefits:**
- ✅ Better resource isolation
- ✅ Independent scaling
- ✅ Fault isolation
- ✅ Easier monitoring and debugging
- ✅ Can run on different servers

### Scenario 2: Integrated Services (Development/Small Deployments)

**API Configuration:**
```json
{
  "Monitoring": {
    "EnableWorkerServices": true
  }
}
```

**Deployment:**
```bash
# Start API with integrated Worker services
dotnet run --project MonitoringGrid.Api
```

**Benefits:**
- ✅ Simpler deployment
- ✅ Single process to manage
- ✅ Shared configuration
- ✅ Ideal for development

## Service Registration

When `EnableWorkerServices` is true, the API automatically registers:

### Worker Services
- `KpiMonitoringWorker` - Automated KPI execution
- `ScheduledTaskWorker` - Database maintenance and cleanup
- `HealthCheckWorker` - System health monitoring
- `AlertProcessingWorker` - Alert processing and escalation
- `Worker` - Main coordinator service

### Additional Components
- Quartz.NET scheduler
- Worker-specific health checks
- Worker configuration validation

## Health Checks

### Separate Services
- **API Health**: `/health` - API-specific health checks
- **Worker Health**: Worker service internal health monitoring

### Integrated Services
- **Combined Health**: `/health` - Includes both API and Worker health checks
- Additional endpoints:
  - `worker-kpi-execution` - KPI execution system health
  - `worker-alert-processing` - Alert processing system health

## Monitoring and Observability

### Metrics
When Worker services are enabled in API:
- All Worker metrics are available through API metrics endpoints
- Combined telemetry and logging
- Unified observability dashboard

### Logging
- Structured logging includes both API and Worker events
- Performance logging for all operations
- Centralized log aggregation

## Migration Guide

### From Legacy Scheduler to Worker Services

1. **Update Configuration:**
   ```json
   {
     "Monitoring": {
       "EnableWorkerServices": true
     }
   }
   ```

2. **Remove Legacy Services:**
   The old `EnhancedKpiSchedulerService` is automatically disabled when Worker services are enabled.

3. **Update Monitoring:**
   - Health check endpoints now include Worker services
   - Metrics include Worker-specific measurements

### Configuration Migration
Old monitoring settings are still respected, but Worker configuration takes precedence for background operations.

## Troubleshooting

### Common Issues

1. **Package Version Conflicts:**
   - Ensure Quartz packages are version 3.13.1 in both API and Worker projects
   - Run `dotnet restore` after updating packages

2. **Service Registration Errors:**
   - Verify Worker project reference is added to API
   - Check that all required using statements are present

3. **Configuration Issues:**
   - Validate JSON configuration syntax
   - Ensure Worker section is present when `EnableWorkerServices` is true

### Debugging

#### Check Service Registration:
```csharp
// In development, you can verify services are registered
var services = app.Services.GetServices<IHostedService>();
foreach (var service in services)
{
    Console.WriteLine($"Registered service: {service.GetType().Name}");
}
```

#### Monitor Worker Activity:
- Check logs for Worker service startup messages
- Monitor health check endpoints
- Verify KPI execution in database

## Performance Considerations

### Resource Usage
- **Integrated Services**: Higher memory usage, shared CPU resources
- **Separate Services**: Better resource isolation, independent scaling

### Scaling
- **Integrated**: Scale API and Worker together
- **Separate**: Scale API and Worker independently based on load

### Database Connections
- Both configurations use the same database connection strings
- Connection pooling is shared in integrated mode
- Separate connection pools in separate services mode

## Security Considerations

### Authentication
- Worker services inherit API security configuration when integrated
- Separate Worker services should be secured independently

### Network Security
- Integrated services: Single network endpoint
- Separate services: Multiple endpoints to secure

## Best Practices

### Development
- Use integrated services (`EnableWorkerServices: true`)
- Faster intervals for quicker feedback
- Enhanced logging for debugging

### Production
- Use separate services (`EnableWorkerServices: false`)
- Deploy Worker on dedicated infrastructure
- Monitor both services independently
- Implement proper health checks and alerting

### Configuration Management
- Use environment-specific configuration files
- Secure sensitive configuration with Azure Key Vault or similar
- Validate configuration on startup

## Conclusion

The Worker integration provides flexibility to choose the deployment model that best fits your needs:

- **Development**: Use integrated services for simplicity
- **Production**: Use separate services for better isolation and scaling

Both approaches provide the same functionality with different operational characteristics.
