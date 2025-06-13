# API Layer Template

This document describes the API layer template that provides RESTful web API endpoints, CQRS implementation, and comprehensive middleware pipeline.

## 🏗️ Architecture Overview

The API layer implements a clean, modern web API with:

- **CQRS Pattern**: Command Query Responsibility Segregation with MediatR
- **RESTful Design**: Standard HTTP methods and status codes
- **Clean Controllers**: Thin controllers that delegate to CQRS handlers
- **Comprehensive Middleware**: Security, logging, monitoring, and error handling
- **API Documentation**: Swagger/OpenAPI with detailed documentation
- **Authentication & Authorization**: JWT-based security (optional)
- **Performance Monitoring**: Metrics, health checks, and observability

## 📁 Project Structure

```
EnterpriseApp.Api/
├── Controllers/
│   ├── Base/
│   │   └── BaseApiController.cs         # Base controller with common functionality
│   └── DomainEntitiesController.cs      # Main domain entity endpoints
├── CQRS/
│   ├── ICommand.cs                      # Command interfaces and base classes
│   ├── IQuery.cs                        # Query interfaces and base classes
│   ├── Commands/
│   │   └── DomainEntityCommands.cs      # Domain entity commands
│   ├── Queries/
│   │   └── DomainEntityQueries.cs       # Domain entity queries
│   └── Handlers/
│       ├── DomainEntityCommandHandlers.cs
│       └── DomainEntityQueryHandlers.cs
├── DTOs/
│   ├── DomainEntityDto.cs               # Data transfer objects
│   └── AuditLogDto.cs                   # Audit log DTOs
├── Middleware/
│   ├── GlobalExceptionMiddleware.cs     # Global exception handling
│   └── RequestResponseLoggingMiddleware.cs
├── Extensions/
│   ├── ServiceCollectionExtensions.cs  # Service registration
│   └── ApplicationBuilderExtensions.cs # Middleware configuration
├── Mappings/
│   └── DomainEntityMappingProfile.cs   # AutoMapper profiles
├── Filters/                             # Action filters
├── Validators/                          # FluentValidation validators
└── Program.cs                           # Application entry point
```

## 🎯 CQRS Implementation

### Command Pattern

Commands represent write operations and business actions:

```csharp
public class CreateDomainEntityCommand : BaseCommand<DomainEntityDto>
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    [Range(1, 5)]
    public int Priority { get; set; } = 3;
}

public class CreateDomainEntityCommandHandler : ICommandHandler<CreateDomainEntityCommand, DomainEntityDto>
{
    public async Task<Result<DomainEntityDto>> Handle(CreateDomainEntityCommand request, CancellationToken cancellationToken)
    {
        // Business logic implementation
        var entity = await _domainEntityService.CreateAsync(createRequest, request.UserId, cancellationToken);
        var dto = _mapper.Map<DomainEntityDto>(entity);
        return Result<DomainEntityDto>.Success(dto);
    }
}
```

### Query Pattern

Queries represent read operations with caching support:

```csharp
public class GetDomainEntitiesQuery : PagedQuery<PagedResult<DomainEntityDto>>
{
    public string? Category { get; set; }
    public DomainEntityStatus? Status { get; set; }
    public bool? IsActive { get; set; }
    public string? SearchTerm { get; set; }
}

public class GetDomainEntitiesQueryHandler : IQueryHandler<GetDomainEntitiesQuery, PagedResult<DomainEntityDto>>
{
    public async Task<Result<PagedResult<DomainEntityDto>>> Handle(GetDomainEntitiesQuery request, CancellationToken cancellationToken)
    {
        // Check cache first
        if (request.UseCache)
        {
            var cachedResult = await _cacheService.GetAsync<PagedResult<DomainEntityDto>>(cacheKey, cancellationToken);
            if (cachedResult != null) return Result<PagedResult<DomainEntityDto>>.Success(cachedResult);
        }
        
        // Query data and cache result
        var (entities, totalCount) = await repository.GetPagedWithFilterAsync(/* parameters */);
        var pagedResult = PagedResult<DomainEntityDto>.Create(dtos, request.Page, request.PageSize, totalCount);
        
        await _cacheService.SetAsync(cacheKey, pagedResult, expiration, cancellationToken);
        return Result<PagedResult<DomainEntityDto>>.Success(pagedResult);
    }
}
```

