# 🗄️ MonitoringGrid Database Documentation

This directory contains all database-related scripts and documentation for the MonitoringGrid system, organized for clarity and maintainability.

## 📁 Directory Structure

```
MonitoringGrid.Infrastructure/Database/
├── README.md                    # This documentation
├── Setup/                       # Initial database setup scripts
│   ├── 00_Complete_Database_Setup.sql
│   ├── 01_CreateSchema.sql
│   ├── 02_CreateAuthSchema.sql
│   ├── 03_SeedAuthData.sql
│   ├── add_missing_auth_tables.sql
│   ├── check_auth_tables.sql
│   └── setup_database.sql
├── Current/                     # Current active scripts
│   ├── 03_StoredProcedures.sql
│   ├── 08_CreateSchedulersTable.sql
│   └── 09_CleanupSchedulingMigration.sql
├── Performance/                 # Performance optimization scripts
│   └── PerformanceOptimization.sql
├── Migrations/                  # Entity Framework migrations
│   └── [EF migration files]
└── Archive/                     # Legacy and archived scripts
    ├── 02_InitialData.sql
    ├── 06_EnhanceKpiScheduling.sql
    ├── 07_NewKpiTypeStoredProcedures.sql
    ├── PerformanceOptimizations.sql
    └── [other legacy scripts]
```

## 🚀 Quick Start

### **1. Initial Database Setup**

For a fresh MonitoringGrid installation:

```sql
-- Run the complete setup script
sqlcmd -S your-server -d master -i "Setup/00_Complete_Database_Setup.sql"
```

This script will:
- Create the PopAI database
- Set up all schemas (monitoring, auth, stats)
- Create all core tables with modern Indicator terminology
- Insert default data (schedulers, contacts)
- Configure optimal database settings

### **2. Performance Optimization**

After initial setup, run the performance optimization:

```sql
-- Optimize database performance
sqlcmd -S your-server -d PopAI -i "Performance/PerformanceOptimization.sql"
```

### **3. Entity Framework Migrations**

If using Entity Framework migrations:

```bash
# Apply any pending migrations
dotnet ef database update --project MonitoringGrid.Infrastructure
```
