# Monitoring Grid - API & Frontend

This document describes the complete Monitoring Grid solution with API and React frontend.

## Architecture Overview

The solution consists of three main components:

1. **Worker Service** (`MonitoringGrid.csproj`) - Background service for KPI monitoring
2. **Web API** (`MonitoringGrid.Api`) - RESTful API for management and data access
3. **React Frontend** (`MonitoringGrid.Frontend`) - Modern web interface

## 🏗️ Architectural Transformation

The MonitoringGrid API has undergone a major architectural transformation, consolidating from **18 fragmented controllers** to **4 domain-driven controllers**:

### Before: Fragmented Architecture (18 Controllers)
- Multiple overlapping controllers (KpiV2, KpiV3, OptimizedKpi, etc.)
- Scattered functionality across many files
- Inconsistent patterns and versioning
- High maintenance overhead

### After: Clean Domain-Driven Architecture (4 Controllers)

| Controller | Domain | Responsibilities |
|------------|--------|------------------|
| **🎯 KpiController** | KPI Management | Core KPI operations, alerts, contacts, execution history, analytics |
| **🔐 SecurityController** | Security & Auth | Authentication, authorization, user management, audit trails |
| **🔄 RealtimeController** | Real-time Ops | Live monitoring, SignalR operations, real-time dashboard |
| **⚙️ WorkerController** | Background Tasks | Worker service management, scheduling, background operations |

### Benefits Achieved:
- ✅ **78% reduction** in controller count (18 → 4)
- ✅ **Domain-driven design** with clear boundaries
- ✅ **Consistent API patterns** across all endpoints
- ✅ **Easier maintenance** and testing
- ✅ **Better developer experience** with logical grouping

## Project Structure

```
MonitoringGrid/
├── Database/                          # Database scripts
│   ├── 00_CreateDatabase.sql         # PopAI database creation
│   ├── 01_CreateSchema.sql           # Monitoring schema
│   ├── 02_InitialData.sql            # Initial configuration
│   └── 03_StoredProcedures.sql       # KPI monitoring procedures
├── MonitoringGrid.csproj             # Worker Service
├── MonitoringGrid.Api/               # Web API
│   ├── Controllers/                  # API controllers
│   ├── DTOs/                        # Data transfer objects
│   ├── Mapping/                     # AutoMapper profiles
│   └── Program.cs                   # API startup
└── MonitoringGrid.Frontend/         # React Frontend
    ├── src/
    │   ├── components/              # Reusable components
    │   ├── pages/                   # Page components
    │   ├── services/                # API services
    │   ├── types/                   # TypeScript types
    │   └── App.tsx                  # Main app component
    ├── package.json                 # Dependencies
    └── vite.config.ts              # Build configuration
```

## API Endpoints - Consolidated Architecture

> **Note**: The API has been restructured into a clean, domain-driven architecture with 4 consolidated controllers.

### 🎯 Indicator Management Hub (`/api/indicator/*`)
**Core Indicator Operations:**
- `GET /api/indicator` - Get all Indicators with filtering
- `GET /api/indicator/{id}` - Get Indicator by ID
- `POST /api/indicator` - Create new Indicator
- `PUT /api/indicator/{id}` - Update Indicator
- `DELETE /api/indicator/{id}` - Delete Indicator
- `POST /api/indicator/{id}/execute` - Execute Indicator manually
- `GET /api/indicator/dashboard` - Get Indicator dashboard data
- `GET /api/indicator/{id}/metrics` - Get Indicator metrics and trends
- `POST /api/indicator/bulk` - Bulk operations on Indicators

**Alert Management (KPI-related):**
- `GET /api/kpi/alerts` - Get KPI alerts with filtering
- `GET /api/kpi/alerts/{id}` - Get specific alert
- `POST /api/kpi/alerts/{id}/resolve` - Resolve alert
- `POST /api/kpi/alerts/resolve-bulk` - Bulk resolve alerts
- `GET /api/kpi/alerts/statistics` - Get alert statistics
- `GET /api/kpi/alerts/dashboard` - Get alert dashboard

