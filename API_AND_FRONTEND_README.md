# Monitoring Grid - API & Frontend

This document describes the complete Monitoring Grid solution with API and React frontend.

## Architecture Overview

The solution consists of three main components:

1. **Worker Service** (`MonitoringGrid.csproj`) - Background service for KPI monitoring
2. **Web API** (`MonitoringGrid.Api`) - RESTful API for management and data access
3. **React Frontend** (`MonitoringGrid.Frontend`) - Modern web interface

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

## API Endpoints

### KPI Management
- `GET /api/kpi` - Get all KPIs with filtering
- `GET /api/kpi/{id}` - Get KPI by ID
- `POST /api/kpi` - Create new KPI
- `PUT /api/kpi/{id}` - Update KPI
- `DELETE /api/kpi/{id}` - Delete KPI
- `POST /api/kpi/{id}/execute` - Execute KPI manually
- `GET /api/kpi/dashboard` - Get KPI dashboard data
- `GET /api/kpi/{id}/metrics` - Get KPI metrics and trends
- `POST /api/kpi/bulk` - Bulk operations on KPIs

### Contact Management
- `GET /api/contact` - Get all contacts
- `GET /api/contact/{id}` - Get contact by ID
- `POST /api/contact` - Create new contact
- `PUT /api/contact/{id}` - Update contact
- `DELETE /api/contact/{id}` - Delete contact
- `POST /api/contact/{id}/assign` - Assign contact to KPIs
- `POST /api/contact/bulk` - Bulk operations on contacts

### Alert Management
- `GET /api/alert` - Get alerts with filtering and pagination
- `GET /api/alert/{id}` - Get alert by ID
- `POST /api/alert/{id}/resolve` - Resolve alert
- `POST /api/alert/resolve-bulk` - Bulk resolve alerts
- `GET /api/alert/statistics` - Get alert statistics
- `GET /api/alert/dashboard` - Get alert dashboard
- `POST /api/alert/manual` - Send manual alert

### System
- `GET /health` - Health check endpoint
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
