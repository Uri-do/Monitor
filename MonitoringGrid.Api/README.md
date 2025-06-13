# MonitoringGrid.Api

RESTful API service for the MonitoringGrid system, providing comprehensive monitoring, alerting, and management capabilities.

## 🏗️ Architecture

This project follows **Clean Architecture** principles and implements the API layer that exposes the Core domain functionality.

### Key Responsibilities:
- **RESTful API**: HTTP endpoints for all system operations
- **Real-time Communication**: SignalR hubs for live updates
- **Authentication & Authorization**: JWT-based security
- **API Documentation**: Swagger/OpenAPI integration
- **Observability**: Metrics, tracing, and structured logging
- **Worker Management**: Background service coordination

## 📁 Project Structure

```
MonitoringGrid.Api/
├── Controllers/                 # API controllers
│   ├── IndicatorController.cs   # Indicator management endpoints
│   ├── SecurityController.cs    # Authentication & authorization
│   ├── WorkerController.cs      # Worker service management
│   ├── AlertController.cs       # Alert management
│   └── MonitorStatisticsController.cs # Statistics endpoints
├── DTOs/                        # Data transfer objects
├── Middleware/                  # Custom middleware components
├── Services/                    # API-specific services
├── Hubs/                        # SignalR hubs
├── Security/                    # Security components
├── Observability/               # Monitoring & tracing
├── Documentation/               # API documentation services
├── Events/                      # Domain event handling
├── Validation/                  # Input validation
└── Extensions/                  # Extension methods
```

## 🚀 Key Features

### API Endpoints
- **Indicators**: CRUD operations, execution, scheduling
- **Alerts**: Alert management and history
- **Statistics**: Performance metrics and analytics
- **Security**: Authentication, user management, roles
- **Worker**: Background service control and monitoring

### Real-time Features
- **Live Dashboard Updates**: Real-time indicator status
- **Alert Notifications**: Instant alert delivery
- **Worker Status**: Live worker process monitoring
- **Performance Metrics**: Real-time system health

### Security Features
- **JWT Authentication**: Token-based security
- **Role-based Authorization**: Granular permissions
- **Security Event Logging**: Comprehensive audit trail
- **Rate Limiting**: API protection and throttling
- **CORS Support**: Cross-origin resource sharing

### Observability
- **Structured Logging**: Comprehensive logging with correlation IDs
- **Distributed Tracing**: Request tracing across services
- **Performance Metrics**: API performance monitoring
- **Health Checks**: System health endpoints

## 🔧 Configuration

### Connection Strings
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=192.168.166.11,1433;Database=PopAI;User Id=conexusadmin;Password=***;TrustServerCertificate=true;",
    "ProgressPlayDB": "Server=192.168.166.11,1433;Database=ProgressPlayDBTest;User Id=conexusadmin;Password=***;TrustServerCertificate=true;"
  }
}
```

### JWT Configuration
```json
{
  "Jwt": {
    "SecretKey": "your-secret-key",
    "Issuer": "MonitoringGrid.Api",
    "Audience": "MonitoringGrid.Client",
    "ExpiryMinutes": 60
  }
}
```

### SignalR Configuration
```json
{
  "SignalR": {
    "EnableDetailedErrors": true,
    "KeepAliveInterval": "00:00:15",
    "ClientTimeoutInterval": "00:01:00"
  }
}
```

## 📊 API Documentation

### Swagger/OpenAPI
- **Development**: Available at `/swagger`
- **Production**: Configurable via settings
- **Interactive Documentation**: Full API exploration
- **Schema Generation**: Automatic model documentation

### Key Endpoints

#### Indicators
```
GET    /api/indicators              # List all indicators
POST   /api/indicators              # Create new indicator
GET    /api/indicators/{id}         # Get indicator details
PUT    /api/indicators/{id}         # Update indicator
DELETE /api/indicators/{id}         # Delete indicator
POST   /api/indicators/{id}/execute # Execute indicator manually
```

#### Worker Management
```
GET    /api/worker/status           # Get worker status
POST   /api/worker/start            # Start worker service
POST   /api/worker/stop             # Stop worker service
POST   /api/worker/restart          # Restart worker service
```

#### Security
```
POST   /api/auth/login              # User authentication
POST   /api/auth/register           # User registration
POST   /api/auth/refresh            # Token refresh
GET    /api/auth/profile            # User profile
```

## 🔄 Real-time Communication

### SignalR Hubs
- **MonitoringHub**: Real-time dashboard updates
- **AlertHub**: Live alert notifications
- **WorkerHub**: Worker status updates

### Connection Management
```javascript
// Connect to monitoring hub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/monitoring-hub")
    .build();

