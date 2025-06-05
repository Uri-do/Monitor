# Monitoring Grid System

A comprehensive .NET 8 monitoring system that tracks Key Performance Indicators (KPIs) and sends automated alerts via email and SMS when thresholds are exceeded.

## Features

- **Real-time KPI Monitoring**: Continuously monitors database metrics using stored procedures
- **Historical Comparison**: Compares current values with historical averages (4-week lookback)
- **Multi-channel Alerts**: Sends notifications via email and SMS based on priority levels
- **Configurable Thresholds**: Supports both percentage deviation and absolute value thresholds
- **Cooldown Periods**: Prevents alert flooding with configurable cooldown intervals
- **Parallel Processing**: Processes multiple KPIs concurrently for better performance
- **Comprehensive Logging**: Detailed logging with Serilog for monitoring and debugging
- **Health Checks**: Built-in health monitoring and status reporting
- **Data Retention**: Automatic cleanup of old alert logs and historical data

## Architecture

### Database Schema
- **monitoring.KPIs**: KPI configuration and metadata
- **monitoring.Contacts**: Contact information for notifications
- **monitoring.KpiContacts**: Many-to-many mapping between KPIs and contacts
- **monitoring.AlertLogs**: Historical record of all alerts sent
- **monitoring.HistoricalData**: Time-series data for trend analysis
- **monitoring.Config**: System configuration settings
- **monitoring.SystemStatus**: Service health and status tracking

### Core Components
- **MonitoringWorker**: Background service that orchestrates KPI processing
- **KpiExecutionService**: Executes stored procedures and calculates deviations
- **AlertService**: Manages alert logic and notification coordination
- **EmailService**: Handles email notifications via SMTP
- **SmsService**: Sends SMS via email-to-SMS gateway

## Setup Instructions

### 1. Database Setup

The monitoring system uses two databases:
- **PopAI**: Contains monitoring tables, configuration, and stored procedures
- **ProgressPlayDBTest**: Main application database (read-only access for monitoring)

Execute the SQL scripts in order:

```bash
# Create the PopAI database (if it doesn't exist)
sqlcmd -S 192.168.166.11,1433 -U saturn -P XXXXXXXX -i Database/00_CreateDatabase.sql

# Create the monitoring schema in PopAI
sqlcmd -S 192.168.166.11,1433 -d PopAI -U saturn -P XXXXXXXX -i Database/01_CreateSchema.sql

# Insert initial configuration data
sqlcmd -S 192.168.166.11,1433 -d PopAI -U saturn -P XXXXXXXX -i Database/02_InitialData.sql

# Create monitoring stored procedures (these will query ProgressPlayDBTest)
sqlcmd -S 192.168.166.11,1433 -d PopAI -U saturn -P XXXXXXXX -i Database/03_StoredProcedures.sql
```

**Important**: Ensure the `saturn` user has:
- Full access to the `PopAI` database (created by script 00)
- Read access to the `ProgressPlayDBTest` database for cross-database queries

### 2. Configuration

Update `appsettings.json` with your environment-specific settings:

```json
{
  "ConnectionStrings": {
    "MonitoringGrid": "data source=192.168.166.11,1433;initial catalog=PopAI;user id=saturn;password=XXXXXXXX;asynchronous processing=true;TrustServerCertificate=true",
    "MainDatabase": "data source=192.168.166.11,1433;initial catalog=ProgressPlayDBTest;user id=saturn;password=XXXXXXXX;asynchronous processing=true;TrustServerCertificate=true"
  },
  "Monitoring": {
    "SmsGateway": "your-sms-gateway@example.com",
    "AdminEmail": "admin@yourcompany.com",
    "EnableSms": true,
    "EnableEmail": true
  },
  "Email": {
    "SmtpServer": "your-smtp-server.com",
    "SmtpPort": 587,
    "Username": "your-email@yourcompany.com",
    "Password": "your-email-password",
    "FromAddress": "monitoring@yourcompany.com"
  }
}
```

### 3. Build and Run

```bash
# Restore packages
dotnet restore

# Build the application
dotnet build

# Run in development mode
dotnet run

# Or publish for production
dotnet publish -c Release -o ./publish
```

