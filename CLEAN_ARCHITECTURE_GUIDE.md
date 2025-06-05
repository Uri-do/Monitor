# Monitoring Grid - Clean Architecture Implementation

This document describes the Clean Architecture refactoring of the Monitoring Grid solution, following Domain-Driven Design (DDD) principles.

## Architecture Overview

The solution has been restructured into the following projects:

```
MonitoringGrid.Core/              # Domain Layer (Business Logic)
├── Entities/                    # Domain entities with business logic
├── Interfaces/                  # Service contracts and repository interfaces
├── Services/                    # Domain services
└── Models/                      # Domain models and value objects

MonitoringGrid.Infrastructure/    # Infrastructure Layer (Data & External Services)
├── Data/                        # Entity Framework DbContext and configurations
├── Services/                    # Service implementations
├── Repositories/                # Data access implementations
└── Migrations/                  # EF migrations (when generated)

MonitoringGrid/                   # Application Layer (Worker Service)
├── Program.cs                   # Application entry point
├── MonitoringWorker.cs          # Background service orchestration
└── Configuration/               # Application-specific configuration

MonitoringGrid.Api/              # Presentation Layer (Web API)
├── Controllers/                 # API endpoints
├── DTOs/                       # Data transfer objects
├── Mapping/                    # AutoMapper profiles
└── Program.cs                  # API startup configuration

MonitoringGrid.Frontend/         # Presentation Layer (React SPA)
├── src/components/             # UI components
├── src/pages/                  # Page components
├── src/services/               # API client services
└── src/types/                  # TypeScript type definitions
```

## Clean Architecture Principles Applied

### 1. **Dependency Inversion**
- Core layer has no dependencies on external frameworks
- Infrastructure layer depends on Core layer
- Application and Presentation layers depend on both Core and Infrastructure
- Dependencies point inward toward the domain

### 2. **Separation of Concerns**
- **Domain Logic**: Isolated in Core project
- **Data Access**: Contained in Infrastructure project
- **Business Rules**: Enforced through domain entities and services
- **External Services**: Abstracted through interfaces

### 3. **Testability**
- Domain logic can be unit tested without external dependencies
- Services are injected through interfaces
- Repository pattern enables easy mocking
- Clear separation enables focused testing

## Core Layer (MonitoringGrid.Core)

### Domain Entities
Rich domain entities with business logic:

- **KPI**: Contains validation, due date calculation, cooldown logic
- **Contact**: Email/SMS validation, contact method checking
- **AlertLog**: Severity calculation, resolution logic
- **HistoricalData**: Time-based data operations
- **Config**: Type-safe configuration value retrieval
- **SystemStatus**: Health monitoring and heartbeat logic

### Domain Interfaces
Service contracts that define business operations:

- **IKpiExecutionService**: KPI execution and validation
- **IAlertService**: Alert management and notification
- **IEmailService**: Email notification abstraction
- **ISmsService**: SMS notification abstraction
- **IRepository<T>**: Generic data access pattern

### Domain Services
Business logic that doesn't belong to a single entity:

- **KpiDomainService**: KPI business operations, statistics, validation

### Domain Models
Value objects and data transfer models:

- **KpiExecutionResult**: Execution outcome with business methods
- **AlertResult**: Alert sending result with summary logic
- **MonitoringConfiguration**: System configuration with validation
- **EmailConfiguration**: Email settings with validation

## Infrastructure Layer (MonitoringGrid.Infrastructure)

### Data Access
Entity Framework implementation with proper configuration:

- **MonitoringContext**: DbContext with entity configurations
- **Entity Configurations**: Fluent API configurations for each entity
- **Repository<T>**: Generic repository implementation
- **Migrations**: Database schema versioning (when generated)

### Service Implementations
Concrete implementations of domain interfaces:

- **KpiExecutionService**: SQL Server stored procedure execution
- **EmailService**: SMTP email sending with retry logic
- **SmsService**: SMS via email gateway (to be implemented)
- **AlertService**: Alert coordination and logging (to be implemented)

### External Dependencies
Infrastructure concerns:

- **Database connections**: SQL Server connectivity
- **SMTP configuration**: Email server setup
- **Retry policies**: Polly integration for resilience
- **Logging**: Structured logging integration

