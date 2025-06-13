# MonitoringGrid.Core

This is the **Domain Layer** of the MonitoringGrid application, following Clean Architecture principles and Domain-Driven Design (DDD) patterns.

## Architecture Overview

The Core project contains the business logic and domain entities, completely independent of external concerns like databases, web frameworks, or third-party services.

## Project Structure

```
MonitoringGrid.Core/
├── Entities/           # Domain entities with business logic
├── Interfaces/         # Service contracts and repository interfaces  
├── Models/            # Domain models and value objects
├── Services/          # Domain services
└── Security/          # Authentication and authorization models
```

## Entities

Domain entities represent the core business objects with rich behavior:

### Monitoring Domain
- **Indicator**: Performance indicator configuration with validation and scheduling logic
- **AlertLog**: Alert records with severity calculation and resolution logic
- **Contact**: Contact information with validation methods
- **IndicatorContact**: Many-to-many relationship between Indicators and contacts
- **SystemStatus**: System health monitoring and heartbeat logic
- **Config**: Type-safe configuration value retrieval
- **Collector**: Statistics collector configuration for ProgressPlayDB integration
- **MonitorStatistics**: Statistical data collection and analysis

### Security Domain
- **User**: User accounts with authentication logic
- **Role**: User roles and permissions
- **Permission**: Granular permissions system
- **RefreshToken**: JWT refresh token management
- **UserPassword**: Password history and validation
- **UserRole**: User-role assignments
- **RolePermission**: Role-permission assignments

### Enterprise Features
- **AuditLog**: Compliance and security tracking
- **WebhookConfig**: External webhook configurations
- **WebhookDeliveryLog**: Webhook delivery tracking
- **ReportTemplate**: Report template definitions
- **ReportSchedule**: Scheduled report configurations
- **AlertEscalation**: Alert escalation workflows
- **AlertAcknowledgment**: Alert acknowledgment tracking
- **AlertSuppressionRule**: Alert suppression rules
- **NotificationPreferences**: User notification preferences
- **ExportJob**: Data export job tracking
- **BackupInfo**: Backup metadata and validation

## Interfaces

Service contracts that define business operations:

### Core Services
- **IIndicatorExecutionService**: Indicator execution and validation
- **IIndicatorService**: Indicator management and configuration
- **IEmailService**: Email notification abstraction
- **ISmsService**: SMS notification abstraction
- **IRepository<T>**: Generic data access pattern
- **IMonitorStatisticsService**: Statistics collection and analysis

### Integration Services
- **ISlackService**: Slack integration
- **ITeamsService**: Microsoft Teams integration
- **IWebhookService**: Generic webhook integration
- **IExternalApiService**: External API integration
- **ILdapService**: LDAP/Active Directory integration
- **ISsoService**: Single Sign-On integration

### Enterprise Services
- **IAuditService**: Audit logging
- **IReportingService**: Report generation
- **INotificationRoutingService**: Notification routing
- **IDataExportService**: Data export functionality
- **IBackupService**: Backup and restore operations

### Security Services
- **IUserService**: User management
- **ISecurityServices**: Authentication and authorization

## Models

Value objects and data transfer models:

### Core Models
- **IndicatorExecutionResult**: Indicator execution outcomes with business methods
- **AlertResult**: Alert sending results with summary logic
- **MonitoringConfiguration**: System configuration with validation

### Analytics Models
- **IndicatorTrendAnalysis**: Trend analysis results
- **IndicatorPerformanceMetrics**: Performance metrics
- **IndicatorPrediction**: Predictive analytics results
- **IndicatorCorrelationAnalysis**: Correlation analysis
- **IndicatorAnomalyDetection**: Anomaly detection results

### Integration Models
- **AlertNotificationDto**: Alert notification data
- **SlackMessage**: Slack message formatting
- **TeamsAdaptiveCard**: Teams adaptive card formatting
- **LdapUser**: LDAP user information
- **SsoAuthResult**: SSO authentication results

## Services

Domain services for complex business logic that doesn't belong to a single entity:

- **IndicatorDomainService**: Indicator business operations, statistics, validation
- **IndicatorAnalyticsService**: Advanced analytics and trend analysis
- **MonitorStatisticsService**: Statistics collection and aggregation service

## Design Principles

### Clean Architecture
- **Independence**: No dependencies on external frameworks or infrastructure
- **Testability**: Pure business logic that's easy to unit test
- **Flexibility**: Can be used with any UI, database, or external service

### Domain-Driven Design
- **Rich Domain Model**: Entities contain business logic, not just data
- **Ubiquitous Language**: Consistent terminology throughout the domain
- **Bounded Context**: Clear boundaries around related concepts

### SOLID Principles
- **Single Responsibility**: Each class has one reason to change
- **Open/Closed**: Open for extension, closed for modification
- **Liskov Substitution**: Derived classes are substitutable for base classes
- **Interface Segregation**: Clients depend only on interfaces they use
- **Dependency Inversion**: Depend on abstractions, not concretions

## Usage Guidelines

### Adding New Entities
1. Create entity in appropriate domain area
2. Add rich business methods, not just properties
3. Include proper validation attributes
4. Add navigation properties for relationships
5. Update corresponding repository interface

### Adding New Services
1. Define interface in `Interfaces/` folder
2. Keep interfaces focused and cohesive
3. Use async/await for I/O operations
4. Include proper cancellation token support
5. Add comprehensive XML documentation

### Adding New Models
1. Create in appropriate model category
2. Use value objects for complex data
3. Include validation logic where appropriate
4. Keep models immutable when possible
5. Add business methods for calculations

## Dependencies

- **Microsoft.EntityFrameworkCore**: For entity configurations
- **Microsoft.Extensions.Logging.Abstractions**: For logging interfaces
- **Microsoft.Extensions.DependencyInjection.Abstractions**: For DI interfaces
- **Microsoft.Extensions.Configuration.Abstractions**: For configuration interfaces
- **System.ComponentModel.Annotations**: For validation attributes

## Testing

The Core project should have comprehensive unit tests covering:
- Entity business logic and validation
- Domain service operations
- Model calculations and transformations
- Interface contracts and behaviors

## Contributing

When contributing to the Core project:
1. Follow existing naming conventions
2. Add comprehensive XML documentation
3. Include unit tests for new functionality
4. Ensure no dependencies on infrastructure concerns
5. Validate business rules in entities, not services
