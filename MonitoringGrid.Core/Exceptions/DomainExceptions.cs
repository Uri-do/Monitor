namespace MonitoringGrid.Core.Exceptions;

/// <summary>
/// Base class for all domain exceptions
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }

    protected DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a business rule is violated
/// </summary>
public class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string rule, string details) 
        : base($"Business rule violation: {rule}. {details}")
    {
        Rule = rule;
        Details = details;
    }

    public string Rule { get; }
    public string Details { get; }
}

/// <summary>
/// Exception thrown when an entity is not found
/// </summary>
public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityType, object id) 
        : base($"{entityType} with ID '{id}' was not found")
    {
        EntityType = entityType;
        Id = id;
    }

    public string EntityType { get; }
    public object Id { get; }
}

/// <summary>
/// Exception thrown when an entity already exists
/// </summary>
public class EntityAlreadyExistsException : DomainException
{
    public EntityAlreadyExistsException(string entityType, string identifier) 
        : base($"{entityType} with identifier '{identifier}' already exists")
    {
        EntityType = entityType;
        Identifier = identifier;
    }

    public string EntityType { get; }
    public string Identifier { get; }
}

/// <summary>
/// Exception thrown when a KPI validation fails
/// </summary>
public class KpiValidationException : DomainException
{
    public KpiValidationException(string indicator, List<string> validationErrors) 
        : base($"KPI '{indicator}' validation failed: {string.Join(", ", validationErrors)}")
    {
        Indicator = indicator;
        ValidationErrors = validationErrors;
    }

    public string Indicator { get; }
    public List<string> ValidationErrors { get; }
}

/// <summary>
/// Exception thrown when a KPI execution fails
/// </summary>
public class KpiExecutionException : DomainException
{
    public KpiExecutionException(string indicator, string error) 
        : base($"KPI '{indicator}' execution failed: {error}")
    {
        Indicator = indicator;
    }

    public KpiExecutionException(string indicator, string error, Exception innerException) 
        : base($"KPI '{indicator}' execution failed: {error}", innerException)
    {
        Indicator = indicator;
    }

    public string Indicator { get; }
}

/// <summary>
/// Exception thrown when an alert operation fails
/// </summary>
public class AlertOperationException : DomainException
{
    public AlertOperationException(int alertId, string operation, string error) 
        : base($"Alert {alertId} {operation} failed: {error}")
    {
        AlertId = alertId;
        Operation = operation;
    }

    public int AlertId { get; }
    public string Operation { get; }
}

/// <summary>
/// Exception thrown when a configuration is invalid
/// </summary>
public class InvalidConfigurationException : DomainException
{
    public InvalidConfigurationException(string configKey, string error) 
        : base($"Configuration '{configKey}' is invalid: {error}")
    {
        ConfigKey = configKey;
    }

    public string ConfigKey { get; }
}

/// <summary>
/// Exception thrown when a user operation is not authorized
/// </summary>
public class UnauthorizedOperationException : DomainException
{
    public UnauthorizedOperationException(string operation, string user) 
        : base($"User '{user}' is not authorized to perform operation: {operation}")
    {
        Operation = operation;
        User = user;
    }

    public string Operation { get; }
    public string User { get; }
}

/// <summary>
/// Exception thrown when a notification fails to send
/// </summary>
public class NotificationException : DomainException
{
    public NotificationException(string channel, string recipient, string error) 
        : base($"Failed to send notification via {channel} to {recipient}: {error}")
    {
        Channel = channel;
        Recipient = recipient;
    }

    public NotificationException(string channel, string recipient, string error, Exception innerException) 
        : base($"Failed to send notification via {channel} to {recipient}: {error}", innerException)
    {
        Channel = channel;
        Recipient = recipient;
    }

    public string Channel { get; }
    public string Recipient { get; }
}
