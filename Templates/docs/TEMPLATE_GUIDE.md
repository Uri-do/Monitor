# Enterprise Application Template Guide

This comprehensive guide explains how to use the Enterprise Application Template to create production-ready applications based on the proven MonitoringGrid architecture.

## 🎯 Overview

The Enterprise Application Template provides a complete foundation for building modern web applications with:

- **Clean Architecture** with clear separation of concerns
- **CQRS + MediatR** for scalable command/query handling
- **Result<T> Pattern** for robust error handling
- **JWT Authentication** with role-based authorization
- **React + TypeScript** frontend with Material-UI
- **Entity Framework Core** with SQL Server
- **Docker** containerization and deployment
- **Comprehensive Testing** setup

## 🚀 Quick Start

### Prerequisites

- .NET 8 SDK
- Node.js 18+ (for frontend)
- SQL Server (local or remote)
- Docker (optional)

### Installation

1. **Install the template:**
   ```bash
   dotnet new install ./Templates
   ```

2. **Create a new application:**
   ```bash
   dotnet new enterprise-app --name "MyCompany.CRM" --domain "Customer" --output "./MyCRM"
   ```

3. **Navigate and setup:**
   ```bash
   cd MyCRM
   dotnet restore
   cd src/MyCompany.CRM.Frontend && npm install
   ```

## 📋 Template Parameters

| Parameter | Description | Default | Example |
|-----------|-------------|---------|---------|
| `--name` | Application name and namespace | `MyApp` | `Contoso.CRM` |
| `--domain` | Primary domain entity | `Item` | `Customer`, `Product` |
| `--database` | Database name | `{name}DB` | `ContosoCRM` |
| `--port` | API port | `5000` | `8080` |
| `--frontend-port` | Frontend port | `3000` | `3001` |
| `--enable-auth` | Include authentication | `true` | `false` |
| `--enable-worker` | Include worker service | `true` | `false` |
| `--enable-docker` | Include Docker setup | `true` | `false` |
| `--enable-testing` | Include test projects | `true` | `false` |

## 🏗️ Architecture Overview

### Project Structure

```
MyApp/
├── src/
│   ├── MyApp.Core/              # Domain Layer
│   │   ├── Entities/            # Domain entities
│   │   ├── Interfaces/          # Service contracts
│   │   ├── Models/              # Domain models
│   │   ├── Services/            # Domain services
│   │   └── Security/            # Auth entities (if enabled)
│   ├── MyApp.Infrastructure/    # Infrastructure Layer
│   │   ├── Data/                # EF configurations
│   │   ├── Services/            # Service implementations
│   │   └── Repositories/        # Data access
│   ├── MyApp.Api/              # API Layer
│   │   ├── Controllers/         # API controllers
│   │   ├── CQRS/               # Commands & queries
│   │   ├── Middleware/          # Custom middleware
│   │   └── Security/            # Auth middleware
│   ├── MyApp.Worker/           # Background Services
│   └── MyApp.Frontend/         # React Frontend
├── tests/                      # Test Projects
├── docs/                       # Documentation
├── docker/                     # Docker configuration
└── Database/                   # Database scripts
```

### Core Patterns

#### 1. Clean Architecture
- **Core**: Business logic, entities, interfaces
- **Infrastructure**: Data access, external services
- **API**: Controllers, middleware, configuration
- **Frontend**: React components, services, state

#### 2. CQRS with MediatR
```csharp
// Command
public class CreateCustomerCommand : ICommand<CustomerDto>
{
    public string Name { get; set; }
    public string Email { get; set; }
}

// Handler
public class CreateCustomerHandler : ICommandHandler<CreateCustomerCommand, CustomerDto>
{
    public async Task<Result<CustomerDto>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        // Business logic here
        return Result<CustomerDto>.Success(customerDto);
    }
}

// Controller
[HttpPost]
public async Task<ActionResult<CustomerDto>> CreateCustomer(CreateCustomerCommand command)
{
    var result = await _mediator.Send(command);
    return result.Match(
        onSuccess: customer => Ok(customer),
        onFailure: error => HandleError(error)
    );
}
```

#### 3. Result<T> Pattern
```csharp
// Service method
public async Task<Result<Customer>> GetCustomerAsync(int id)
{
    var customer = await _repository.GetByIdAsync(id);
    
    if (customer == null)
        return Error.NotFound("Customer", id);
    
    return Result<Customer>.Success(customer);
}

// Usage
var result = await _customerService.GetCustomerAsync(customerId);
result.Match(
    onSuccess: customer => ProcessCustomer(customer),
    onFailure: error => LogError(error)
);
```

#### 4. Authentication & Authorization
```csharp
// JWT Authentication
[Authorize]
[HttpGet]
public async Task<ActionResult<IEnumerable<CustomerDto>>> GetCustomers()
{
    // Only authenticated users can access
}

// Role-based Authorization
[Authorize(Roles = "Admin,Manager")]
[HttpDelete("{id}")]
public async Task<ActionResult> DeleteCustomer(int id)
{
    // Only Admin or Manager roles can delete
}

// Permission-based Authorization
[Authorize(Policy = "CanManageCustomers")]
[HttpPut("{id}")]
public async Task<ActionResult> UpdateCustomer(int id, UpdateCustomerCommand command)
{
    // Only users with specific permission can update
}
```