// Listen for indicator updates
connection.on("IndicatorStatusUpdate", (data) => {
    // Handle real-time updates
});
```

## 🛡️ Security

### Authentication Flow
1. **Login**: User provides credentials
2. **Token Generation**: JWT token issued
3. **Token Validation**: Middleware validates requests
4. **Authorization**: Role-based access control

### Security Features
- **Password Hashing**: Secure password storage
- **Token Expiry**: Automatic token invalidation
- **Replay Protection**: Token reuse prevention
- **Audit Logging**: Security event tracking

## 📈 Monitoring & Observability

### Metrics
- **Request Metrics**: Response times, status codes
- **Business Metrics**: Indicator execution counts
- **System Metrics**: Memory, CPU, database connections

### Tracing
- **Request Tracing**: End-to-end request tracking
- **Database Tracing**: SQL query performance
- **External Service Tracing**: Third-party API calls

### Logging
- **Structured Logging**: JSON-formatted logs
- **Correlation IDs**: Request correlation across services
- **Log Levels**: Configurable logging verbosity

## 🚀 Deployment

### Development
```bash
dotnet run --project MonitoringGrid.Api
```

### Production
```bash
dotnet publish -c Release
# Deploy to IIS, Docker, or cloud platform
```

### Docker Support
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY . /app
WORKDIR /app
EXPOSE 80
ENTRYPOINT ["dotnet", "MonitoringGrid.Api.dll"]
```

## 🧪 Testing

### Unit Tests
```bash
dotnet test MonitoringGrid.Api.Tests
```

### Integration Tests
```bash
dotnet test MonitoringGrid.Tests --filter Category=Integration
```

### API Testing
- **Swagger UI**: Interactive testing
- **Postman Collection**: Automated testing
- **Health Checks**: System validation

## 📋 Recent Improvements

### Phase 3: API Project Cleanup ✅
- **Legacy KPI References**: Updated to Indicator terminology
- **Async Method Warnings**: Fixed 28 async/await warnings
- **Nullable Reference Warnings**: Resolved null reference issues
- **Observability Updates**: Renamed KpiActivitySource to IndicatorActivitySource
- **Controller Updates**: Updated WorkerController endpoint naming
- **Build Status**: Reduced warnings from 52 to 24

### Key Changes Made:
1. **IndicatorActivitySource.cs**: Renamed from KpiActivitySource, updated all references
2. **SecurityEventService.cs**: Fixed 5 async method warnings
3. **EnhancedJwtMiddleware.cs**: Fixed nullable reference warning
4. **EnhancedExceptionHandlingMiddleware.cs**: Fixed nullable assignment warning
5. **AdvancedCachingService.cs**: Fixed null reference return warning
6. **DomainEventIntegrationService.cs**: Fixed async method warning
7. **WorkerController.cs**: Updated execute-kpi endpoint to execute-indicator

## 🔗 Dependencies

### Core Dependencies
- **ASP.NET Core 8.0**: Web framework
- **Entity Framework Core**: Data access
- **SignalR**: Real-time communication
- **Swagger/OpenAPI**: API documentation

### Monitoring Dependencies
- **Serilog**: Structured logging
- **OpenTelemetry**: Distributed tracing
- **Health Checks**: System monitoring

### Security Dependencies
- **JWT Bearer**: Authentication
- **BCrypt**: Password hashing
- **CORS**: Cross-origin support

## 📞 Support

For issues, questions, or contributions, please refer to the main project documentation or contact the development team.
