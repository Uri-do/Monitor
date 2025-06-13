using Microsoft.Extensions.Logging;
using EnterpriseApp.Core.Entities;
using EnterpriseApp.Core.Enums;
using EnterpriseApp.Core.Interfaces;
using EnterpriseApp.Core.Models;
using EnterpriseApp.Core.Common;

namespace EnterpriseApp.Infrastructure.Services;

/// <summary>
/// Domain service implementation for DomainEntity business logic
/// </summary>
public class DomainEntityService : IDomainEntityService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ILogger<DomainEntityService> _logger;

    /// <summary>
    /// Initializes a new instance of the DomainEntityService class
    /// </summary>
    public DomainEntityService(
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ILogger<DomainEntityService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<DomainEntity> CreateAsync(CreateDomainEntityRequest request, string createdBy, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new DomainEntity: {Name}", request.Name);

        // Validate the request
        var validationResult = await ValidateCreateRequestAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.Message));
            _logger.LogWarning("DomainEntity creation validation failed: {Errors}", errors);
            throw new ArgumentException($"Validation failed: {errors}");
        }

        var entity = new DomainEntity
        {
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            Priority = request.Priority,
            Tags = request.Tags,
            ExternalId = request.ExternalId,
            Metadata = request.Metadata,
            Status = DomainEntityStatus.Draft,
            IsActive = true,
            CreatedBy = createdBy,
            ModifiedBy = createdBy
        };

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var repository = _unitOfWork.Repository<DomainEntity>();
                await repository.AddAsync(entity, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Log audit event
                await _auditService.LogCreationAsync(
                    nameof(DomainEntity),
                    entity.Id.ToString(),
                    createdBy,
                    cancellationToken: cancellationToken);

            }, cancellationToken);

            _logger.LogInformation("Successfully created DomainEntity with ID: {Id}", entity.Id);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create DomainEntity: {Name}", request.Name);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DomainEntity> UpdateAsync(int id, UpdateDomainEntityRequest request, string modifiedBy, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating DomainEntity with ID: {Id}", id);

        var repository = _unitOfWork.Repository<DomainEntity>();
        var entity = await repository.GetByIdAsync(id, cancellationToken);

        if (entity == null)
        {
            _logger.LogWarning("DomainEntity with ID {Id} not found", id);
            throw new ArgumentException($"DomainEntity with ID {id} not found");
        }

        // Store old values for audit
        var oldValues = new
        {
            entity.Name,
            entity.Description,
            entity.Category,
            entity.Priority,
            entity.Status,
            entity.Tags,
            entity.ExternalId,
            entity.Metadata
        };

        // Validate the request
        var validationResult = await ValidateUpdateRequestAsync(entity, request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.Message));
            _logger.LogWarning("DomainEntity update validation failed: {Errors}", errors);
            throw new ArgumentException($"Validation failed: {errors}");
        }

        // Update entity
        entity.Update(request.Name, request.Description, request.Category, request.Priority, modifiedBy);
        entity.Status = request.Status;
        entity.Tags = request.Tags;
        entity.ExternalId = request.ExternalId;
        entity.Metadata = request.Metadata;

        var newValues = new
        {
            entity.Name,
            entity.Description,
            entity.Category,
            entity.Priority,
            entity.Status,
            entity.Tags,
            entity.ExternalId,
            entity.Metadata
        };

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await repository.UpdateAsync(entity, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Log audit event
                await _auditService.LogUpdateAsync(
                    nameof(DomainEntity),
                    entity.Id.ToString(),
                    modifiedBy,
                    oldValues,
                    newValues,
                    cancellationToken: cancellationToken);

            }, cancellationToken);

            _logger.LogInformation("Successfully updated DomainEntity with ID: {Id}", id);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update DomainEntity with ID: {Id}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting DomainEntity with ID: {Id}", id);

        var repository = _unitOfWork.Repository<DomainEntity>();
        var entity = await repository.GetByIdAsync(id, cancellationToken);

        if (entity == null)
        {
            _logger.LogWarning("DomainEntity with ID {Id} not found", id);
            throw new ArgumentException($"DomainEntity with ID {id} not found");
        }

        // Check if entity can be deleted
        if (!await CanDeleteAsync(entity, cancellationToken))
        {
            _logger.LogWarning("DomainEntity with ID {Id} cannot be deleted due to business rules", id);
            throw new InvalidOperationException("DomainEntity cannot be deleted due to business constraints");
        }

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Soft delete - mark as deleted instead of removing from database
                entity.Status = DomainEntityStatus.Deleted;
                entity.IsActive = false;
                entity.ModifiedBy = deletedBy;
                entity.ModifiedDate = DateTime.UtcNow;

                await repository.UpdateAsync(entity, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Log audit event
                await _auditService.LogDeletionAsync(
                    nameof(DomainEntity),
                    entity.Id.ToString(),
                    deletedBy,
                    cancellationToken: cancellationToken);

            }, cancellationToken);

            _logger.LogInformation("Successfully deleted DomainEntity with ID: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete DomainEntity with ID: {Id}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DomainEntity> ActivateAsync(int id, string activatedBy, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Activating DomainEntity with ID: {Id}", id);

        var repository = _unitOfWork.Repository<DomainEntity>();
        var entity = await repository.GetByIdAsync(id, cancellationToken);

        if (entity == null)
        {
            _logger.LogWarning("DomainEntity with ID {Id} not found", id);
            throw new ArgumentException($"DomainEntity with ID {id} not found");
        }

        if (!entity.CanActivate())
        {
            _logger.LogWarning("DomainEntity with ID {Id} cannot be activated in its current state", id);
            throw new InvalidOperationException("DomainEntity cannot be activated in its current state");
        }

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                entity.Activate(activatedBy);
                await repository.UpdateAsync(entity, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Log audit event
                await _auditService.LogCustomAsync(
                    nameof(DomainEntity),
                    entity.Id.ToString(),
                    "DomainEntity activated",
                    activatedBy,
                    cancellationToken: cancellationToken);

            }, cancellationToken);

            _logger.LogInformation("Successfully activated DomainEntity with ID: {Id}", id);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate DomainEntity with ID: {Id}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DomainEntity> DeactivateAsync(int id, string deactivatedBy, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deactivating DomainEntity with ID: {Id}", id);

        var repository = _unitOfWork.Repository<DomainEntity>();
        var entity = await repository.GetByIdAsync(id, cancellationToken);

        if (entity == null)
        {
            _logger.LogWarning("DomainEntity with ID {Id} not found", id);
            throw new ArgumentException($"DomainEntity with ID {id} not found");
        }

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                entity.Deactivate(deactivatedBy);
                await repository.UpdateAsync(entity, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Log audit event
                await _auditService.LogCustomAsync(
                    nameof(DomainEntity),
                    entity.Id.ToString(),
                    "DomainEntity deactivated",
                    deactivatedBy,
                    cancellationToken: cancellationToken);

            }, cancellationToken);

            _logger.LogInformation("Successfully deactivated DomainEntity with ID: {Id}", id);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate DomainEntity with ID: {Id}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DomainEntityStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting DomainEntity statistics");

        try
        {
            var repository = _unitOfWork.Repository<DomainEntity>() as IDomainEntityRepository;
            if (repository == null)
            {
                throw new InvalidOperationException("DomainEntity repository not available");
            }

            var statistics = await repository.GetStatisticsAsync(cancellationToken);
            _logger.LogInformation("Successfully retrieved DomainEntity statistics");
            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get DomainEntity statistics");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidateAsync(DomainEntity entity, CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationError>();
        var warnings = new List<ValidationWarning>();

        // Validate name
        if (string.IsNullOrWhiteSpace(entity.Name))
        {
            errors.Add(new ValidationError
            {
                PropertyName = nameof(entity.Name),
                Message = "Name is required",
                Code = "REQUIRED"
            });
        }
        else if (entity.Name.Length > 200)
        {
            errors.Add(new ValidationError
            {
                PropertyName = nameof(entity.Name),
                Message = "Name cannot exceed 200 characters",
                Code = "MAX_LENGTH"
            });
        }

        // Validate priority
        if (entity.Priority < 1 || entity.Priority > 5)
        {
            errors.Add(new ValidationError
            {
                PropertyName = nameof(entity.Priority),
                Message = "Priority must be between 1 and 5",
                Code = "RANGE"
            });
        }

        // Check for duplicate names
        var repository = _unitOfWork.Repository<DomainEntity>();
        var existingEntity = await repository.FirstOrDefaultAsync(
            e => e.Name == entity.Name && e.Id != entity.Id && e.Status != DomainEntityStatus.Deleted,
            cancellationToken);

        if (existingEntity != null)
        {
            warnings.Add(new ValidationWarning
            {
                PropertyName = nameof(entity.Name),
                Message = "Another entity with the same name already exists",
                Code = "DUPLICATE_NAME"
            });
        }

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors,
            Warnings = warnings
        };
    }

    /// <inheritdoc />
    public async Task<BulkOperationResult> ProcessBulkOperationAsync(BulkOperationRequest request, string processedBy, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing bulk operation: {Operation} for {Count} entities", request.Operation, request.EntityIds.Count);

        var result = new BulkOperationResult();
        var repository = _unitOfWork.Repository<DomainEntity>();

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                foreach (var entityId in request.EntityIds)
                {
                    try
                    {
                        var entity = await repository.GetByIdAsync(entityId, cancellationToken);
                        if (entity == null)
                        {
                            result.Errors.Add(new BulkOperationError
                            {
                                EntityId = entityId,
                                Message = "Entity not found",
                                Code = "NOT_FOUND"
                            });
                            result.FailureCount++;
                            continue;
                        }

                        switch (request.Operation)
                        {
                            case BulkOperationType.Activate:
                                if (entity.CanActivate())
                                {
                                    entity.Activate(processedBy);
                                    result.SuccessCount++;
                                }
                                else
                                {
                                    result.Errors.Add(new BulkOperationError
                                    {
                                        EntityId = entityId,
                                        Message = "Cannot activate entity in current state",
                                        Code = "INVALID_STATE"
                                    });
                                    result.FailureCount++;
                                }
                                break;

                            case BulkOperationType.Deactivate:
                                entity.Deactivate(processedBy);
                                result.SuccessCount++;
                                break;

                            case BulkOperationType.Delete:
                                if (await CanDeleteAsync(entity, cancellationToken))
                                {
                                    entity.Status = DomainEntityStatus.Deleted;
                                    entity.IsActive = false;
                                    entity.ModifiedBy = processedBy;
                                    entity.ModifiedDate = DateTime.UtcNow;
                                    result.SuccessCount++;
                                }
                                else
                                {
                                    result.Errors.Add(new BulkOperationError
                                    {
                                        EntityId = entityId,
                                        Message = "Cannot delete entity due to business constraints",
                                        Code = "CANNOT_DELETE"
                                    });
                                    result.FailureCount++;
                                }
                                break;

                            default:
                                result.Errors.Add(new BulkOperationError
                                {
                                    EntityId = entityId,
                                    Message = "Unsupported operation",
                                    Code = "UNSUPPORTED_OPERATION"
                                });
                                result.FailureCount++;
                                break;
                        }

                        await repository.UpdateAsync(entity, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing entity {EntityId} in bulk operation", entityId);
                        result.Errors.Add(new BulkOperationError
                        {
                            EntityId = entityId,
                            Message = ex.Message,
                            Code = "PROCESSING_ERROR"
                        });
                        result.FailureCount++;
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Log bulk operation audit event
                await _auditService.LogCustomAsync(
                    "BulkOperation",
                    request.Operation.ToString(),
                    $"Bulk {request.Operation} operation: {result.SuccessCount} succeeded, {result.FailureCount} failed",
                    processedBy,
                    cancellationToken: cancellationToken);

            }, cancellationToken);

            _logger.LogInformation("Bulk operation completed: {SuccessCount} succeeded, {FailureCount} failed", 
                result.SuccessCount, result.FailureCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process bulk operation");
            throw;
        }
    }

    /// <summary>
    /// Validates a create request
    /// </summary>
    private async Task<ValidationResult> ValidateCreateRequestAsync(CreateDomainEntityRequest request, CancellationToken cancellationToken)
    {
        var entity = new DomainEntity
        {
            Name = request.Name,
            Priority = request.Priority
        };

        return await ValidateAsync(entity, cancellationToken);
    }

    /// <summary>
    /// Validates an update request
    /// </summary>
    private async Task<ValidationResult> ValidateUpdateRequestAsync(DomainEntity entity, UpdateDomainEntityRequest request, CancellationToken cancellationToken)
    {
        entity.Name = request.Name;
        entity.Priority = request.Priority;

        return await ValidateAsync(entity, cancellationToken);
    }

    /// <summary>
    /// Checks if an entity can be deleted
    /// </summary>
    private async Task<bool> CanDeleteAsync(DomainEntity entity, CancellationToken cancellationToken)
    {
        // Add business rules for deletion
        // For example, check if entity has active items
        if (entity.Items.Any(i => i.IsActive))
        {
            return false;
        }

        // Add more business rules as needed
        return true;
    }
}
