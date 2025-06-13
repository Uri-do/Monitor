using Serilog;
using EnterpriseApp.Api.Extensions;
using EnterpriseApp.Api.Middleware;
using EnterpriseApp.Infrastructure;
<!--#if (enableAuth)-->
using EnterpriseApp.Api.Security;
<!--#endif-->
<!--#if (enableRealtime)-->
using EnterpriseApp.Api.Hubs;
<!--#endif-->

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting EnterpriseApp API");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day));

    // Validate configuration
    ConfigurationValidation.ValidateConfiguration(builder.Configuration);

    // Add services to the container
    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<GlobalExceptionFilter>();
        options.Filters.Add<ValidationFilter>();
    });

    // Add API versioning
    builder.Services.AddApiVersioning();

    // Add Swagger/OpenAPI
    builder.Services.AddSwaggerDocumentation();

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DefaultPolicy", policy =>
        {
            policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" })
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });

    // Add compression
    builder.Services.AddResponseCompression();

    // Add caching
    builder.Services.AddMemoryCache();
    builder.Services.AddResponseCaching();

    // Add Infrastructure services
    builder.Services.AddInfrastructure(builder.Configuration);

    // Add MediatR
    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

    // Add AutoMapper
    builder.Services.AddAutoMapper();

    // Add FluentValidation
    builder.Services.AddFluentValidation();

<!--#if (enableAuth)-->
    // Add Authentication & Authorization
    builder.Services.AddAuthentication(builder.Configuration);
    builder.Services.AddAuthorization();
<!--#endif-->

<!--#if (enableRealtime)-->
    // Add SignalR
    builder.Services.AddSignalR();
<!--#endif-->

<!--#if (enableMonitoring)-->
    // Add Health Checks
    builder.Services.AddHealthChecks(builder.Configuration);

    // Add Metrics
    builder.Services.AddMetrics();
<!--#endif-->

    // Add custom services
    builder.Services.AddApplicationServices();

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "EnterpriseApp API V1");
            c.RoutePrefix = "swagger";
            c.DisplayRequestDuration();
            c.EnableDeepLinking();
            c.EnableFilter();
            c.ShowExtensions();
        });
    }
    else
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    // Security headers
    app.UseSecurityHeaders();

    // HTTPS redirection
    app.UseHttpsRedirection();

    // Response compression
    app.UseResponseCompression();

    // CORS
    app.UseCors("DefaultPolicy");

    // Request/Response logging
    app.UseRequestResponseLogging();

    // Performance monitoring
    app.UsePerformanceMonitoring();

<!--#if (enableMonitoring)-->
    // Metrics
    app.UseMetrics();

    // Health checks
    app.UseHealthChecks();
<!--#endif-->

    // Response caching
    app.UseResponseCaching();

<!--#if (enableAuth)-->
    // Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();
<!--#endif-->

    // Global exception handling
    app.UseMiddleware<GlobalExceptionMiddleware>();

    // Rate limiting
    app.UseRateLimiting();

    // API versioning
    app.UseApiVersioning();

    // Controllers
    app.MapControllers();

<!--#if (enableRealtime)-->
    // SignalR Hubs
    app.MapHub<NotificationHub>("/hubs/notifications");
<!--#endif-->

<!--#if (enableMonitoring)-->
    // Health check UI
    app.MapHealthChecksUI();
<!--#endif-->

    // Ensure database is created and seeded
    await app.Services.EnsureDatabaseAsync();
    await app.Services.SeedDataAsync();

    Log.Information("EnterpriseApp API started successfully");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "EnterpriseApp API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
