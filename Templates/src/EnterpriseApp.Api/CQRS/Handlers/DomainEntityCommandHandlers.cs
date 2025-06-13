using AutoMapper;
using Microsoft.Extensions.Logging;
using EnterpriseApp.Api.CQRS;
using EnterpriseApp.Api.CQRS.Commands;
using EnterpriseApp.Api.DTOs;
using EnterpriseApp.Core.Common;
using EnterpriseApp.Core.Interfaces;
using EnterpriseApp.Core.Models;

namespace EnterpriseApp.Api.CQRS.Handlers;

/// <summary>
/// Handler for CreateDomainEntityCommand
/// </summary>
public class CreateDomainEntityCommandHandler : ICommandHandler<CreateDomainEntityCommand, DomainEntityDto>
{
    private readonly IDomainEntityService _domainEntityService;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateDomainEntityCommandHandler> _logger;

    public CreateDomainEntityCommandHandler(
        IDomainEntityService domainEntityService,
        IMapper mapper,
        ILogger<CreateDomainEntityCommandHandler> logger)
    {
        _domainEntityService = domainEntityService ?? throw new ArgumentNullException(nameof(domainEntityService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<DomainEntityDto>> Handle(CreateDomainEntityCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating DomainEntity: {Name}", request.Name);

            var createRequest = _mapper.Map<CreateDomainEntityRequest>(request);
            var entity = await _domainEntityService.CreateAsync(createRequest, request.UserId ?? "SYSTEM", cancellationToken);

            var dto = _mapper.Map<DomainEntityDto>(entity);
            
            _logger.LogInformation("Successfully created DomainEntity with ID: {Id}", entity.Id);
            return Result<DomainEntityDto>.Success(dto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation failed for CreateDomainEntityCommand");
            return Result<DomainEntityDto>.Failure(Error.Validation("CreateDomainEntity", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create DomainEntity");
            return Result<DomainEntityDto>.Failure(Error.Failure("CreateDomainEntity.Failed", "Failed to create DomainEntity"));
        }
    }
}

/// <summary>
/// Handler for UpdateDomainEntityCommand
/// </summary>
public class UpdateDomainEntityCommandHandler : ICommandHandler<UpdateDomainEntityCommand, DomainEntityDto>
{
    private readonly IDomainEntityService _domainEntityService;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateDomainEntityCommandHandler> _logger;

    public UpdateDomainEntityCommandHandler(
        IDomainEntityService domainEntityService,
        IMapper mapper,
        ILogger<UpdateDomainEntityCommandHandler> logger)
    {
        _domainEntityService = domainEntityService ?? throw new ArgumentNullException(nameof(domainEntityService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<DomainEntityDto>> Handle(UpdateDomainEntityCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating DomainEntity with ID: {Id}", request.Id);

            var updateRequest = _mapper.Map<UpdateDomainEntityRequest>(request);
            var entity = await _domainEntityService.UpdateAsync(request.Id, updateRequest, request.UserId ?? "SYSTEM", cancellationToken);

            var dto = _mapper.Map<DomainEntityDto>(entity);
            
            _logger.LogInformation("Successfully updated DomainEntity with ID: {Id}", request.Id);
            return Result<DomainEntityDto>.Success(dto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation failed for UpdateDomainEntityCommand");
            return Result<DomainEntityDto>.Failure(Error.Validation("UpdateDomainEntity", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update DomainEntity with ID: {Id}", request.Id);
            return Result<DomainEntityDto>.Failure(Error.Failure("UpdateDomainEntity.Failed", "Failed to update DomainEntity"));
        }
    }
}

/// <summary>
/// Handler for DeleteDomainEntityCommand
/// </summary>
public class DeleteDomainEntityCommandHandler : ICommandHandler<DeleteDomainEntityCommand>
{
    private readonly IDomainEntityService _domainEntityService;
    private readonly ILogger<DeleteDomainEntityCommandHandler> _logger;

    public DeleteDomainEntityCommandHandler(
        IDomainEntityService domainEntityService,
        ILogger<DeleteDomainEntityCommandHandler> logger)
    {
        _domainEntityService = domainEntityService ?? throw new ArgumentNullException(nameof(domainEntityService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(DeleteDomainEntityCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Deleting DomainEntity with ID: {Id}", request.Id);

            await _domainEntityService.DeleteAsync(request.Id, request.UserId ?? "SYSTEM", cancellationToken);
            
            _logger.LogInformation("Successfully deleted DomainEntity with ID: {Id}", request.Id);
            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "DomainEntity not found for deletion: {Id}", request.Id);
            return Result.Failure(Error.NotFound("DomainEntity", request.Id));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot delete DomainEntity: {Id}", request.Id);
            return Result.Failure(Error.BusinessRule("CannotDelete", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete DomainEntity with ID: {Id}", request.Id);
            return Result.Failure(Error.Failure("DeleteDomainEntity.Failed", "Failed to delete DomainEntity"));
        }
    }
}

/// <summary>
/// Handler for ActivateDomainEntityCommand
/// </summary>
public class ActivateDomainEntityCommandHandler : ICommandHandler<ActivateDomainEntityCommand, DomainEntityDto>
{
    private readonly IDomainEntityService _domainEntityService;
    private readonly IMapper _mapper;
    private readonly ILogger<ActivateDomainEntityCommandHandler> _logger;

    public ActivateDomainEntityCommandHandler(
        IDomainEntityService domainEntityService,
        IMapper mapper,
        ILogger<ActivateDomainEntityCommandHandler> logger)
    {
        _domainEntityService = domainEntityService ?? throw new ArgumentNullException(nameof(domainEntityService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<DomainEntityDto>> Handle(ActivateDomainEntityCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Activating DomainEntity with ID: {Id}", request.Id);

            var entity = await _domainEntityService.ActivateAsync(request.Id, request.UserId ?? "SYSTEM", cancellationToken);
            var dto = _mapper.Map<DomainEntityDto>(entity);
            
            _logger.LogInformation("Successfully activated DomainEntity with ID: {Id}", request.Id);
            return Result<DomainEntityDto>.Success(dto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "DomainEntity not found for activation: {Id}", request.Id);
            return Result<DomainEntityDto>.Failure(Error.NotFound("DomainEntity", request.Id));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot activate DomainEntity: {Id}", request.Id);
            return Result<DomainEntityDto>.Failure(Error.BusinessRule("CannotActivate", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate DomainEntity with ID: {Id}", request.Id);
            return Result<DomainEntityDto>.Failure(Error.Failure("ActivateDomainEntity.Failed", "Failed to activate DomainEntity"));
        }
    }
}

/// <summary>
/// Handler for DeactivateDomainEntityCommand
/// </summary>
public class DeactivateDomainEntityCommandHandler : ICommandHandler<DeactivateDomainEntityCommand, DomainEntityDto>
{
    private readonly IDomainEntityService _domainEntityService;
    private readonly IMapper _mapper;
    private readonly ILogger<DeactivateDomainEntityCommandHandler> _logger;

    public DeactivateDomainEntityCommandHandler(
        IDomainEntityService domainEntityService,
        IMapper mapper,
        ILogger<DeactivateDomainEntityCommandHandler> logger)
    {
        _domainEntityService = domainEntityService ?? throw new ArgumentNullException(nameof(domainEntityService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<DomainEntityDto>> Handle(DeactivateDomainEntityCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Deactivating DomainEntity with ID: {Id}", request.Id);

            var entity = await _domainEntityService.DeactivateAsync(request.Id, request.UserId ?? "SYSTEM", cancellationToken);
            var dto = _mapper.Map<DomainEntityDto>(entity);
            
            _logger.LogInformation("Successfully deactivated DomainEntity with ID: {Id}", request.Id);
            return Result<DomainEntityDto>.Success(dto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "DomainEntity not found for deactivation: {Id}", request.Id);
            return Result<DomainEntityDto>.Failure(Error.NotFound("DomainEntity", request.Id));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate DomainEntity with ID: {Id}", request.Id);
            return Result<DomainEntityDto>.Failure(Error.Failure("DeactivateDomainEntity.Failed", "Failed to deactivate DomainEntity"));
        }
    }
}

/// <summary>
/// Handler for BulkDomainEntityCommand
/// </summary>
public class BulkDomainEntityCommandHandler : ICommandHandler<BulkDomainEntityCommand, BulkOperationResultDto>
{
    private readonly IDomainEntityService _domainEntityService;
    private readonly IMapper _mapper;
    private readonly ILogger<BulkDomainEntityCommandHandler> _logger;

    public BulkDomainEntityCommandHandler(
        IDomainEntityService domainEntityService,
        IMapper mapper,
        ILogger<BulkDomainEntityCommandHandler> logger)
    {
        _domainEntityService = domainEntityService ?? throw new ArgumentNullException(nameof(domainEntityService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<BulkOperationResultDto>> Handle(BulkDomainEntityCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing bulk operation: {Operation} for {Count} entities", 
                request.Operation, request.EntityIds.Count);

            var bulkRequest = _mapper.Map<BulkOperationRequest>(request);
            var result = await _domainEntityService.ProcessBulkOperationAsync(bulkRequest, request.UserId ?? "SYSTEM", cancellationToken);

            var dto = _mapper.Map<BulkOperationResultDto>(result);
            dto.Operation = request.Operation.ToString();
            
            _logger.LogInformation("Bulk operation completed: {SuccessCount} succeeded, {FailureCount} failed", 
                result.SuccessCount, result.FailureCount);
            
            return Result<BulkOperationResultDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process bulk operation: {Operation}", request.Operation);
            return Result<BulkOperationResultDto>.Failure(Error.Failure("BulkOperation.Failed", "Failed to process bulk operation"));
        }
    }
}
