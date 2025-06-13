using AutoMapper;
using Microsoft.Extensions.Logging;
using EnterpriseApp.Api.CQRS;
using EnterpriseApp.Api.CQRS.Queries;
using EnterpriseApp.Api.DTOs;
using EnterpriseApp.Core.Common;
using EnterpriseApp.Core.Interfaces;
using EnterpriseApp.Core.Models;

namespace EnterpriseApp.Api.CQRS.Handlers;

/// <summary>
/// Handler for GetDomainEntityByIdQuery
/// </summary>
public class GetDomainEntityByIdQueryHandler : IQueryHandler<GetDomainEntityByIdQuery, DomainEntityDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetDomainEntityByIdQueryHandler> _logger;

    public GetDomainEntityByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService,
        ILogger<GetDomainEntityByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<DomainEntityDto>> Handle(GetDomainEntityByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting DomainEntity by ID: {Id}", request.Id);

            // Check cache first
            var cacheKey = CacheKeys.DomainEntity(request.Id);
            if (request.UseCache)
            {
                var cachedEntity = await _cacheService.GetAsync<DomainEntityDto>(cacheKey, cancellationToken);
                if (cachedEntity != null)
                {
                    _logger.LogDebug("DomainEntity found in cache: {Id}", request.Id);
                    return Result<DomainEntityDto>.Success(cachedEntity);
                }
            }

            var repository = _unitOfWork.Repository<Core.Entities.DomainEntity>();
            var entity = await repository.GetByIdAsync(request.Id, cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("DomainEntity not found: {Id}", request.Id);
                return Result<DomainEntityDto>.Failure(Error.NotFound("DomainEntity", request.Id));
            }

            // Load related data if requested
            if (request.IncludeItems || request.IncludeAuditLogs)
            {
                var includes = new List<string>();
                if (request.IncludeItems) includes.Add("Items");
                if (request.IncludeAuditLogs) includes.Add("AuditLogs");

                var entityWithIncludes = await repository.GetAsync(
                    filter: e => e.Id == request.Id,
                    includeProperties: string.Join(",", includes),
                    cancellationToken: cancellationToken);

                entity = entityWithIncludes.FirstOrDefault();
            }

            var dto = _mapper.Map<DomainEntityDto>(entity);

            // Cache the result
            if (request.UseCache)
            {
                var expiration = request.CacheExpiration ?? TimeSpan.FromMinutes(30);
                await _cacheService.SetAsync(cacheKey, dto, expiration, cancellationToken);
            }

            _logger.LogInformation("Successfully retrieved DomainEntity: {Id}", request.Id);
            return Result<DomainEntityDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get DomainEntity by ID: {Id}", request.Id);
            return Result<DomainEntityDto>.Failure(Error.Failure("GetDomainEntity.Failed", "Failed to retrieve DomainEntity"));
        }
    }
}

/// <summary>
/// Handler for GetDomainEntitiesQuery
/// </summary>
public class GetDomainEntitiesQueryHandler : IQueryHandler<GetDomainEntitiesQuery, PagedResult<DomainEntityDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetDomainEntitiesQueryHandler> _logger;

    public GetDomainEntitiesQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService,
        ILogger<GetDomainEntitiesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<PagedResult<DomainEntityDto>>> Handle(GetDomainEntitiesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting DomainEntities with pagination: Page {Page}, Size {PageSize}", request.Page, request.PageSize);

            // Validate pagination
            var validationResult = request.ValidatePagination();
            if (validationResult.IsFailure)
            {
                return Result<PagedResult<DomainEntityDto>>.Failure(validationResult.Error);
            }

            // Check cache
            var cacheKey = CacheKeys.Build("domainentities", "paged", 
                request.Page.ToString(), request.PageSize.ToString(), 
                request.Category ?? "all", request.Status?.ToString() ?? "all",
                request.IsActive?.ToString() ?? "all", request.SearchTerm ?? "");

            if (request.UseCache)
            {
                var cachedResult = await _cacheService.GetAsync<PagedResult<DomainEntityDto>>(cacheKey, cancellationToken);
                if (cachedResult != null)
                {
                    _logger.LogDebug("Paged DomainEntities found in cache");
                    return Result<PagedResult<DomainEntityDto>>.Success(cachedResult);
                }
            }

            var repository = _unitOfWork.Repository<Core.Entities.DomainEntity>() as IDomainEntityRepository;
            if (repository == null)
            {
                throw new InvalidOperationException("DomainEntity repository not available");
            }

            var (entities, totalCount) = await repository.GetPagedWithFilterAsync(
                request.Page,
                request.PageSize,
                request.SearchTerm,
                request.Category,
                request.Status,
                request.IsActive,
                cancellationToken);

            var dtos = _mapper.Map<IEnumerable<DomainEntityDto>>(entities);
            var pagedResult = PagedResult<DomainEntityDto>.Create(dtos, request.Page, request.PageSize, totalCount);

            // Cache the result
            if (request.UseCache)
            {
                var expiration = request.CacheExpiration ?? TimeSpan.FromMinutes(15);
                await _cacheService.SetAsync(cacheKey, pagedResult, expiration, cancellationToken);
            }

            _logger.LogInformation("Successfully retrieved {Count} DomainEntities (Total: {Total})", 
                dtos.Count(), totalCount);
            
            return Result<PagedResult<DomainEntityDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get paged DomainEntities");
            return Result<PagedResult<DomainEntityDto>>.Failure(Error.Failure("GetDomainEntities.Failed", "Failed to retrieve DomainEntities"));
        }
    }
}

