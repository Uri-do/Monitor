namespace EnterpriseApp.Core.Enums;

/// <summary>
/// Status of a DomainEntity
/// </summary>
public enum DomainEntityStatus
{
    /// <summary>
    /// DomainEntity is in draft state
    /// </summary>
    Draft = 0,

    /// <summary>
    /// DomainEntity is ready for activation
    /// </summary>
    Ready = 1,

    /// <summary>
    /// DomainEntity is active and operational
    /// </summary>
    Active = 2,

    /// <summary>
    /// DomainEntity is temporarily inactive
    /// </summary>
    Inactive = 3,

    /// <summary>
    /// DomainEntity is archived
    /// </summary>
    Archived = 4,

    /// <summary>
    /// DomainEntity is deleted (soft delete)
    /// </summary>
    Deleted = 5
}

/// <summary>
/// Priority levels for DomainEntity
/// </summary>
public enum Priority
{
    /// <summary>
    /// Highest priority
    /// </summary>
    Critical = 1,

    /// <summary>
    /// High priority
    /// </summary>
    High = 2,

    /// <summary>
    /// Normal priority
    /// </summary>
    Normal = 3,

    /// <summary>
    /// Low priority
    /// </summary>
    Low = 4,

    /// <summary>
    /// Lowest priority
    /// </summary>
    Minimal = 5
}

/// <summary>
/// Audit action types
/// </summary>
public enum AuditAction
{
    /// <summary>
    /// Entity was created
    /// </summary>
    Created = 0,

    /// <summary>
    /// Entity was updated
    /// </summary>
    Updated = 1,

    /// <summary>
    /// Entity was deleted
    /// </summary>
    Deleted = 2,

    /// <summary>
    /// Entity was activated
    /// </summary>
    Activated = 3,

    /// <summary>
    /// Entity was deactivated
    /// </summary>
    Deactivated = 4,

    /// <summary>
    /// Entity was archived
    /// </summary>
    Archived = 5,

    /// <summary>
    /// Entity was restored
    /// </summary>
    Restored = 6,

    /// <summary>
    /// Custom action
    /// </summary>
    Custom = 99
}