### 4. Windows Service Installation

To run as a Windows Service using the deployment script:

```powershell
# Run the automated deployment script
.\Scripts\Deploy.ps1 -MonitoringConnectionString "data source=192.168.166.11,1433;initial catalog=PopAI;user id=saturn;password=XXXXXXXX;asynchronous processing=true;TrustServerCertificate=true" -MainConnectionString "data source=192.168.166.11,1433;initial catalog=ProgressPlayDBTest;user id=saturn;password=XXXXXXXX;asynchronous processing=true;TrustServerCertificate=true"

# Or manually:
# Publish the application
dotnet publish -c Release -o C:\Services\MonitoringGrid

# Install as Windows Service
sc create "MonitoringGrid" binPath="C:\Services\MonitoringGrid\MonitoringGrid.exe"

# Start the service
sc start MonitoringGrid
```

## Configuration Options

### Monitoring Settings

| Setting | Description | Default |
|---------|-------------|---------|
| `MaxParallelExecutions` | Maximum concurrent KPI processing | 5 |
| `ServiceIntervalSeconds` | Delay between processing cycles | 30 |
| `AlertRetryCount` | Number of retry attempts for failed alerts | 3 |
| `EnableSms` | Enable SMS notifications | true |
| `EnableEmail` | Enable email notifications | true |
| `EnableHistoricalComparison` | Enable historical trend analysis | true |
| `BatchSize` | Number of KPIs to process per cycle | 10 |
| `MaxAlertHistoryDays` | Days to retain alert history | 90 |

### KPI Configuration

Each KPI supports the following settings:

- **Indicator**: Descriptive name for the KPI
- **Owner**: Person responsible for this metric
- **Priority**: 1 = SMS + Email, 2 = Email only
- **Frequency**: How often to check (in minutes)
- **Deviation**: Acceptable percentage deviation from historical average
- **SpName**: Stored procedure to execute
- **SubjectTemplate**: Email/SMS subject with placeholders
- **DescriptionTemplate**: Email body with placeholders
- **CooldownMinutes**: Minimum time between alerts
- **MinimumThreshold**: Absolute minimum value threshold

### Template Placeholders

Use these placeholders in subject and description templates:

- `{frequency}`: KPI frequency in minutes
- `{current}`: Current measured value
- `{historical}`: Historical average value
- `{deviation}`: Calculated deviation percentage
- `{indicator}`: KPI name
- `{owner}`: KPI owner
- `{timestamp}`: Current timestamp
- `{threshold}`: Minimum threshold value

## Monitoring and Health Checks

The system provides health check endpoints for monitoring:

- Database connectivity
- Service heartbeat status
- KPI processing statistics
- Recent alert activity

Access health checks at: `http://localhost:5000/health`

## Logging

Logs are written to:
- Console (for development)
- File: `logs/monitoring-grid-YYYY-MM-DD.log`
- Windows Event Log (warnings and errors only)

Log levels can be configured in `appsettings.json` under the `Serilog` section.

## Troubleshooting

### Common Issues

1. **Database Connection Errors**
   - Verify connection string
   - Check SQL Server accessibility
   - Ensure user has necessary permissions

2. **Email Sending Failures**
   - Verify SMTP settings
   - Check firewall rules
   - Validate credentials

3. **KPI Execution Errors**
   - Check stored procedure syntax
   - Verify required database objects exist
   - Review SQL Server logs

4. **High Memory Usage**
   - Reduce `MaxParallelExecutions`
   - Decrease `BatchSize`
   - Increase cleanup frequency

### Performance Tuning

- Adjust `ServiceIntervalSeconds` based on monitoring requirements
- Optimize stored procedures for better performance
- Consider database indexing for large datasets
- Monitor resource usage and adjust parallel execution limits

## Security Considerations

- Store sensitive configuration in Azure Key Vault or similar
- Use SQL Server integrated authentication when possible
- Implement network security for database access
- Regular security updates for dependencies
- Monitor for suspicious alert patterns

## Support

For issues and questions:
1. Check the logs for detailed error information
2. Verify configuration settings
3. Test database connectivity manually
4. Review health check status