## 🎮 Controller Design

### Base Controller

All controllers inherit from `BaseApiController` which provides:

```csharp
public abstract class BaseApiController : ControllerBase
{
    protected readonly IMediator Mediator;
    protected readonly ILogger Logger;

    // Result handling
    protected ActionResult<T> HandleResult<T>(Result<T> result)
    protected ActionResult HandleResult(Result result)
    
    // User context
    protected string? GetCurrentUserId()
    protected List<string> GetCurrentUserRoles()
    protected bool HasRole(string role)
    
    // Request context
    protected string? GetClientIpAddress()
    protected string? GetUserAgent()
    protected string GetCorrelationId()
}
```

### Domain Controller

Clean, focused controllers that delegate to CQRS handlers:

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize] // If auth is enabled
public class DomainEntitiesController : BaseApiController
{
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DomainEntityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DomainEntityDto>> GetById(int id, [FromQuery] bool includeItems = false)
    {
        var query = new GetDomainEntityByIdQuery { Id = id, IncludeItems = includeItems, UserId = GetCurrentUserId() };
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpPost]
    [Authorize(Policy = "CanCreateDomainEntity")]
    [ProducesResponseType(typeof(DomainEntityDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<DomainEntityDto>> Create([FromBody] CreateDomainEntityCommand command)
    {
        command.UserId = GetCurrentUserId();
        command.IpAddress = GetClientIpAddress();
        var result = await Mediator.Send(command);
        
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
        
        return HandleResult(result);
    }
}
```

## 🛡️ Middleware Pipeline

### Security Middleware

```csharp
public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
{
    return app.Use(async (context, next) =>
    {
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        
        var csp = "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval';";
        context.Response.Headers.Add("Content-Security-Policy", csp);
        
        await next();
    });
}
```

### Global Exception Handling

```csharp
public class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var problemDetails = CreateProblemDetails(context, exception);
        response.StatusCode = problemDetails.Status ?? 500;
        
        var json = JsonSerializer.Serialize(problemDetails, jsonOptions);
        await response.WriteAsync(json);
    }
}
```

### Request/Response Logging

```csharp
public class RequestResponseLoggingMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = GetOrCreateCorrelationId(context);

        await LogRequestAsync(context, correlationId);
        
        // Capture response
        var originalResponseBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            await LogResponseAsync(context, correlationId, stopwatch.ElapsedMilliseconds);
            
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalResponseBodyStream);
        }
    }
}
```

## 📊 Data Transfer Objects (DTOs)

### Rich DTOs with Computed Properties

```csharp
public class DomainEntityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DomainEntityStatus Status { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    
    // Related data
    public List<DomainEntityItemDto> Items { get; set; } = new();
    public List<AuditLogDto> AuditLogs { get; set; } = new();
    
    // Computed properties
    public DomainEntityComputedDto Computed { get; set; } = new();
}

public class DomainEntityComputedDto
{
    public int TotalItems { get; set; }
    public int ActiveItems { get; set; }
    public decimal TotalValue { get; set; }
    public int DaysSinceCreation { get; set; }
    public List<string> TagList { get; set; } = new();
    public string StatusDisplayName { get; set; } = string.Empty;
    public bool CanDelete { get; set; }
    public bool CanActivate { get; set; }
}
```

## 🗺️ AutoMapper Configuration

### Comprehensive Mapping Profiles

```csharp
public class DomainEntityMappingProfile : Profile
{
    public DomainEntityMappingProfile()
    {
        // Entity to DTO mappings
        CreateMap<DomainEntity, DomainEntityDto>()
            .ForMember(dest => dest.Computed, opt => opt.MapFrom(src => CreateComputedProperties(src)))
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items.Where(i => i.IsActive)));

        // Command to model mappings
        CreateMap<CreateDomainEntityCommand, CreateDomainEntityRequest>();
        CreateMap<UpdateDomainEntityCommand, UpdateDomainEntityRequest>();

        // Statistics mappings
        CreateMap<DomainEntityStatistics, DomainEntityStatisticsDto>()
            .ForMember(dest => dest.Growth, opt => opt.MapFrom(src => CreateGrowthStatistics(src)));
    }
}
```

## 🔧 Service Configuration

### Comprehensive Service Registration

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // API versioning
        services.AddApiVersioning();
        
        // Swagger documentation
        services.AddSwaggerDocumentation();
        
        // FluentValidation
        services.AddFluentValidation();
        
        // Authentication (if enabled)
        services.AddAuthentication(configuration);
        services.AddAuthorization();
        
        // CORS
        services.AddCors();
        
        // Response compression
        services.AddResponseCompression();
        
        // Health checks
        services.AddHealthChecks(configuration);
        
        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
        
        // AutoMapper
        services.AddAutoMapper();
        
        return services;
    }
}
```