**Contact Management (Notification contacts):**
- `GET /api/v{version}/kpi/contacts` - Get notification contacts
- `GET /api/v{version}/kpi/contacts/{id}` - Get contact by ID
- `POST /api/v{version}/kpi/contacts` - Create new contact
- `PUT /api/v{version}/kpi/contacts/{id}` - Update contact
- `DELETE /api/v{version}/kpi/contacts/{id}` - Delete contact
- `POST /api/v{version}/kpi/contacts/{id}/assign` - Assign contact to KPIs
- `POST /api/v{version}/kpi/contacts/bulk` - Bulk contact operations

**Execution History & Analytics:**
- `GET /api/kpi/execution-history` - Get execution history
- `GET /api/kpi/execution-stats` - Get execution statistics
- `GET /api/kpi/{id}/analytics` - Get KPI performance analytics
- `GET /api/kpi/analytics/system` - Get system analytics
- `GET /api/kpi/health` - Get system health

### 🔐 Security Management Hub (`/api/security/*`)
**Authentication:**
- `POST /api/security/auth/login` - User authentication
- `POST /api/security/auth/register` - User registration
- `POST /api/security/auth/refresh` - Refresh JWT token

**User & Role Management:**
- `GET /api/security/users` - Get all users
- `PUT /api/security/users/{id}/roles` - Update user roles
- `GET /api/security/roles` - Get all roles
- `GET /api/security/permissions` - Get all permissions

**Security Configuration:**
- `GET /api/security/config` - Get security configuration
- `PUT /api/security/config` - Update security configuration

**Security Events & Audit:**
- `GET /api/security/events` - Get security events
- `GET /api/security/events/user/{id}` - Get user security events

### 🔄 Real-time Operations Hub (`/api/realtime/*`)
- `GET /api/realtime/status` - Get real-time system status
- `POST /api/realtime/execute/{id}` - Execute KPI in real-time
- `GET /api/realtime/dashboard` - Get live dashboard data
- `GET /api/realtime/connection-info` - Get SignalR connection info
- `POST /api/realtime/test-connection` - Test SignalR connection

### ⚙️ Worker Management Hub (`/api/worker/*`)
- `GET /api/worker/status` - Get worker service status
- `POST /api/worker/start` - Start worker service
- `POST /api/worker/stop` - Stop worker service
- `POST /api/worker/restart` - Restart worker service
- `GET /api/worker/logs` - Get worker logs

### System
- `GET /health` - Health check endpoint (unversioned)
- `GET /api/info` - API information

## Setup Instructions

### 1. Database Setup

```bash
# Create PopAI database
sqlcmd -S 192.168.166.11,1433 -U saturn -P XXXXXXXX -i Database/00_CreateDatabase.sql

# Create monitoring schema
sqlcmd -S 192.168.166.11,1433 -d PopAI -U saturn -P XXXXXXXX -i Database/01_CreateSchema.sql

# Insert initial data
sqlcmd -S 192.168.166.11,1433 -d PopAI -U saturn -P XXXXXXXX -i Database/02_InitialData.sql

# Create stored procedures
sqlcmd -S 192.168.166.11,1433 -d PopAI -U saturn -P XXXXXXXX -i Database/03_StoredProcedures.sql
```

### 2. Worker Service

```bash
# Build and run the worker service
dotnet build MonitoringGrid.csproj
dotnet run --project MonitoringGrid.csproj
```

### 3. Web API

```bash
# Build and run the API
cd MonitoringGrid.Api
dotnet restore
dotnet build
dotnet run

# API will be available at:
# - HTTPS: https://localhost:7000
# - HTTP: http://localhost:5000
# - Swagger UI: https://localhost:7000 (in development)
```

### 4. React Frontend

```bash
# Install dependencies and start development server
cd MonitoringGrid.Frontend
npm install
npm run dev

# Frontend will be available at:
# - http://localhost:3000
```

## Configuration

### API Configuration (`MonitoringGrid.Api/appsettings.json`)

