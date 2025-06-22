# ⚙️ Configuration Standardization Guide

This guide covers the standardized configuration system for MonitoringGrid, implemented as part of Phase 10 cleanup.

## 🎯 Overview

The MonitoringGrid configuration system has been standardized to provide:
- **Consistent structure** across all projects
- **Validation and type safety** with strongly-typed options
- **Environment-specific overrides** for development/production
- **Legacy compatibility** with migration path
- **Comprehensive validation** with detailed error reporting

## 📋 Configuration Structure

### **Standardized Configuration Hierarchy**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "...",
    "SourceDatabase": "..."
  },
  "MonitoringGrid": {
    "Database": { ... },
    "Monitoring": { ... },
    "Email": { ... },
    "Security": { ... },
    "Worker": { ... }
  },
  "Logging": { ... },
  "AllowedHosts": "*"
}
```

### **Connection Strings**

**Standardized Names:**
- `DefaultConnection` - Main monitoring database (PopAI)
- `SourceDatabase` - Source database for monitoring (ProgressPlayDBTest)

**Legacy Names (deprecated):**
- ~~`MonitoringGrid`~~ → Use `DefaultConnection`
- ~~`ProgressPlayDB`~~ → Use `SourceDatabase`

## 🔧 Configuration Sections

### **Database Configuration**

```json
"MonitoringGrid": {
  "Database": {
    "DefaultConnection": "Data Source=server;Initial Catalog=PopAI;...",
    "SourceDatabase": "Data Source=server;Initial Catalog=ProgressPlayDBTest;...",
    "TimeoutSeconds": 30,
    "EnablePooling": true,
    "MaxPoolSize": 50
  }
}
```

### **Monitoring Configuration**

```json
"Monitoring": {
  "MaxParallelExecutions": 5,
  "ServiceIntervalSeconds": 30,
  "AlertRetryCount": 3,
  "EnableSms": true,
  "EnableEmail": true,
  "EnableHistoricalComparison": true,
  "EnableAbsoluteThresholds": true,
  "BatchSize": 10,
  "MaxAlertHistoryDays": 90,
  "HistoricalWeeksBack": 4,
  "DefaultLastMinutes": 1440,
  "DefaultFrequency": 60,
  "DefaultCooldownMinutes": 30,
  "EnableWorkerServices": true,
  "SmsGateway": "sms-gateway@example.com",
  "AdminEmail": "admin@example.com"
}
```

### **Email Configuration**

```json
"Email": {
  "SmtpServer": "smtp.example.com",
  "SmtpPort": 587,
  "Username": "monitoring@example.com",
  "Password": "your-email-password",
  "EnableSsl": true,
  "FromAddress": "monitoring@example.com",
  "FromName": "Monitoring Grid System",
  "TimeoutSeconds": 30
}
```

### **Security Configuration**

```json
"Security": {
  "Jwt": {
    "SecretKey": "your-secret-key-at-least-32-characters",
    "Issuer": "MonitoringGrid.Api",
    "Audience": "MonitoringGrid.Frontend",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 30,
    "Algorithm": "HS256"
  },
  "PasswordPolicy": {
    "MinimumLength": 8,
    "RequireUppercase": true,
    "RequireLowercase": true,
    "RequireDigit": true,
    "RequireSpecialCharacter": true,
    "MaxFailedAttempts": 5,
    "LockoutDurationMinutes": 30,
    "PasswordExpirationDays": 90,
    "PasswordHistoryCount": 5
  },
  "Session": {
    "TimeoutMinutes": 60,
    "ExtendOnActivity": true,
    "MaxConcurrentSessions": 3
  },
  "RateLimit": {
    "IsEnabled": true,
    "MaxRequestsPerMinute": 100,
    "MaxLoginAttemptsPerMinute": 5,
    "BanDurationMinutes": 15
  },
  "Encryption": {
    "EncryptionKey": "base64-encoded-encryption-key",
    "HashingSalt": "base64-encoded-hashing-salt"
  }
}
```

### **Worker Configuration**

```json
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
  "Logging": {
    "MinimumLevel": "Information",
    "EnableStructuredLogging": true,
    "EnablePerformanceLogging": false,
    "LogToConsole": true,
    "LogToEventLog": false
  }
}
```

## 🔍 Configuration Validation

### **Automatic Validation**

The system automatically validates configuration on startup:

```csharp
// In Program.cs or Startup.cs
services.AddMonitoringGridConfiguration(configuration);
configuration.ValidateConfiguration();
```

### **Manual Validation Tool**

Run the configuration validation tool:

```bash
# Validate all configuration files
dotnet run --project MonitoringGrid.Infrastructure -- config validate

