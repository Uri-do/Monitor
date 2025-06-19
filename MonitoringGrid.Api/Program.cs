using MonitoringGrid.Api.Extensions;
using MonitoringGrid.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/monitoring-api-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services using extension methods
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Add basic services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddSignalR();
builder.Services.AddScoped<MonitoringGrid.Api.Hubs.IRealtimeNotificationService, MonitoringGrid.Api.Hubs.RealtimeNotificationService>();
builder.Services.AddScoped<MonitoringGrid.Api.Authentication.IApiKeyService, MonitoringGrid.Api.Authentication.InMemoryApiKeyService>();

// Add Authentication and Authorization (configured for development)
var jwtSettings = builder.Configuration.GetSection("Security:Jwt");
var secretKey = jwtSettings["SecretKey"] ?? "MonitoringGrid-Super-Secret-Key-That-Is-Long-Enough-For-HMAC-SHA256-Algorithm-2024";

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"] ?? "MonitoringGrid.Api",
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"] ?? "MonitoringGrid.Frontend",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        // Handle authentication failures gracefully
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                // Log authentication failures but don't block anonymous endpoints
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogDebug("JWT Authentication failed: {Error}", context.Exception.Message);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Add policy for development endpoints that allow anonymous access
    options.AddPolicy("AllowAnonymous", policy => policy.RequireAssertion(_ => true));

    // Default policy requires authentication
    options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddResponseCompression();
builder.Services.AddMemoryCache();

// Add Entity Framework
builder.Services.AddDbContext<MonitoringGrid.Infrastructure.Data.MonitoringContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Add MediatR - scan all relevant assemblies for handlers
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly); // API assembly
    cfg.RegisterServicesFromAssembly(typeof(MonitoringGrid.Core.Entities.Indicator).Assembly); // Core assembly
    cfg.RegisterServicesFromAssembly(typeof(MonitoringGrid.Infrastructure.DependencyInjection).Assembly); // Infrastructure assembly
});

// Add custom middleware options
builder.Services.AddSingleton(new MonitoringGrid.Api.Middleware.ResponseCachingOptions
{
    Enabled = true,
    DefaultDuration = TimeSpan.FromMinutes(5),
    MaxCacheSize = 100,
    EnableCompression = false // Temporarily disabled to fix encoding issues
});

builder.Services.AddCors(options =>
{
    // Get allowed origins from configuration
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? new[] { "http://localhost:3000", "http://localhost:5173" };

    // Development policy - more permissive
    options.AddPolicy("DevelopmentPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });

    // Default policy for production
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});
builder.Services.AddHealthChecks();

// Add Round 3 Enterprise Features
builder.Services.AddDataProtection();

// Add Infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

// Add Simple Indicator Processor only if worker services are disabled
var enableWorkerServices = builder.Configuration.GetValue<bool>("MonitoringGrid:Monitoring:EnableWorkerServices", true);
if (!enableWorkerServices)
{
    builder.Services.AddHostedService<MonitoringGrid.Api.Services.SimpleIndicatorProcessor>();
    Console.WriteLine("✅ SimpleIndicatorProcessor registered (worker services disabled)");
}
else
{
    Console.WriteLine("⚠️ SimpleIndicatorProcessor skipped (worker services enabled)");
}

// Build the application
var app = builder.Build();

// Configure the middleware pipeline
await app.ConfigureMiddlewarePipelineAsync(app.Environment);

// Run the application
app.Run();

// Make Program class accessible for testing
public partial class Program { }