```json
{
  "ConnectionStrings": {
    "MonitoringGrid": "data source=192.168.166.11,1433;initial catalog=PopAI;user id=saturn;password=XXXXXXXX;...",
    "MainDatabase": "data source=192.168.166.11,1433;initial catalog=ProgressPlayDBTest;user id=saturn;password=XXXXXXXX;..."
  },
  "Monitoring": {
    "EnableSms": true,
    "EnableEmail": true,
    "AdminEmail": "admin@example.com"
  },
  "Email": {
    "SmtpServer": "smtp.example.com",
    "SmtpPort": 587,
    "Username": "monitoring@example.com",
    "Password": "your-password"
  }
}
```

### Frontend Configuration (`MonitoringGrid.Frontend/vite.config.ts`)

The frontend is configured to proxy API requests to the backend:

```typescript
export default defineConfig({
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: 'https://localhost:7000',
        changeOrigin: true,
        secure: false
      }
    }
  }
})
```

## Features

### Dashboard
- Real-time KPI status overview
- Alert summary and trends
- System health monitoring
- Quick access to due KPIs and recent alerts

### KPI Management
- Create, edit, and delete KPIs
- Configure monitoring parameters
- Assign contacts for notifications
- Manual execution and testing
- Performance metrics and trends

### Contact Management
- Manage notification contacts
- Email and SMS configuration
- KPI assignment management
- Contact validation

### Alert Management
- View and filter alerts
- Resolve alerts with notes
- Bulk operations
- Alert statistics and trends

### Analytics (Planned)
- Historical trend analysis
- Performance reporting
- Custom dashboards
- Data export capabilities

## Technology Stack

### Backend
- **.NET 8** - Modern C# framework
- **Entity Framework Core** - ORM for database access
- **AutoMapper** - Object mapping
- **Serilog** - Structured logging
- **Swagger/OpenAPI** - API documentation
- **SQL Server** - Database

### Frontend
- **React 18** - Modern UI framework
- **TypeScript** - Type-safe JavaScript
- **Material-UI (MUI)** - Component library
- **React Query** - Data fetching and caching
- **React Router** - Client-side routing
- **Vite** - Fast build tool
- **Recharts** - Data visualization

## Development

### Running in Development

1. **Start the API**:
   ```bash
   cd MonitoringGrid.Api
   dotnet run
   ```

2. **Start the Frontend**:
   ```bash
   cd MonitoringGrid.Frontend
   npm run dev
   ```

3. **Start the Worker Service** (optional):
   ```bash
   dotnet run --project MonitoringGrid.csproj
   ```

### API Documentation

When running in development mode, Swagger UI is available at the API root URL (https://localhost:7000).

### Frontend Development

The React frontend includes:
- Hot module replacement for fast development
- TypeScript for type safety
- ESLint for code quality
- Material-UI for consistent design

## Deployment

### Production Deployment

1. **Build the API**:
   ```bash
   cd MonitoringGrid.Api
   dotnet publish -c Release -o ./publish
   ```

2. **Build the Frontend**:
   ```bash
   cd MonitoringGrid.Frontend
   npm run build
   ```

3. **Deploy using the PowerShell script**:
   ```powershell
   .\Scripts\Deploy.ps1 -MonitoringConnectionString "..." -MainConnectionString "..."
   ```

### Docker Deployment

Both API and frontend can be containerized using the provided Dockerfile and docker-compose.yml.

## Security Considerations

- API uses CORS configuration for frontend access
- Database connections use SQL Server authentication
- Sensitive configuration should use Azure Key Vault or similar
- HTTPS is enforced in production

## Monitoring and Health Checks

- API includes health check endpoints
- System status is displayed in the frontend
- Comprehensive logging with Serilog
- Real-time status updates in the UI

## Future Enhancements

- User authentication and authorization
- Role-based access control
- Advanced analytics and reporting
- Mobile-responsive design improvements
- Real-time notifications via SignalR
- Custom dashboard creation
- Data export and import capabilities
