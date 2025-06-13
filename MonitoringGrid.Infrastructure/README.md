# MonitoringGrid.Infrastructure

Infrastructure layer implementing data access, external services, and cross-cutting concerns for the MonitoringGrid system.

## 🏗️ Architecture

This project follows **Clean Architecture** principles and implements the infrastructure layer that supports the Core domain.

### Key Responsibilities:
- **Data Access**: Entity Framework Core implementation
- **External Services**: Email, SMS, and notification services
- **Database Context**: MonitoringContext with proper entity configurations
- **Service Implementations**: Concrete implementations of Core interfaces
- **Migrations**: Database schema management

## 📁 Project Structure

```
MonitoringGrid.Infrastructure/
├── Data/
│   ├── Configurations/          # EF Core entity configurations
│   └── MonitoringContext.cs     # Main database context
├── Services/                    # Service implementations
│   ├── IndicatorService.cs      # Indicator management service
│   ├── IndicatorExecutionService.cs # Indicator execution service
│   ├── EmailService.cs          # Email notification service
│   ├── SmsService.cs           # SMS notification service
│   └── MonitorStatisticsService.cs # Statistics service
├── Repositories/                # Repository implementations
├── Migrations/                  # EF Core migrations
├── Database/                    # Legacy SQL scripts (see Database/README.md)
└── Utilities/                   # Infrastructure utilities
```

## 🎯 Key Features

### Data Access Layer
- **Entity Framework Core** with SQL Server provider
- **Repository Pattern** implementation
- **Specification Pattern** for complex queries
- **Unit of Work** pattern for transaction management

### Service Layer
- **Indicator Management**: CRUD operations for performance indicators
- **Indicator Execution**: Background execution and monitoring
- **Statistics Collection**: Data aggregation and analysis
- **Notification Services**: Email and SMS alert delivery

### Database Management
- **Code-First Migrations**: Schema versioning and updates
- **Proper Indexing**: Optimized for monitoring workloads
- **Connection String Management**: Multiple database support
- **Schema Organization**: Monitoring schema separation

## 🔧 Configuration

### Database Connections
- **PopAI Database**: Main monitoring database (192.168.166.11,1433)
- **ProgressPlayDBTest**: Target monitored database
- **Connection Strings**: Configured in appsettings.json

### Entity Framework
- **Monitoring Schema**: All tables in `monitoring` schema
- **Naming Conventions**: Consistent ID naming (IndicatorID, ContactID)
- **Relationships**: Proper foreign key constraints
- **Indexes**: Performance-optimized indexing strategy

## 📊 Database Schema

### Core Tables
- `monitoring.Indicators` - Performance indicator definitions
- `monitoring.IndicatorContacts` - Indicator-contact relationships
- `monitoring.Contacts` - Contact information
- `monitoring.AlertLogs` - Alert history and tracking

### Statistics Tables
- `monitoring.Collectors` - Statistics collector configurations
- `monitoring.MonitorStatistics` - Collected statistical data

### System Tables
- `monitoring.SystemStatus` - System health monitoring
- `monitoring.Config` - System configuration values
- `monitoring.ScheduledJobs` - Background job scheduling

## 🚀 Usage

### Service Registration
```csharp
services.AddInfrastructure(configuration);
```

### Database Updates
```bash
dotnet ef database update
```

### Adding Migrations
```bash
dotnet ef migrations add MigrationName
```

## 📝 Migration History

The system has been fully migrated from legacy KPI terminology to modern Indicator terminology:

- ✅ All services use Indicator terminology
- ✅ Database schema updated to use IndicatorID (bigint)
- ✅ Foreign key relationships properly configured
- ✅ Indexes optimized for performance
- ✅ Legacy scripts documented and preserved

## 🔍 Build Status

- ✅ **Zero Warnings**: Clean build with no compiler warnings
- ✅ **Zero Errors**: All dependencies resolved
- ✅ **Async Patterns**: Proper async/await usage throughout
- ✅ **Nullable References**: Full nullable reference type support

## 🧪 Testing

- Unit tests for repository implementations
- Integration tests for database operations
- Service layer testing with mocked dependencies
- Migration testing for schema changes
