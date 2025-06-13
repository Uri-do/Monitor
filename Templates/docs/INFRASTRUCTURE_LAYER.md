# Infrastructure Layer Template

This document describes the Infrastructure layer template that provides data access, external services, and cross-cutting concerns implementation.

## 🏗️ Architecture Overview

The Infrastructure layer implements the interfaces defined in the Core layer and provides:

- **Data Access**: Entity Framework Core with SQL Server
- **Repository Pattern**: Generic and specific repository implementations
- **Unit of Work**: Transaction management and coordination
- **Service Implementations**: Domain services, caching, email, etc.
- **Security Services**: Authentication, authorization, JWT management
- **Cross-cutting Concerns**: Logging, monitoring, health checks

## 📁 Project Structure

```
EnterpriseApp.Infrastructure/
├── Data/
│   ├── ApplicationDbContext.cs          # Main EF DbContext
│   ├── Configurations/                  # EF entity configurations
│   │   ├── DomainEntityConfiguration.cs
│   │   ├── AuditLogConfiguration.cs
│   │   ├── UserConfiguration.cs
│   │   └── RefreshTokenConfiguration.cs
│   └── Seed/                           # Data seeding
├── Repositories/
│   ├── Repository.cs                   # Generic repository
│   ├── UnitOfWork.cs                   # Unit of Work implementation
│   ├── DomainEntityRepository.cs       # Specific repository
│   └── AuditRepository.cs              # Audit repository
├── Services/
│   ├── DomainEntityService.cs          # Domain service
│   ├── AuditService.cs                 # Audit service
│   ├── EmailService.cs                 # Email service
│   ├── CacheService.cs                 # Caching services
│   └── NotificationService.cs          # Notification service
├── Security/                           # Authentication services
├── Utilities/                          # Helper utilities
└── DependencyInjection.cs             # Service registration
```

## 🗄️ Database Layer

### ApplicationDbContext

The main database context provides:

- **Entity Configuration**: Fluent API configurations for all entities
- **Automatic Auditing**: Tracks changes and generates audit logs
- **Soft Delete Support**: Handles logical deletion of entities
- **Optimistic Concurrency**: Prevents data conflicts
- **Performance Optimization**: Includes indexes and query optimization

Key Features:
```csharp
public class ApplicationDbContext : DbContext
{
    // Domain entities
    public DbSet<DomainEntity> DomainEntities { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    
    // Authentication entities (if enabled)
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    
    // Automatic audit logging on save
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        var auditEntries = GenerateAuditEntries();
        var result = await base.SaveChangesAsync(cancellationToken);
        // Save audit logs after main changes
        return result;
    }
}
```

### Entity Configurations

Each entity has a dedicated configuration class:

```csharp
public class DomainEntityConfiguration : IEntityTypeConfiguration<DomainEntity>
{
    public void Configure(EntityTypeBuilder<DomainEntity> builder)
    {
        // Table and key configuration
        builder.ToTable("DomainEntities");
        builder.HasKey(e => e.Id);
        
        // Property configurations with constraints
        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);
            
        // Indexes for performance
        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => e.Category);
        
        // Relationships
        builder.HasMany(e => e.Items)
            .WithOne(i => i.DomainEntity)
            .HasForeignKey(i => i.DomainEntityId);
    }
}
```

## 📊 Repository Pattern

### Generic Repository

Provides common CRUD operations:

```csharp
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;
    
    // Standard CRUD operations
    public virtual async Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    public virtual Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    public virtual Task RemoveAsync(T entity, CancellationToken cancellationToken = default)
    
    // Advanced querying
    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    public virtual async Task<IEnumerable<T>> GetWithIncludesAsync(params Expression<Func<T, object>>[] includes)
}
```

### Specific Repositories

Domain-specific repositories extend the generic repository:

```csharp
public class DomainEntityRepository : Repository<DomainEntity>, IDomainEntityRepository
{
    // Domain-specific methods
    public async Task<IEnumerable<DomainEntity>> GetActiveAsync(CancellationToken cancellationToken = default)
    public async Task<IEnumerable<DomainEntity>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    public async Task<IEnumerable<DomainEntity>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    
    // Advanced operations
    public async Task<DomainEntityStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    public async Task<int> BulkUpdateStatusAsync(IEnumerable<int> entityIds, DomainEntityStatus newStatus, string modifiedBy, CancellationToken cancellationToken = default)
}
```

### Unit of Work

Manages transactions and repository coordination:

```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly Dictionary<Type, object> _repositories;
    
    public IRepository<T> Repository<T>() where T : class
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
}
```

## 🔧 Service Implementations

### Domain Services

Implement business logic and coordinate between repositories:

```csharp
public class DomainEntityService : IDomainEntityService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ILogger<DomainEntityService> _logger;
    
    public async Task<DomainEntity> CreateAsync(CreateDomainEntityRequest request, string createdBy, CancellationToken cancellationToken = default)
    {
        // Validate request
        var validationResult = await ValidateCreateRequestAsync(request, cancellationToken);
        
        // Create entity
        var entity = new DomainEntity { /* ... */ };
        
        // Save in transaction with audit logging
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _unitOfWork.Repository<DomainEntity>().AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditService.LogCreationAsync(nameof(DomainEntity), entity.Id.ToString(), createdBy, cancellationToken: cancellationToken);
        }, cancellationToken);
        
        return entity;
    }
}
```

### Audit Service

Provides comprehensive audit logging:

```csharp
public class AuditService : IAuditService
{
    public async Task LogCreationAsync(string entityName, string entityId, string userId, string? username = null, string? ipAddress = null, CancellationToken cancellationToken = default)
    public async Task LogUpdateAsync(string entityName, string entityId, string userId, object? oldValues = null, object? newValues = null, string? username = null, string? ipAddress = null, CancellationToken cancellationToken = default)
    public async Task LogDeletionAsync(string entityName, string entityId, string userId, string? username = null, string? ipAddress = null, CancellationToken cancellationToken = default)
    public async Task<IEnumerable<AuditLog>> GetAuditTrailAsync(string entityName, string entityId, CancellationToken cancellationToken = default)
}
```

### Caching Services

Multiple caching implementations:

```csharp
// Memory Cache Implementation
public class MemoryCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    public Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
}

// Redis Cache Implementation
public class RedisCacheService : ICacheService
{
    // Same interface, Redis-backed implementation
}
```

### Email Service

SMTP-based email service with template support:

```csharp
public class EmailService : IEmailService
{
    public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
    public async Task<bool> SendEmailWithAttachmentsAsync(string to, string subject, string body, IEnumerable<EmailAttachment> attachments, bool isHtml = true, CancellationToken cancellationToken = default)
    public async Task<bool> SendTemplatedEmailAsync(string to, string templateName, object model, CancellationToken cancellationToken = default)
}
```

## 🔐 Security Services (if auth enabled)

### Authentication Service
- JWT token generation and validation
- User login/logout
- Password management
- Two-factor authentication support

### Authorization Service
- Role-based access control
- Permission checking
- User role management

### Password Service
- Secure password hashing (BCrypt)
- Password strength validation
- Password history tracking

## ⚙️ Dependency Injection

Comprehensive service registration:

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext(configuration);
        
        // Repositories
        services.AddRepositories();
        
        // Services
        services.AddDomainServices();
        
        // Authentication (if enabled)
        services.AddAuthenticationServices(configuration);
        
        // Caching
        services.AddCaching(configuration);
        
        return services;
    }
}
```

## 🏥 Health Checks

Built-in health checks for:
- Database connectivity
- Cache availability
- External service dependencies
- File storage access

## 📊 Performance Features

### Database Optimization
- Proper indexing strategy
- Query optimization
- Connection pooling
- Retry policies

### Caching Strategy
- Multi-level caching (Memory + Redis)
- Cache-aside pattern
- Automatic cache invalidation
- Performance monitoring

### Bulk Operations
- Bulk insert/update operations
- Batch processing
- Transaction optimization

## 🔧 Configuration

### Database Configuration
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=EnterpriseAppDB;Trusted_Connection=true;",
    "Redis": "localhost:6379"
  }
}
```

### Email Configuration
```json
{
  "EmailSettings": {
    "Enabled": true,
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "EnableSsl": true,
    "Username": "your-email@gmail.com",
    "Password": "your-password",
    "FromEmail": "noreply@yourapp.com",
    "FromName": "Your App"
  }
}
```

## 🧪 Testing Support

The Infrastructure layer is designed for testability:

- Repository interfaces for mocking
- In-memory database support for testing
- Dependency injection for service substitution
- Separate test configurations

## 📈 Monitoring & Observability

Built-in support for:
- Structured logging with Serilog
- Performance metrics collection
- Health check endpoints
- Audit trail tracking
- Error tracking and reporting

## 🚀 Getting Started

1. **Configure Database**: Update connection strings
2. **Run Migrations**: `dotnet ef database update`
3. **Configure Services**: Update appsettings.json
4. **Register Services**: Use DependencyInjection.AddInfrastructure()
5. **Seed Data**: Run initial data seeding

The Infrastructure layer provides a solid foundation for enterprise applications with production-ready features, comprehensive error handling, and excellent performance characteristics.