/// <summary>
/// Handler for GetActiveDomainEntitiesQuery
/// </summary>
public class GetActiveDomainEntitiesQueryHandler : IQueryHandler<GetActiveDomainEntitiesQuery, List<DomainEntityDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetActiveDomainEntitiesQueryHandler> _logger;

    public GetActiveDomainEntitiesQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService,
        ILogger<GetActiveDomainEntitiesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<List<DomainEntityDto>>> Handle(GetActiveDomainEntitiesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting active DomainEntities");

            // Check cache
            var cacheKey = CacheKeys.DomainEntityList(request.Category, true);
            if (request.UseCache)
            {
                var cachedEntities = await _cacheService.GetAsync<List<DomainEntityDto>>(cacheKey, cancellationToken);
                if (cachedEntities != null)
                {
                    _logger.LogDebug("Active DomainEntities found in cache");
                    
                    // Apply limit if specified
                    if (request.Limit.HasValue && cachedEntities.Count > request.Limit.Value)
                    {
                        cachedEntities = cachedEntities.Take(request.Limit.Value).ToList();
                    }
                    
                    return Result<List<DomainEntityDto>>.Success(cachedEntities);
                }
            }

            var repository = _unitOfWork.Repository<Core.Entities.DomainEntity>() as IDomainEntityRepository;
            if (repository == null)
            {
                throw new InvalidOperationException("DomainEntity repository not available");
            }

            IEnumerable<Core.Entities.DomainEntity> entities;

            if (!string.IsNullOrEmpty(request.Category))
            {
                entities = await repository.GetByCategoryAsync(request.Category, cancellationToken);
                entities = entities.Where(e => e.IsActive);
            }
            else
            {
                entities = await repository.GetActiveAsync(cancellationToken);
            }

            // Apply limit if specified
            if (request.Limit.HasValue)
            {
                entities = entities.Take(request.Limit.Value);
            }

            var dtos = _mapper.Map<List<DomainEntityDto>>(entities);

            // Cache the result
            if (request.UseCache)
            {
                var expiration = request.CacheExpiration ?? TimeSpan.FromMinutes(20);
                await _cacheService.SetAsync(cacheKey, dtos, expiration, cancellationToken);
            }

            _logger.LogInformation("Successfully retrieved {Count} active DomainEntities", dtos.Count);
            return Result<List<DomainEntityDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active DomainEntities");
            return Result<List<DomainEntityDto>>.Failure(Error.Failure("GetActiveDomainEntities.Failed", "Failed to retrieve active DomainEntities"));
        }
    }
}

/// <summary>
/// Handler for SearchDomainEntitiesQuery
/// </summary>
public class SearchDomainEntitiesQueryHandler : IQueryHandler<SearchDomainEntitiesQuery, PagedResult<DomainEntityDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<SearchDomainEntitiesQueryHandler> _logger;

    public SearchDomainEntitiesQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<SearchDomainEntitiesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<PagedResult<DomainEntityDto>>> Handle(SearchDomainEntitiesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Searching DomainEntities with term: {SearchTerm}", request.SearchTerm);

            // Validate pagination
            var validationResult = request.ValidatePagination();
            if (validationResult.IsFailure)
            {
                return Result<PagedResult<DomainEntityDto>>.Failure(validationResult.Error);
            }

            var repository = _unitOfWork.Repository<Core.Entities.DomainEntity>() as IDomainEntityRepository;
            if (repository == null)
            {
                throw new InvalidOperationException("DomainEntity repository not available");
            }

            var entities = await repository.SearchAsync(request.SearchTerm, cancellationToken);

            // Apply additional filters
            if (!string.IsNullOrEmpty(request.Category))
            {
                entities = entities.Where(e => e.Category == request.Category);
            }

            if (request.Status.HasValue)
            {
                entities = entities.Where(e => e.Status == request.Status.Value);
            }

            if (request.IsActive.HasValue)
            {
                entities = entities.Where(e => e.IsActive == request.IsActive.Value);
            }

            // Apply pagination
            var totalCount = entities.Count();
            var pagedEntities = entities
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize);

            var dtos = _mapper.Map<IEnumerable<DomainEntityDto>>(pagedEntities);
            var pagedResult = PagedResult<DomainEntityDto>.Create(dtos, request.Page, request.PageSize, totalCount);

            _logger.LogInformation("Search returned {Count} results (Total: {Total}) for term: {SearchTerm}", 
                dtos.Count(), totalCount, request.SearchTerm);
            
            return Result<PagedResult<DomainEntityDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search DomainEntities with term: {SearchTerm}", request.SearchTerm);
            return Result<PagedResult<DomainEntityDto>>.Failure(Error.Failure("SearchDomainEntities.Failed", "Failed to search DomainEntities"));
        }
    }
}

