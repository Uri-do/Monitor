using MonitoringGrid.Api.Extensions;
using MonitoringGrid.Infrastructure;
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

// Add all services using organized extension methods
builder.Services.AddDatabaseServices(builder.Configuration);
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddMediatRServices();
builder.Services.AddValidationServices();
builder.Services.AddMappingServices();
builder.Services.AddSwaggerServices();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddCachingServices(builder.Configuration);
builder.Services.AddCorsServices(builder.Configuration);
builder.Services.AddHealthCheckServices(builder.Configuration);
builder.Services.AddRateLimitingServices(builder.Configuration);

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