## Benefits of This Architecture

### 1. **Maintainability**
- Clear separation of concerns
- Easy to locate and modify business logic
- Reduced coupling between layers
- Consistent patterns throughout the solution

### 2. **Testability**
- Domain logic can be unit tested in isolation
- Infrastructure can be mocked for testing
- Clear interfaces enable dependency injection
- Business rules are easily verifiable

### 3. **Flexibility**
- Easy to swap implementations (e.g., different databases)
- New features can be added without affecting existing code
- External services can be replaced without domain changes
- Multiple presentation layers can share the same core

### 4. **Scalability**
- Clear boundaries enable team collaboration
- Microservices extraction is simplified
- Performance optimizations can be targeted
- Caching strategies can be implemented at appropriate layers

## Migration Guide

### From Old Structure to Clean Architecture

1. **Domain Entities**: Moved from `Models/` to `Core/Entities/`
2. **Business Logic**: Extracted from services to domain entities
3. **Data Access**: Moved from root to `Infrastructure/Data/`
4. **Service Interfaces**: Created in `Core/Interfaces/`
5. **Service Implementations**: Moved to `Infrastructure/Services/`

### Breaking Changes
- Namespace changes require using statement updates
- Service registration needs to be updated in DI container
- Some business logic moved from services to entities
- Configuration classes moved to Core layer

## Development Workflow

### Adding New Features

1. **Define Domain Model**: Add entities and value objects to Core
2. **Create Interfaces**: Define service contracts in Core/Interfaces
3. **Implement Business Logic**: Add domain services and entity methods
4. **Create Infrastructure**: Implement data access and external services
5. **Add Presentation**: Create API endpoints and UI components
6. **Write Tests**: Unit test domain logic, integration test infrastructure

### Testing Strategy

1. **Unit Tests**: Test domain entities and services in isolation
2. **Integration Tests**: Test infrastructure implementations
3. **API Tests**: Test presentation layer endpoints
4. **End-to-End Tests**: Test complete user workflows

## Configuration and Dependency Injection

### Service Registration Pattern

```csharp
// Core services (domain)
services.AddScoped<KpiDomainService>();

// Infrastructure services
services.AddScoped<IRepository<KPI>, Repository<KPI>>();
services.AddScoped<IKpiExecutionService, KpiExecutionService>();
services.AddScoped<IEmailService, EmailService>();
services.AddScoped<IAlertService, AlertService>();

// Infrastructure data
services.AddDbContext<MonitoringContext>(options =>
    options.UseSqlServer(connectionString));
```

### Configuration Binding

```csharp
// Domain configuration
services.Configure<MonitoringConfiguration>(
    configuration.GetSection("Monitoring"));

// Infrastructure configuration  
services.Configure<EmailConfiguration>(
    configuration.GetSection("Email"));
```

## Future Enhancements

### Planned Improvements

1. **CQRS Implementation**: Separate read and write models
2. **Event Sourcing**: Track domain events for audit trails
3. **Mediator Pattern**: Decouple request handling
4. **Domain Events**: Publish events for cross-cutting concerns
5. **Specification Pattern**: Flexible query building
6. **Unit of Work**: Transaction management across repositories

### Microservices Readiness

The clean architecture makes the solution ready for microservices extraction:

- **KPI Service**: Core KPI execution and management
- **Alert Service**: Notification and alert management
- **Contact Service**: Contact and communication management
- **Analytics Service**: Reporting and data analysis

## Best Practices

### Domain Layer
- Keep entities focused on business logic
- Use value objects for complex data types
- Implement domain services for multi-entity operations
- Avoid infrastructure dependencies

### Infrastructure Layer
- Implement interfaces defined in Core
- Handle all external dependencies
- Use repository pattern for data access
- Implement proper error handling and logging

### Application Layer
- Orchestrate domain and infrastructure services
- Handle cross-cutting concerns (logging, caching)
- Manage application lifecycle
- Configure dependency injection

### Presentation Layer
- Keep controllers thin
- Use DTOs for data transfer
- Implement proper validation
- Handle HTTP concerns only

This Clean Architecture implementation provides a solid foundation for the Monitoring Grid system, enabling maintainable, testable, and scalable code that follows industry best practices.
