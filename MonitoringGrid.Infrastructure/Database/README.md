# Database Scripts

This directory contains database scripts for the MonitoringGrid system.

## ⚠️ Legacy Scripts (KPI Era)

The following scripts are **LEGACY** and use outdated KPI terminology. They are kept for historical reference but should **NOT** be used for new deployments:

### Legacy Scripts:
- `01_CreateSchema.sql` - Legacy KPI table structure
- `02_CreateKpiTable.sql` - Legacy KPI table creation
- `03_CreateContactsTable.sql` - Legacy contacts structure
- `04_CreateKpiContactsTable.sql` - Legacy KPI-Contact relationships
- `05_CreateAlertLogTable.sql` - Legacy alert log structure
- `06_EnhanceKpiScheduling.sql` - Legacy KPI scheduling enhancements
- `07_CreateHistoricalDataTable.sql` - Legacy historical data structure
- `08_CreateSystemStatusTable.sql` - Legacy system status structure
- `09_CreateConfigTable.sql` - Legacy configuration structure
- `10_AddKpiExecutionTracking.sql` - Legacy KPI execution tracking

## ✅ Current Schema

The current database schema uses **Indicator** terminology and is managed through Entity Framework migrations:

### Current Tables:
- `monitoring.Indicators` - Performance indicators configuration
- `monitoring.IndicatorContacts` - Indicator-Contact relationships
- `monitoring.AlertLogs` - Alert logging with IndicatorId references
- `monitoring.Contacts` - Contact information
- `monitoring.SystemStatus` - System health monitoring
- `monitoring.Config` - System configuration
- `monitoring.Collectors` - Statistics collectors configuration
- `monitoring.MonitorStatistics` - Statistical data collection
- `monitoring.ScheduledJobs` - Job scheduling with IndicatorID references

### Migration Files:
- Use Entity Framework migrations in the `Migrations/` directory
- Latest migrations handle KPI → Indicator transition
- All new development should use EF migrations

## 🚀 For New Deployments

1. **Use Entity Framework migrations** instead of legacy SQL scripts
2. Run `dotnet ef database update` to apply current schema
3. All tables use **Indicator** terminology, not KPI
4. ID fields are `bigint` (long) type, not `int`

## 📝 Notes

- Legacy scripts are preserved for reference only
- Do not modify legacy scripts - they may have been executed in production
- All new database changes should be done through EF migrations
- The system has been fully migrated from KPI to Indicator terminology
