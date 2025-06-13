using Serilog;
using EnterpriseApp.Worker.Extensions;
using EnterpriseApp.Worker.Services;
using EnterpriseApp.Infrastructure;
<!--#if (enableMonitoring)-->
using Prometheus;
<!--#endif-->

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/worker-log-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting EnterpriseApp Worker Service");

    var builder = Host.CreateApplicationBuilder(args);

    // Add Serilog
    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/worker-log-.txt", rollingInterval: RollingInterval.Day));

    // Configure for Windows Service or Systemd
    if (OperatingSystem.IsWindows())
    {
        builder.Services.AddWindowsService(options =>
        {
            options.ServiceName = "EnterpriseApp Worker";
        });
    }
    else if (OperatingSystem.IsLinux())
    {
        builder.Services.AddSystemd();
    }

    // Add Infrastructure services
    builder.Services.AddInfrastructure(builder.Configuration);

    // Add Worker services
    builder.Services.AddWorkerServices(builder.Configuration);

    // Add Job Scheduling
    builder.Services.AddJobScheduling(builder.Configuration);

    // Add Background Services
    builder.Services.AddBackgroundServices();

<!--#if (enableMonitoring)-->
    // Add Health Checks
    builder.Services.AddWorkerHealthChecks(builder.Configuration);

    // Add Metrics
    builder.Services.AddWorkerMetrics();
<!--#endif-->

<!--#if (enableRealtime)-->
    // Add SignalR Client
    builder.Services.AddSignalRClient(builder.Configuration);
<!--#endif-->

    // Add HTTP clients
    builder.Services.AddHttpClients(builder.Configuration);

    var host = builder.Build();

    // Ensure database is ready
    await host.Services.EnsureDatabaseAsync();

<!--#if (enableMonitoring)-->
    // Start metrics server
    var metricsPort = builder.Configuration.GetValue<int>("Metrics:Port", 9090);
    var metricServer = new MetricServer(port: metricsPort);
    metricServer.Start();
    Log.Information("Metrics server started on port {Port}", metricsPort);
<!--#endif-->

    // Start the worker
    Log.Information("EnterpriseApp Worker Service started successfully");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "EnterpriseApp Worker Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
