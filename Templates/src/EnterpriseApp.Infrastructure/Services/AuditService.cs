using Microsoft.Extensions.Logging;
using System.Text.Json;
using EnterpriseApp.Core.Entities;
using EnterpriseApp.Core.Interfaces;
using EnterpriseApp.Core.Models;

namespace EnterpriseApp.Infrastructure.Services;

/// <summary>
/// Audit service implementation for tracking changes
/// </summary>
public class AuditService : IAuditService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuditService> _logger;

    /// <summary>
    /// Initializes a new instance of the AuditService class
    /// </summary>
    public AuditService(IUnitOfWork unitOfWork, ILogger<AuditService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task LogAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        try
        {
            var repository = _unitOfWork.Repository<AuditLog>();
            await repository.AddAsync(auditLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Audit log created: {EntityName} {EntityId} {Action} by {UserId}", 
                auditLog.EntityName, auditLog.EntityId, auditLog.Action, auditLog.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log for {EntityName} {EntityId}", 
                auditLog.EntityName, auditLog.EntityId);
            // Don't rethrow - audit logging should not break the main operation
        }
    }

    /// <inheritdoc />
    public async Task LogCreationAsync(string entityName, string entityId, string userId, string? username = null, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var auditLog = AuditLog.ForCreation(entityName, entityId, userId, username, ipAddress);
        await LogAsync(auditLog, cancellationToken);
    }

    /// <inheritdoc />
    public async Task LogUpdateAsync(string entityName, string entityId, string userId, object? oldValues = null, object? newValues = null, string? username = null, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var oldValuesJson = oldValues != null ? JsonSerializer.Serialize(oldValues) : null;
        var newValuesJson = newValues != null ? JsonSerializer.Serialize(newValues) : null;

        var auditLog = AuditLog.ForUpdate(entityName, entityId, userId, oldValuesJson, newValuesJson, username, ipAddress);
        await LogAsync(auditLog, cancellationToken);
    }

    /// <inheritdoc />
    public async Task LogDeletionAsync(string entityName, string entityId, string userId, string? username = null, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var auditLog = AuditLog.ForDeletion(entityName, entityId, userId, username, ipAddress);
        await LogAsync(auditLog, cancellationToken);
    }

    /// <inheritdoc />
    public async Task LogCustomAsync(string entityName, string entityId, string actionDescription, string userId, string? username = null, string? ipAddress = null, string severity = "Information", CancellationToken cancellationToken = default)
    {
        var auditLog = AuditLog.ForCustomAction(entityName, entityId, actionDescription, userId, username, ipAddress, severity);
        await LogAsync(auditLog, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditLog>> GetAuditTrailAsync(string entityName, string entityId, CancellationToken cancellationToken = default)
    {
        try
        {
            var repository = _unitOfWork.Repository<AuditLog>() as IAuditRepository;
            if (repository == null)
            {
                throw new InvalidOperationException("Audit repository not available");
            }

            return await repository.GetByEntityAsync(entityName, entityId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit trail for {EntityName} {EntityId}", entityName, entityId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(AuditLogFilter filter, CancellationToken cancellationToken = default)
    {
        try
        {
            var repository = _unitOfWork.Repository<AuditLog>() as IAuditRepository;
            if (repository == null)
            {
                throw new InvalidOperationException("Audit repository not available");
            }

            var query = _unitOfWork.Repository<AuditLog>().GetAsync(
                filter: BuildFilterExpression(filter),
                orderBy: q => q.OrderByDescending(a => a.Timestamp),
                skip: (filter.Page - 1) * filter.PageSize,
                take: filter.PageSize,
                cancellationToken: cancellationToken);

            return await query;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit logs with filter");
            throw;
        }
    }

    /// <summary>
    /// Builds a filter expression from the audit log filter
    /// </summary>
    private static System.Linq.Expressions.Expression<Func<AuditLog, bool>>? BuildFilterExpression(AuditLogFilter filter)
    {
        System.Linq.Expressions.Expression<Func<AuditLog, bool>>? expression = null;

        if (!string.IsNullOrEmpty(filter.EntityName))
        {
            expression = CombineExpressions(expression, a => a.EntityName == filter.EntityName);
        }

        if (!string.IsNullOrEmpty(filter.EntityId))
        {
            expression = CombineExpressions(expression, a => a.EntityId == filter.EntityId);
        }

        if (!string.IsNullOrEmpty(filter.UserId))
        {
            expression = CombineExpressions(expression, a => a.UserId == filter.UserId);
        }

        if (filter.Action.HasValue)
        {
            expression = CombineExpressions(expression, a => a.Action == filter.Action.Value);
        }

        if (filter.StartDate.HasValue)
        {
            expression = CombineExpressions(expression, a => a.Timestamp >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            expression = CombineExpressions(expression, a => a.Timestamp <= filter.EndDate.Value);
        }

        return expression;
    }

    /// <summary>
    /// Combines two expressions with AND logic
    /// </summary>
    private static System.Linq.Expressions.Expression<Func<AuditLog, bool>>? CombineExpressions(
        System.Linq.Expressions.Expression<Func<AuditLog, bool>>? first,
        System.Linq.Expressions.Expression<Func<AuditLog, bool>> second)
    {
        if (first == null)
            return second;

        var parameter = System.Linq.Expressions.Expression.Parameter(typeof(AuditLog), "a");
        var firstBody = ReplaceParameter(first.Body, first.Parameters[0], parameter);
        var secondBody = ReplaceParameter(second.Body, second.Parameters[0], parameter);
        var combined = System.Linq.Expressions.Expression.AndAlso(firstBody, secondBody);

        return System.Linq.Expressions.Expression.Lambda<Func<AuditLog, bool>>(combined, parameter);
    }

    /// <summary>
    /// Replaces a parameter in an expression
    /// </summary>
    private static System.Linq.Expressions.Expression ReplaceParameter(
        System.Linq.Expressions.Expression expression,
        System.Linq.Expressions.ParameterExpression oldParameter,
        System.Linq.Expressions.ParameterExpression newParameter)
    {
        return new ParameterReplacer(oldParameter, newParameter).Visit(expression);
    }

    /// <summary>
    /// Expression visitor for replacing parameters
    /// </summary>
    private class ParameterReplacer : System.Linq.Expressions.ExpressionVisitor
    {
        private readonly System.Linq.Expressions.ParameterExpression _oldParameter;
        private readonly System.Linq.Expressions.ParameterExpression _newParameter;

        public ParameterReplacer(System.Linq.Expressions.ParameterExpression oldParameter, System.Linq.Expressions.ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
        }

        protected override System.Linq.Expressions.Expression VisitParameter(System.Linq.Expressions.ParameterExpression node)
        {
            return node == _oldParameter ? _newParameter : base.VisitParameter(node);
        }
    }

    /// <summary>
    /// Logs a security event
    /// </summary>
    public async Task LogSecurityEventAsync(string eventType, string description, string userId, string? ipAddress = null, string severity = "Warning", CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog
        {
            EntityName = "Security",
            EntityId = eventType,
            Action = Core.Enums.AuditAction.Custom,
            ActionDescription = description,
            UserId = userId,
            IpAddress = ipAddress,
            Severity = severity,
            Source = "SecurityService"
        };

        await LogAsync(auditLog, cancellationToken);
    }

    /// <summary>
    /// Logs a login attempt
    /// </summary>
    public async Task LogLoginAttemptAsync(string username, bool successful, string? ipAddress = null, string? userAgent = null, string? failureReason = null, CancellationToken cancellationToken = default)
    {
        var description = successful 
            ? $"Successful login for user: {username}"
            : $"Failed login attempt for user: {username}. Reason: {failureReason}";

        var auditLog = new AuditLog
        {
            EntityName = "Authentication",
            EntityId = username,
            Action = successful ? Core.Enums.AuditAction.Custom : Core.Enums.AuditAction.Custom,
            ActionDescription = description,
            UserId = successful ? username : "ANONYMOUS",
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Severity = successful ? "Information" : "Warning",
            Source = "AuthenticationService"
        };

        await LogAsync(auditLog, cancellationToken);
    }

    /// <summary>
    /// Logs a password change event
    /// </summary>
    public async Task LogPasswordChangeAsync(string userId, bool successful, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var description = successful 
            ? "Password changed successfully"
            : "Password change failed";

        var auditLog = new AuditLog
        {
            EntityName = "User",
            EntityId = userId,
            Action = Core.Enums.AuditAction.Updated,
            ActionDescription = description,
            UserId = userId,
            IpAddress = ipAddress,
            Severity = successful ? "Information" : "Warning",
            Source = "UserService"
        };

        await LogAsync(auditLog, cancellationToken);
    }

    /// <summary>
    /// Logs a permission change event
    /// </summary>
    public async Task LogPermissionChangeAsync(string targetUserId, string action, string permission, string changedBy, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var description = $"Permission {action}: {permission}";

        var auditLog = new AuditLog
        {
            EntityName = "Permission",
            EntityId = targetUserId,
            Action = Core.Enums.AuditAction.Updated,
            ActionDescription = description,
            UserId = changedBy,
            IpAddress = ipAddress,
            Severity = "Information",
            Source = "AuthorizationService"
        };

        await LogAsync(auditLog, cancellationToken);
    }

    /// <summary>
    /// Gets audit statistics for a date range
    /// </summary>
    public async Task<AuditStatistics> GetAuditStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var repository = _unitOfWork.Repository<AuditLog>() as AuditRepository;
            if (repository == null)
            {
                throw new InvalidOperationException("Audit repository not available");
            }

            return await repository.GetStatisticsAsync(startDate, endDate, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit statistics");
            throw;
        }
    }

    /// <summary>
    /// Cleans up old audit logs
    /// </summary>
    public async Task<int> CleanupOldAuditLogsAsync(int retentionDays = 365, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            var repository = _unitOfWork.Repository<AuditLog>() as AuditRepository;
            
            if (repository == null)
            {
                throw new InvalidOperationException("Audit repository not available");
            }

            var deletedCount = await repository.CleanupOldLogsAsync(cutoffDate, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Cleaned up {Count} old audit logs older than {CutoffDate}", deletedCount, cutoffDate);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old audit logs");
            throw;
        }
    }
}
