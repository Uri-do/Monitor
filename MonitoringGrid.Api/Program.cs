using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MonitoringGrid.Api.Mapping;
using MonitoringGrid.Api.Hubs;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Models;
using MonitoringGrid.Core.Services;
using MonitoringGrid.Infrastructure.Data;
using MonitoringGrid.Infrastructure.Repositories;
using MonitoringGrid.Infrastructure.Services;
using Serilog;
using System.Text.Json.Serialization;
using FluentValidation.AspNetCore;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Add configuration sections
builder.Services.Configure<MonitoringConfiguration>(
    builder.Configuration.GetSection("Monitoring"));
builder.Services.Configure<EmailConfiguration>(
    builder.Configuration.GetSection("Email"));

// Add Entity Framework
var connectionString = builder.Configuration.GetConnectionString("MonitoringGrid");
if (string.IsNullOrEmpty(connectionString))
{
    Log.Fatal("MonitoringGrid connection string is required");
    return 1;
}

builder.Services.AddDbContext<MonitoringContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(30);
    });
    
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Add domain services
builder.Services.AddScoped<KpiDomainService>();

// Add repository services
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IAlertRepository, AlertRepository>();

// Add application services
builder.Services.AddScoped<IKpiExecutionService, KpiExecutionService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IRealtimeNotificationService, RealtimeNotificationService>();

// Add SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173") // React dev servers
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "Monitoring Grid API", 
        Version = "v1",
        Description = "API for managing KPI monitoring and alerting system"
    });
    
    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<MonitoringContext>("database")
    .AddCheck("api", () => HealthCheckResult.Healthy("API is running"));

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Monitoring Grid API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowReactApp");

app.UseAuthorization();

app.MapControllers();

// Map SignalR hubs
app.MapHub<MonitoringHub>("/monitoring-hub");

// Add health check endpoint
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                description = x.Value.Description,
                duration = x.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

// Add API info endpoint
app.MapGet("/api/info", () => new
{
    Name = "Monitoring Grid API",
    Version = "1.0.0",
    Environment = app.Environment.EnvironmentName,
    Timestamp = DateTime.UtcNow
});

// Ensure database is created and accessible
try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<MonitoringContext>();
    
    Log.Information("Checking database connection...");
    await context.Database.CanConnectAsync();
    Log.Information("Database connection successful");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Failed to connect to database: {Message}", ex.Message);
    return 1;
}

Log.Information("Monitoring Grid API starting...");
Log.Information("Swagger UI available at: {BaseUrl}", app.Environment.IsDevelopment() ? "https://localhost:7000" : "API base URL");

try
{
    await app.RunAsync();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "API terminated unexpectedly: {Message}", ex.Message);
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
