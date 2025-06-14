# 💻 Development Documentation

This section contains developer guides, setup instructions, and integration documentation for the MonitoringGrid system.

## 📋 Documents in this Section

### 🚀 **Getting Started**
- **[API & Frontend Guide](API_AND_FRONTEND_README.md)** - Complete development setup and workflow
- **[System Review](monitoring-grid-review.md)** - Comprehensive system overview and architecture

### 🔧 **Integration Guides**
- **[Worker Integration](WORKER_INTEGRATION_GUIDE.md)** - Background service integration and management
- **[Worker Project Summary](WORKER_PROJECT_SUMMARY.md)** - Detailed worker service documentation

### 🧪 **Testing & Validation**
- **[Authentication Testing](test-authentication-flow.md)** - Auth flow testing procedures
- **[KPI Monitoring Grid Development](KPI%20Monitoring%20Grid%20Development_.md)** - Development guidelines
- **[Request Title: Monitoring Grid](Request%20Title_%20Monitoring%20Grid.md)** - Project requirements and specifications

## 🛠️ Development Environment Setup

### **Prerequisites**
- .NET 8 SDK
- Node.js 18+ (for frontend)
- SQL Server (PopAI + ProgressPlayDB)
- Visual Studio 2022 or VS Code
- Git for version control

### **Quick Start**
```bash
# Clone the repository
git clone https://github.com/Uri-do/Monitor.git
cd Monitor

# Backend setup
dotnet restore
dotnet build

# Frontend setup
cd MonitoringGrid.Frontend
npm install
npm run dev

# Database setup
# See Deployment/DUAL_DATABASE_SETUP.md
```

## 🏗️ Project Structure

```
MonitoringGrid/
├── MonitoringGrid.Core/          # Domain layer
├── MonitoringGrid.Infrastructure/ # Data access & external services
├── MonitoringGrid.Api/           # Web API layer
├── MonitoringGrid.Worker/        # Background services
├── MonitoringGrid.Frontend/      # React frontend
├── MonitoringGrid.Core.Tests/    # Unit tests
├── MonitoringGrid.Api.Tests/     # Integration tests
└── docs/                         # Documentation
```

## 🔄 Development Workflow

### **1. Feature Development**
1. Create feature branch from `main`
2. Implement following Clean Architecture
3. Add comprehensive tests
4. Update documentation
5. Create pull request

### **2. Code Standards**
- Follow Clean Architecture principles
- Use CQRS pattern for API operations
- Implement Result pattern for error handling
- Write comprehensive unit tests
- Document public APIs

### **3. Testing Strategy**
- **Unit Tests**: Domain logic and business rules
- **Integration Tests**: API endpoints and database operations
- **Performance Tests**: Load testing and benchmarks
- **E2E Tests**: Complete user workflows

## 🎯 Key Development Areas

### **Backend Development**
- **Domain Models**: Rich entities with business logic
- **CQRS Handlers**: Command and query processing
- **API Controllers**: HTTP endpoint implementation
- **Background Services**: Scheduled indicator execution

### **Frontend Development**
- **React Components**: Modern functional components
- **State Management**: Zustand for global state
- **API Integration**: TanStack Query for data fetching
- **UI Components**: Material-UI design system

### **Database Development**
- **Entity Framework**: Code-first migrations
- **Dual Database**: PopAI (monitoring) + ProgressPlayDB (source)
- **Performance**: Indexing and query optimization
- **Data Integrity**: Constraints and validation

## 🔧 Development Tools

### **Backend Tools**
- **Visual Studio 2022**: Primary IDE
- **Entity Framework Tools**: Database migrations
- **Swagger**: API documentation and testing
- **xUnit**: Unit testing framework

### **Frontend Tools**
- **VS Code**: Frontend development
- **Vite**: Build tool and dev server
- **ESLint**: Code linting
- **Prettier**: Code formatting

### **Database Tools**
- **SQL Server Management Studio**: Database management
- **Entity Framework CLI**: Migration management
- **Azure Data Studio**: Cross-platform database tool

## 🚀 Deployment Process

### **Development**
```bash
# Backend
dotnet run --project MonitoringGrid.Api

# Frontend
cd MonitoringGrid.Frontend
npm run dev

# Worker (optional)
dotnet run --project MonitoringGrid.Worker
```

### **Production**
```bash
# Build and publish
dotnet publish -c Release
cd MonitoringGrid.Frontend
npm run build

# Deploy to server
# See Deployment documentation
```

## 🧪 Testing Guidelines

### **Unit Tests**
- Test domain logic in isolation
- Mock external dependencies
- Use AAA pattern (Arrange, Act, Assert)
- Aim for high code coverage

### **Integration Tests**
- Test API endpoints end-to-end
- Use test database
- Verify business workflows
- Test error scenarios

### **Performance Tests**
- Load testing with realistic data
- Memory usage monitoring
- Response time benchmarks
- Concurrent user simulation

## 📚 Learning Resources

### **Architecture**
- Clean Architecture by Robert Martin
- Domain-Driven Design by Eric Evans
- CQRS and Event Sourcing patterns

### **Technologies**
- .NET 8 documentation
- Entity Framework Core guides
- React and TypeScript tutorials
- Material-UI component library

## 🤝 Contributing

1. **Follow the development workflow**
2. **Maintain code quality standards**
3. **Write comprehensive tests**
4. **Update documentation**
5. **Review security implications**

---

**Last Updated**: June 2025  
**Development Environment**: .NET 8 + React 18  
**Status**: ✅ Active Development