# Validate specific files
dotnet run --project MonitoringGrid.Infrastructure -- config validate appsettings.json appsettings.Development.json
```

### **Validation Rules**

**Required Sections:**
- `ConnectionStrings:DefaultConnection`
- `ConnectionStrings:SourceDatabase`
- `MonitoringGrid:Database`
- `MonitoringGrid:Monitoring`
- `MonitoringGrid:Security:Jwt`

**Business Rules:**
- JWT SecretKey must be at least 32 characters
- Email SMTP settings required if email is enabled
- SMS gateway required if SMS is enabled
- Database timeout should be at least 30 seconds
- Worker parallel settings should not exceed monitoring limits

**Environment-Specific Rules:**
- **Development**: Worker services disabled by default
- **Production**: No default/placeholder secrets allowed

## 🔄 Migration from Legacy Configuration

### **Connection String Migration**

**Before (Legacy):**
```json
"ConnectionStrings": {
  "MonitoringGrid": "...",
  "ProgressPlayDB": "..."
}
```

**After (Standardized):**
```json
"ConnectionStrings": {
  "DefaultConnection": "...",
  "SourceDatabase": "..."
}
```

### **Section Structure Migration**

**Before (Legacy):**
```json
{
  "Monitoring": { ... },
  "Email": { ... },
  "Security": { ... },
  "Worker": { ... }
}
```

**After (Standardized):**
```json
{
  "MonitoringGrid": {
    "Monitoring": { ... },
    "Email": { ... },
    "Security": { ... },
    "Worker": { ... }
  }
}
```

### **Worker Configuration Migration**

**Before (Legacy KPI references):**
```json
"Worker": {
  "IndicatorMonitoring": {
    "MaxParallelIndicators": 3
  }
}
```

**After (Standardized Indicator references):**
```json
"Worker": {
  "IndicatorMonitoring": {
    "MaxParallelIndicators": 3
  }
}
```

## 🛠️ Development Usage

### **Strongly-Typed Configuration**

```csharp
// Inject specific configuration sections
public class MyService
{
    private readonly MonitoringOptions _monitoringOptions;
    private readonly DatabaseOptions _databaseOptions;

    public MyService(
        IOptions<MonitoringOptions> monitoringOptions,
        IOptions<DatabaseOptions> databaseOptions)
    {
        _monitoringOptions = monitoringOptions.Value;
        _databaseOptions = databaseOptions.Value;
    }
}
```

### **Configuration Extensions**

```csharp
// Get validated configuration section
var options = configuration.GetValidatedSection<MonitoringOptions>("MonitoringGrid:Monitoring");

// Get connection string with validation
var connectionString = configuration.GetConnectionStringOrThrow("DefaultConnection");

// Create configuration summary for logging
var summary = configuration.CreateConfigurationSummary();
logger.LogInformation(summary);
```

## 📊 Configuration Summary

The system provides a configuration summary for logging and debugging:

```
=== MonitoringGrid Configuration Summary ===

Database Timeout: 30s
Max Parallel Executions: 5
Service Interval: 30s
Email Enabled: True
SMS Enabled: True
Worker Services Enabled: True
Historical Comparison: True
Alert Retention: 90 days

Worker Configuration:
  Indicator Monitoring Interval: 60s
  Max Parallel Indicators: 5
  Execution Timeout: 300s
  Scheduled Tasks Enabled: True

Security Configuration:
  JWT Issuer: MonitoringGrid.Api
  JWT Audience: MonitoringGrid.Frontend
  Access Token Expiration: 60 minutes
  Rate Limiting Enabled: True
  Max Requests/Minute: 100

Connection Strings:
  DefaultConnection: Data Source=192.168.166.11,1433; Initial Catalog=PopAI
  SourceDatabase: Data Source=192.168.166.11,1433; Initial Catalog=ProgressPlayDBTest

=== End Configuration Summary ===
```

## 🔒 Security Considerations

### **Production Security**

- **Never use default secrets** in production
- **Use environment variables** for sensitive data
- **Rotate encryption keys** regularly
- **Monitor configuration changes**

### **Development Security**

- **Use placeholder values** for development
- **Keep development configs** in source control
- **Use separate databases** for development
- **Enable detailed logging** for debugging

## 🚀 Best Practices

1. **Use the standardized structure** for all new configuration
2. **Validate configuration** on application startup
3. **Use strongly-typed options** instead of raw configuration access
4. **Document configuration changes** in deployment notes
5. **Test configuration** in staging before production
6. **Monitor configuration** for security issues
7. **Use environment-specific overrides** appropriately

---

**Last Updated**: June 2025  
**Configuration Version**: 2.0 (Standardized)  
**Status**: ✅ Production Ready
