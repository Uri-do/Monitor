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

/// <summary>
/// Exception thrown when a requested resource is not found
/// </summary>
public class NotFoundException : DomainException
{
    public string? Details { get; }

    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string message, string details) : base(message)
    {
        Details = details;
    }

    public NotFoundException(string resourceType, object key)
        : base($"{resourceType} with key '{key}' was not found")
    {
        Details = $"Resource type: {resourceType}, Key: {key}";
    }
}

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : DomainException
{
    public Dictionary<string, List<string>>? Errors { get; }

    public ValidationException(string message) : base(message) { }

    public ValidationException(string message, Dictionary<string, List<string>> errors) : base(message)
    {
        Errors = errors;
    }

    public ValidationException(string field, string error) : base($"Validation failed for {field}")
    {
        Errors = new Dictionary<string, List<string>>
        {
            { field, new List<string> { error } }
        };
    }
}

/// <summary>
/// Exception thrown when user is not authorized
/// </summary>
public class UnauthorizedException : DomainException
{
    public UnauthorizedException(string message = "Unauthorized access") : base(message) { }
}

/// <summary>
/// Exception thrown when user is forbidden from accessing a resource
/// </summary>
public class ForbiddenException : DomainException
{
    public ForbiddenException(string message = "Access forbidden") : base(message) { }
}

/// <summary>
/// Exception thrown when there's a conflict with current state
/// </summary>
public class ConflictException : DomainException
{
    public string? Details { get; }

    public ConflictException(string message) : base(message) { }

    public ConflictException(string message, string details) : base(message)
    {
        Details = details;
    }
}

/// <summary>
/// Exception thrown when a business rule is violated
/// </summary>
public class BusinessRuleException : DomainException
{
    public string RuleName { get; }

    public BusinessRuleException(string ruleName, string message) : base(message)
    {
        RuleName = ruleName;
    }
}

/// <summary>
/// Exception thrown when external service calls fail
/// </summary>
public class ExternalServiceException : DomainException
{
    public string ServiceName { get; }

    public ExternalServiceException(string serviceName, string message) : base(message)
    {
        ServiceName = serviceName;
    }

    public ExternalServiceException(string serviceName, string message, Exception innerException)
        : base(message, innerException)
    {
        ServiceName = serviceName;
    }
}