/// <summary>
/// Handler for GetDomainEntityStatisticsQuery
/// </summary>
public class GetDomainEntityStatisticsQueryHandler : IQueryHandler<GetDomainEntityStatisticsQuery, DomainEntityStatisticsDto>
{
    private readonly IDomainEntityService _domainEntityService;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetDomainEntityStatisticsQueryHandler> _logger;

    public GetDomainEntityStatisticsQueryHandler(
        IDomainEntityService domainEntityService,
        IMapper mapper,
        ICacheService cacheService,
        ILogger<GetDomainEntityStatisticsQueryHandler> logger)
    {
        _domainEntityService = domainEntityService ?? throw new ArgumentNullException(nameof(domainEntityService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<DomainEntityStatisticsDto>> Handle(GetDomainEntityStatisticsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting DomainEntity statistics");

            // Check cache
            var cacheKey = CacheKeys.Statistics("domainentity", DateTime.UtcNow.Date);
            if (request.UseCache)
            {
                var cachedStats = await _cacheService.GetAsync<DomainEntityStatisticsDto>(cacheKey, cancellationToken);
                if (cachedStats != null)
                {
                    _logger.LogDebug("DomainEntity statistics found in cache");
                    return Result<DomainEntityStatisticsDto>.Success(cachedStats);
                }
            }

            var statistics = await _domainEntityService.GetStatisticsAsync(cancellationToken);
            var dto = _mapper.Map<DomainEntityStatisticsDto>(statistics);

            // Cache the result
            if (request.UseCache)
            {
                var expiration = request.CacheExpiration ?? TimeSpan.FromHours(1);
                await _cacheService.SetAsync(cacheKey, dto, expiration, cancellationToken);
            }

            _logger.LogInformation("Successfully retrieved DomainEntity statistics");
            return Result<DomainEntityStatisticsDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get DomainEntity statistics");
            return Result<DomainEntityStatisticsDto>.Failure(Error.Failure("GetDomainEntityStatistics.Failed", "Failed to retrieve statistics"));
        }
    }
}

/// <summary>
/// Handler for GetDomainEntityCategoriesQuery
/// </summary>
public class GetDomainEntityCategoriesQueryHandler : IQueryHandler<GetDomainEntityCategoriesQuery, List<string>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetDomainEntityCategoriesQueryHandler> _logger;

    public GetDomainEntityCategoriesQueryHandler(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<GetDomainEntityCategoriesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<List<string>>> Handle(GetDomainEntityCategoriesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting DomainEntity categories");

            // Check cache
            var cacheKey = CacheKeys.Build("domainentity", "categories", request.ActiveOnly.ToString());
            if (request.UseCache)
            {
                var cachedCategories = await _cacheService.GetAsync<List<string>>(cacheKey, cancellationToken);
                if (cachedCategories != null)
                {
                    _logger.LogDebug("DomainEntity categories found in cache");
                    return Result<List<string>>.Success(cachedCategories);
                }
            }

            var repository = _unitOfWork.Repository<Core.Entities.DomainEntity>() as IDomainEntityRepository;
            if (repository == null)
            {
                throw new InvalidOperationException("DomainEntity repository not available");
            }

            var categories = await repository.GetCategoriesAsync(cancellationToken);
            var categoriesList = categories.ToList();

            // Cache the result
            if (request.UseCache)
            {
                var expiration = request.CacheExpiration ?? TimeSpan.FromHours(2);
                await _cacheService.SetAsync(cacheKey, categoriesList, expiration, cancellationToken);
            }

            _logger.LogInformation("Successfully retrieved {Count} categories", categoriesList.Count);
            return Result<List<string>>.Success(categoriesList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get DomainEntity categories");
            return Result<List<string>>.Failure(Error.Failure("GetDomainEntityCategories.Failed", "Failed to retrieve categories"));
        }
    }
}
