namespace MonitoringGrid.Api.Constants;

/// <summary>
/// Constants for Worker service operations
/// </summary>
public static class WorkerConstants
{
    /// <summary>
    /// Timeout values in milliseconds
    /// </summary>
    public static class Timeouts
    {
        /// <summary>
        /// Time to wait for process initialization after start
        /// </summary>
        public const int ProcessInitializationMs = 500;

        /// <summary>
        /// Time to wait for graceful shutdown
        /// </summary>
        public const int GracefulShutdownMs = 3000;

        /// <summary>
        /// Time to wait for force kill
        /// </summary>
        public const int ForceKillMs = 5000;

        /// <summary>
        /// Time to wait for force kill with entire process tree
        /// </summary>
        public const int ForceKillTreeMs = 10000;

        /// <summary>
        /// Time to wait between restart operations
        /// </summary>
        public const int RestartDelayMs = 2000;
    }

    /// <summary>
    /// Configuration keys
    /// </summary>
    public static class ConfigKeys
    {
        /// <summary>
        /// Configuration key for enabling worker services
        /// </summary>
        public const string EnableWorkerServices = "Monitoring:EnableWorkerServices";
    }

    /// <summary>
    /// Worker service modes
    /// </summary>
    public static class Modes
    {
        /// <summary>
        /// Integrated worker mode
        /// </summary>
        public const string Integrated = "Integrated";

        /// <summary>
        /// Manual worker mode
        /// </summary>
        public const string Manual = "Manual";
    }

    /// <summary>
    /// Process and service names
    /// </summary>
    public static class Names
    {
        /// <summary>
        /// MonitoringGrid namespace for worker services
        /// </summary>
        public const string WorkerNamespace = "MonitoringGrid.Worker";

        /// <summary>
        /// MonitoringGrid process name pattern
        /// </summary>
        public const string ProcessNamePattern = "MonitoringGrid";

        /// <summary>
        /// Monitor process name pattern (alternative)
        /// </summary>
        public const string MonitorProcessPattern = "Monitor";

        /// <summary>
        /// Default scheduler name
        /// </summary>
        public const string DefaultSchedulerName = "Default Interval Scheduler";

        /// <summary>
        /// System user name
        /// </summary>
        public const string SystemUser = "system";
    }

    /// <summary>
    /// File paths and directories
    /// </summary>
    public static class Paths
    {
        /// <summary>
        /// Worker project directory relative path
        /// </summary>
        public const string WorkerProjectPath = "MonitoringGrid.Worker";

        /// <summary>
        /// Worker project file name
        /// </summary>
        public const string WorkerProjectFile = "MonitoringGrid.Worker.csproj";

        /// <summary>
        /// Dotnet executable name
        /// </summary>
        public const string DotnetExecutable = "dotnet";

        /// <summary>
        /// Dotnet run command arguments template
        /// </summary>
        public const string DotnetRunArgs = "run --project MonitoringGrid.Worker.csproj";
    }

    /// <summary>
    /// Default values for scheduler configuration
    /// </summary>
    public static class Defaults
    {
        /// <summary>
        /// Default scheduler interval in minutes
        /// </summary>
        public const int SchedulerIntervalMinutes = 5;

        /// <summary>
        /// Default scheduler description
        /// </summary>
        public const string SchedulerDescription = "Default scheduler for indicators - runs every 5 minutes";

        /// <summary>
        /// Default schedule type
        /// </summary>
        public const string ScheduleType = "interval";

        /// <summary>
        /// Default timezone
        /// </summary>
        public const string Timezone = "UTC";

        /// <summary>
        /// Maximum minutes since heartbeat for health check
        /// </summary>
        public const int MaxMinutesSinceHeartbeat = 5;
    }

    /// <summary>
    /// Status messages
    /// </summary>
    public static class Messages
    {
        /// <summary>
        /// Worker services are integrated message
        /// </summary>
        public const string WorkerServicesIntegrated = "Worker services are integrated. Restart the API to enable them.";

        /// <summary>
        /// Worker services integrated disable message
        /// </summary>
        public const string WorkerServicesIntegratedDisable = "Worker services are integrated. Restart the API to disable them.";

        /// <summary>
        /// No worker processes running message
        /// </summary>
        public const string NoWorkerProcesses = "No worker processes are currently running";

        /// <summary>
        /// Failed to start worker process message
        /// </summary>
        public const string FailedToStartWorker = "Failed to start Worker process";

        /// <summary>
        /// Failed to stop any worker processes message
        /// </summary>
        public const string FailedToStopWorkers = "Failed to stop any worker processes";

        /// <summary>
        /// No worker processes found to stop message
        /// </summary>
        public const string NoWorkerProcessesToStop = "No worker processes found to stop";

        /// <summary>
        /// All indicators already have schedulers message
        /// </summary>
        public const string AllIndicatorsHaveSchedulers = "All indicators already have schedulers assigned";
    }

    /// <summary>
    /// Service status values
    /// </summary>
    public static class Status
    {
        /// <summary>
        /// Running status
        /// </summary>
        public const string Running = "Running";

        /// <summary>
        /// Manual execution context
        /// </summary>
        public const string Manual = "Manual";
    }
}
