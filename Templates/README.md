# Enterprise Application Template Generator

This template generator creates a complete enterprise-grade application based on the proven MonitoringGrid architecture, featuring Clean Architecture, CQRS, React frontend, and comprehensive security.

## 🏗️ Architecture Overview

The generated application follows Clean Architecture principles with:

- **Core Layer**: Domain entities, interfaces, and business logic
- **Infrastructure Layer**: Data access, external services, and implementations
- **API Layer**: Controllers, CQRS handlers, and web concerns
- **Frontend Layer**: React + TypeScript with Material-UI
- **Worker Layer**: Background services and scheduled tasks

## 🚀 Features Included

### Backend (.NET 8)
- ✅ Clean Architecture with DDD principles
- ✅ CQRS with MediatR pattern
- ✅ Result<T> pattern for error handling
- ✅ Entity Framework Core with SQL Server
- ✅ JWT Authentication & Authorization
- ✅ Role-based access control (RBAC)
- ✅ Comprehensive security middleware
- ✅ API versioning and documentation
- ✅ Structured logging with Serilog
- ✅ Health checks and monitoring
- ✅ Background worker services
- ✅ Unit and integration testing

### Frontend (React 18 + TypeScript)
- ✅ Modern React with TypeScript
- ✅ Material-UI component library
- ✅ TanStack Query for data fetching
- ✅ React Router for navigation
- ✅ Authentication context and guards
- ✅ Responsive design system
- ✅ Real-time updates with SignalR
- ✅ Internationalization (i18n)
- ✅ Testing with Vitest and Testing Library

### Infrastructure & DevOps
- ✅ Docker containerization
- ✅ Docker Compose for development
- ✅ SQL Server database setup
- ✅ Redis caching
- ✅ Prometheus metrics
- ✅ Grafana dashboards
- ✅ ELK stack for logging
- ✅ Kubernetes deployment manifests

## 📦 Installation

```bash
# Install the template
dotnet new install EnterpriseApp.Templates

# Create a new application
dotnet new enterprise-app --name "MyCompany.CRM" --domain "Customer" --output "./MyApp"

# Navigate to the created application
cd MyApp

# Run the setup script
./setup.ps1
```

## 🛠️ Template Parameters

| Parameter | Description | Default | Example |
|-----------|-------------|---------|---------|
| `--name` | Application name and namespace | `MyApp` | `Contoso.CRM` |
| `--domain` | Primary domain entity | `Item` | `Customer`, `Product`, `Order` |
| `--database` | Database name | `{name}DB` | `ContosoCRM` |
| `--port` | API port | `5000` | `8080` |
| `--frontend-port` | Frontend port | `3000` | `3001` |
| `--enable-auth` | Include authentication | `true` | `false` |
| `--enable-worker` | Include worker service | `true` | `false` |
| `--enable-docker` | Include Docker setup | `true` | `false` |

## 📁 Generated Project Structure

```
MyApp/
├── src/
│   ├── MyApp.Core/              # Domain layer
│   ├── MyApp.Infrastructure/    # Infrastructure layer
│   ├── MyApp.Api/              # API layer
│   ├── MyApp.Worker/           # Background services
│   └── MyApp.Frontend/         # React frontend
├── tests/
│   ├── MyApp.Core.Tests/       # Unit tests
│   ├── MyApp.Api.Tests/        # Integration tests
│   └── MyApp.Frontend.Tests/   # Frontend tests
├── docs/                       # Documentation
├── scripts/                    # Setup and deployment scripts
├── docker/                     # Docker configuration
├── k8s/                        # Kubernetes manifests
├── Database/                   # Database scripts
├── docker-compose.yml          # Development environment
├── MyApp.sln                   # Solution file
└── README.md                   # Project documentation
```

## 🎯 Quick Start Examples

### Basic CRM Application
```bash
dotnet new enterprise-app \
  --name "Contoso.CRM" \
  --domain "Customer" \
  --database "ContosoCRM" \
  --output "./ContosoCRM"
```

### E-commerce Platform
```bash
dotnet new enterprise-app \
  --name "ShopApp.Platform" \
  --domain "Product" \
  --database "ShopAppDB" \
  --port 8080 \
  --output "./ShopApp"
```

### Project Management System
```bash
dotnet new enterprise-app \
  --name "ProjectHub.Core" \
  --domain "Project" \
  --database "ProjectHubDB" \
  --enable-worker true \
  --output "./ProjectHub"
```

## 🔧 Customization

After generation, you can customize:

1. **Domain Entities**: Modify entities in `{Name}.Core/Entities/`
2. **Business Logic**: Add domain services in `{Name}.Core/Services/`
3. **API Endpoints**: Create CQRS handlers in `{Name}.Api/CQRS/`
4. **Frontend Components**: Add React components in `{Name}.Frontend/src/components/`
5. **Database Schema**: Update Entity Framework configurations

## 📚 Documentation

- [Architecture Guide](./docs/ARCHITECTURE.md)
- [Development Setup](./docs/DEVELOPMENT.md)
- [Deployment Guide](./docs/DEPLOYMENT.md)
- [Security Guide](./docs/SECURITY.md)
- [Testing Guide](./docs/TESTING.md)
- [API Documentation](./docs/API.md)

## 🤝 Contributing

This template is based on the proven MonitoringGrid architecture. Contributions are welcome!

## 📄 License

MIT License - see [LICENSE](./LICENSE) for details.