## 📋 API Features

### Complete CRUD Operations
- **GET** `/api/domainentities` - Get all with pagination and filtering
- **GET** `/api/domainentities/{id}` - Get by ID with optional includes
- **GET** `/api/domainentities/active` - Get active entities
- **GET** `/api/domainentities/search` - Search with full-text search
- **GET** `/api/domainentities/statistics` - Get comprehensive statistics
- **GET** `/api/domainentities/categories` - Get all categories
- **GET** `/api/domainentities/tags` - Get all tags
- **POST** `/api/domainentities` - Create new entity
- **PUT** `/api/domainentities/{id}` - Update existing entity
- **DELETE** `/api/domainentities/{id}` - Delete entity
- **POST** `/api/domainentities/{id}/activate` - Activate entity
- **POST** `/api/domainentities/{id}/deactivate` - Deactivate entity
- **POST** `/api/domainentities/bulk` - Bulk operations
- **GET** `/api/domainentities/{id}/audit` - Get audit trail
- **GET** `/api/domainentities/export` - Export data

### Advanced Features
- **Pagination**: Consistent pagination with metadata
- **Filtering**: Multiple filter options with query parameters
- **Sorting**: Flexible sorting by any field
- **Search**: Full-text search across multiple fields
- **Caching**: Intelligent caching with cache invalidation
- **Bulk Operations**: Efficient bulk processing
- **Audit Trail**: Complete audit logging
- **Export**: Multiple export formats (Excel, CSV, JSON, PDF)
- **Statistics**: Rich analytics and reporting

### Security Features (if auth enabled)
- **JWT Authentication**: Token-based authentication
- **Role-Based Authorization**: Fine-grained permissions
- **Policy-Based Authorization**: Flexible authorization policies
- **Audit Logging**: Security event tracking
- **Rate Limiting**: API rate limiting
- **CORS**: Cross-origin resource sharing

### Monitoring & Observability
- **Health Checks**: Comprehensive health monitoring
- **Metrics**: Prometheus metrics collection
- **Logging**: Structured logging with Serilog
- **Tracing**: Request correlation and tracing
- **Performance Monitoring**: Response time tracking

## 🚀 Getting Started

1. **Configure Services**: Update `Program.cs` with required services
2. **Configure Database**: Set connection strings in `appsettings.json`
3. **Configure Authentication**: Set JWT settings (if auth enabled)
4. **Run Migrations**: Apply database migrations
5. **Start API**: Run the application
6. **Access Documentation**: Visit `/swagger` for API documentation

## 📈 Production Features

### Performance
- **Response Compression**: Gzip and Brotli compression
- **Caching**: Multi-level caching strategy
- **Connection Pooling**: Optimized database connections
- **Async/Await**: Non-blocking operations throughout

### Security
- **Security Headers**: Comprehensive security headers
- **HTTPS Enforcement**: HTTPS redirection and HSTS
- **Input Validation**: FluentValidation with sanitization
- **Error Handling**: Secure error responses

### Reliability
- **Circuit Breaker**: Fault tolerance patterns
- **Retry Policies**: Automatic retry for transient failures
- **Timeout Handling**: Request timeout management
- **Graceful Shutdown**: Clean application shutdown

### Monitoring
- **Health Checks**: Database, cache, and service health
- **Metrics Collection**: Performance and business metrics
- **Structured Logging**: JSON-formatted logs
- **Correlation IDs**: Request tracking across services

The API layer provides a complete, production-ready web API with modern patterns, comprehensive features, and excellent developer experience! 🎯
