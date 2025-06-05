# Dual Database Setup for Monitoring Grid

## Overview

The Monitoring Grid system has been updated to use a **dual database architecture**:

1. **PopAI Database** - Contains all monitoring-specific tables and stored procedures
2. **ProgressPlayDBTest Database** - Main application database (read-only access for monitoring)

This separation provides several benefits:
- **Isolation**: Monitoring system doesn't interfere with main application
- **Security**: Limited access to main database (read-only)
- **Performance**: Monitoring operations don't impact main database performance
- **Maintenance**: Easier to backup, restore, and maintain monitoring data separately

## Database Architecture

### PopAI Database (Monitoring Database)
**Purpose**: Stores all monitoring configuration, historical data, and alert logs

**Tables**:
- `monitoring.KPIs` - KPI configuration and metadata
- `monitoring.Contacts` - Contact information for notifications
- `monitoring.KpiContacts` - KPI-Contact mappings
- `monitoring.AlertLogs` - Historical record of all alerts
- `monitoring.HistoricalData` - Time-series data for trend analysis
- `monitoring.Config` - System configuration settings
- `monitoring.SystemStatus` - Service health monitoring

**Stored Procedures**:
- `monitoring.usp_MonitorDeposits`
- `monitoring.usp_MonitorTransactions`
- `monitoring.usp_MonitorSettlementCompanies`
- `monitoring.usp_MonitorCountryDeposits`
- `monitoring.usp_MonitorWhiteLabelPerformance`

### ProgressPlayDBTest Database (Main Application Database)
**Purpose**: Main application database - accessed read-only by monitoring stored procedures

**Accessed Tables**:
- `accounts.tbl_Account_transactions`
- `accounts.tbl_Account_payment_methods`
- `accounts.tbl_Settlement_companies`
- `common.tbl_Players`
- `common.tbl_Countries`
- `common.tbl_White_labels`

## Connection Strings

The application now uses two connection strings:

```json
{
  "ConnectionStrings": {
    "MonitoringGrid": "data source=192.168.166.11,1433;initial catalog=PopAI;user id=saturn;password=XXXXXXXX;asynchronous processing=true;TrustServerCertificate=true",
    "MainDatabase": "data source=192.168.166.11,1433;initial catalog=ProgressPlayDBTest;user id=saturn;password=XXXXXXXX;asynchronous processing=true;TrustServerCertificate=true"
  }
}
```

## Setup Instructions

### 1. Create PopAI Database
```sql
sqlcmd -S 192.168.166.11,1433 -U saturn -P XXXXXXXX -i Database/00_CreateDatabase.sql
```

### 2. Create Monitoring Schema
```sql
sqlcmd -S 192.168.166.11,1433 -d PopAI -U saturn -P XXXXXXXX -i Database/01_CreateSchema.sql
```

### 3. Insert Initial Data
```sql
sqlcmd -S 192.168.166.11,1433 -d PopAI -U saturn -P XXXXXXXX -i Database/02_InitialData.sql
```

### 4. Create Stored Procedures
```sql
sqlcmd -S 192.168.166.11,1433 -d PopAI -U saturn -P XXXXXXXX -i Database/03_StoredProcedures.sql
```

### 5. Deploy Application
```powershell
.\Scripts\Deploy.ps1 -MonitoringConnectionString "data source=192.168.166.11,1433;initial catalog=PopAI;user id=saturn;password=XXXXXXXX;asynchronous processing=true;TrustServerCertificate=true" -MainConnectionString "data source=192.168.166.11,1433;initial catalog=ProgressPlayDBTest;user id=saturn;password=XXXXXXXX;asynchronous processing=true;TrustServerCertificate=true"
```

## Cross-Database Queries

The stored procedures in PopAI database use **cross-database queries** to access data from ProgressPlayDBTest:

```sql
-- Example from monitoring.usp_MonitorDeposits
SELECT @CurrentValue = COUNT(*)
FROM [ProgressPlayDBTest].accounts.tbl_Account_transactions at (NOLOCK)
WHERE at.updated_dt >= @StartTime
    AND at.transaction_type_id = 263
    AND at.is_done = 1
```

## Security Considerations

### Database Permissions
The `saturn` user needs:

**PopAI Database**:
- `db_datareader` - Read access to monitoring tables
- `db_datawriter` - Write access for logging and historical data
- `db_ddladmin` - Schema modification rights (for initial setup)
- `EXECUTE` - Permission to run stored procedures

**ProgressPlayDBTest Database**:
- `db_datareader` - Read-only access to application tables
- **NO WRITE ACCESS** - Monitoring should never modify main application data

### Network Security
- Both databases are on the same server (192.168.166.11)
- Same user account (`saturn`) for simplified management
- TrustServerCertificate=true for internal network communication

## Monitoring and Maintenance

### Health Checks
The application monitors both database connections:
- PopAI database connectivity (primary)
- ProgressPlayDBTest database accessibility (for queries)

### Backup Strategy
- **PopAI Database**: Regular backups of monitoring configuration and historical data
- **ProgressPlayDBTest**: Existing backup strategy (not affected by monitoring system)

### Performance Impact
- Minimal impact on main database (read-only queries with NOLOCK hints)
- Monitoring queries are optimized and run at configurable intervals
- Historical data cleanup prevents unbounded growth

## Troubleshooting

### Common Issues

1. **Cross-database query failures**
   - Verify `saturn` user has read access to ProgressPlayDBTest
   - Check if both databases are accessible from the same connection

2. **Permission errors**
   - Ensure proper database roles are assigned
   - Verify EXECUTE permissions on stored procedures

3. **Connection string issues**
   - Validate both connection strings in configuration
   - Test connectivity to both databases separately

### Testing Connectivity
Use the provided test script:
```bash
dotnet run --project Scripts/TestConnection.cs
```

This will verify:
- Connection to both databases
- Schema existence in PopAI
- Stored procedure execution
- Cross-database query functionality

## Migration from Single Database

If migrating from a single database setup:

1. **Backup existing data** (if any)
2. **Create PopAI database** using provided scripts
3. **Update connection strings** in configuration
4. **Migrate existing monitoring data** (if applicable)
5. **Test thoroughly** before production deployment

## Benefits of This Architecture

✅ **Separation of Concerns**: Monitoring isolated from main application
✅ **Security**: Limited access to production data
✅ **Performance**: No impact on main application performance
✅ **Scalability**: Monitoring database can be scaled independently
✅ **Maintenance**: Easier backup and maintenance procedures
✅ **Compliance**: Better audit trail and data governance