## 🔧 Customization Guide

### 1. Domain Entities

Replace the template `DomainEntity` with your specific entities:

```csharp
// Replace DomainEntity.cs with Customer.cs
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public DateTime CreatedDate { get; set; }
    
    // Business logic methods
    public void UpdateContactInfo(string email, string phone)
    {
        Email = email;
        Phone = phone;
        ModifiedDate = DateTime.UtcNow;
    }
}
```

### 2. CQRS Commands & Queries

Create specific commands and queries for your domain:

```csharp
// Commands/Customer/CreateCustomerCommand.cs
public class CreateCustomerCommand : ICommand<CustomerDto>
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
}

// Queries/Customer/GetCustomerQuery.cs
public class GetCustomerQuery : IQuery<CustomerDto>
{
    public int CustomerId { get; set; }
}
```

### 3. Frontend Components

Customize React components for your domain:

```typescript
// components/Customer/CustomerList.tsx
export const CustomerList: React.FC = () => {
    const { data: customers, isLoading } = useCustomers();
    
    if (isLoading) return <CircularProgress />;
    
    return (
        <DataGrid
            rows={customers}
            columns={customerColumns}
            pageSize={25}
        />
    );
};
```

### 4. Database Configuration

Update Entity Framework configurations:

```csharp
// Infrastructure/Data/Configurations/CustomerConfiguration.cs
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        
        builder.HasKey(c => c.Id);
        
        builder.Property(c => c.Name)
            .HasMaxLength(200)
            .IsRequired();
            
        builder.Property(c => c.Email)
            .HasMaxLength(255)
            .IsRequired();
            
        builder.HasIndex(c => c.Email)
            .IsUnique();
    }
}
```

## 🗄️ Database Setup

### 1. Connection Strings

Update `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyAppDB;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

### 2. Migrations

```bash
# Add initial migration
dotnet ef migrations add InitialCreate --project src/MyApp.Infrastructure --startup-project src/MyApp.Api

# Update database
dotnet ef database update --project src/MyApp.Infrastructure --startup-project src/MyApp.Api
```

### 3. Seed Data

Create seed data in `Infrastructure/Data/Seed/`:

```csharp
public static class DatabaseSeeder
{
    public static async Task SeedAsync(MyAppContext context)
    {
        if (!context.Customers.Any())
        {
            var customers = new[]
            {
                new Customer { Name = "John Doe", Email = "john@example.com" },
                new Customer { Name = "Jane Smith", Email = "jane@example.com" }
            };
            
            context.Customers.AddRange(customers);
            await context.SaveChangesAsync();
        }
    }
}
```

## 🐳 Docker Deployment

### Development

```bash
# Start development environment
docker-compose up -d

# View logs
docker-compose logs -f
```

### Production

```bash
# Build production images
docker-compose -f docker-compose.yml -f docker-compose.prod.yml build

# Deploy to production
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

## 🧪 Testing

### Unit Tests

```csharp
[Test]
public async Task CreateCustomer_ValidData_ReturnsSuccess()
{
    // Arrange
    var command = new CreateCustomerCommand
    {
        Name = "Test Customer",
        Email = "test@example.com"
    };
    
    // Act
    var result = await _handler.Handle(command, CancellationToken.None);
    
    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Name.Should().Be("Test Customer");
}
```

### Integration Tests

```csharp
[Test]
public async Task CreateCustomer_ValidData_CreatesInDatabase()
{
    // Arrange
    var customer = new CreateCustomerRequest
    {
        Name = "Integration Test Customer",
        Email = "integration@example.com"
    };
    
    // Act
    var response = await _client.PostAsJsonAsync("/api/customers", customer);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    
    var createdCustomer = await response.Content.ReadFromJsonAsync<CustomerDto>();
    createdCustomer.Name.Should().Be("Integration Test Customer");
}
```

## 📚 Best Practices

### 1. Domain Modeling
- Keep entities focused on business logic
- Use value objects for complex data types
- Implement domain services for multi-entity operations

### 2. Error Handling
- Use Result<T> pattern consistently
- Create specific error types for different scenarios
- Log errors with correlation IDs

### 3. Security
- Always validate input data
- Use authorization attributes on controllers
- Implement audit logging for sensitive operations

### 4. Performance
- Use pagination for large datasets
- Implement caching for frequently accessed data
- Use async/await consistently

### 5. Testing
- Write unit tests for business logic
- Create integration tests for API endpoints
- Use test doubles for external dependencies

## 🔗 Additional Resources

- [Clean Architecture Guide](./ARCHITECTURE.md)
- [Development Setup](./DEVELOPMENT.md)
- [Deployment Guide](./DEPLOYMENT.md)
- [Security Guide](./SECURITY.md)
- [API Documentation](./API.md)

## 🤝 Contributing

This template is based on the proven MonitoringGrid architecture. To contribute:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## 📄 License

MIT License - see [LICENSE](../LICENSE) for details.
