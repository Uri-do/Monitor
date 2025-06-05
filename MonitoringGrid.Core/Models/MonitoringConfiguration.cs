namespace MonitoringGrid.Core.Models;

/// <summary>
/// Configuration settings for the monitoring system
/// </summary>
public class MonitoringConfiguration
{
    public string SmsGateway { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public int MaxParallelExecutions { get; set; } = 5;
    public int AlertRetryCount { get; set; } = 3;
    public bool EnableSms { get; set; } = true;
    public bool EnableEmail { get; set; } = true;
    public bool EnableHistoricalComparison { get; set; } = true;
    public bool EnableAbsoluteThresholds { get; set; } = true;
    public int ServiceIntervalSeconds { get; set; } = 30;
    public int DatabaseTimeoutSeconds { get; set; } = 30;
    public int BatchSize { get; set; } = 10;
    public int MaxAlertHistoryDays { get; set; } = 90;
    public int HistoricalWeeksBack { get; set; } = 4;

    // Domain methods
    public bool IsValid(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(AdminEmail))
            errors.Add("AdminEmail is required");

        if (MaxParallelExecutions <= 0)
            errors.Add("MaxParallelExecutions must be greater than 0");

        if (AlertRetryCount < 0)
            errors.Add("AlertRetryCount cannot be negative");

        if (ServiceIntervalSeconds <= 0)
            errors.Add("ServiceIntervalSeconds must be greater than 0");

        if (DatabaseTimeoutSeconds <= 0)
            errors.Add("DatabaseTimeoutSeconds must be greater than 0");

        if (BatchSize <= 0)
            errors.Add("BatchSize must be greater than 0");

        if (MaxAlertHistoryDays <= 0)
            errors.Add("MaxAlertHistoryDays must be greater than 0");

        if (HistoricalWeeksBack <= 0)
            errors.Add("HistoricalWeeksBack must be greater than 0");

        if (EnableSms && string.IsNullOrWhiteSpace(SmsGateway))
            errors.Add("SmsGateway is required when SMS is enabled");

        return errors.Count == 0;
    }
}

/// <summary>
/// Email configuration settings
/// </summary>
public class EmailConfiguration
{
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Monitoring Grid System";
    public int TimeoutMs { get; set; } = 30000;

    // Domain methods
    public bool IsValid(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(SmtpServer))
            errors.Add("SmtpServer is required");

        if (SmtpPort <= 0 || SmtpPort > 65535)
            errors.Add("SmtpPort must be between 1 and 65535");

        if (string.IsNullOrWhiteSpace(Username))
            errors.Add("Username is required");

        if (string.IsNullOrWhiteSpace(Password))
            errors.Add("Password is required");

        if (string.IsNullOrWhiteSpace(FromAddress))
            errors.Add("FromAddress is required");

        if (TimeoutMs <= 0)
            errors.Add("TimeoutMs must be greater than 0");

        // Basic email format validation
        if (!string.IsNullOrWhiteSpace(FromAddress) && !FromAddress.Contains('@'))
            errors.Add("FromAddress must be a valid email address");

        return errors.Count == 0;
    }
}
