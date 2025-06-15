using MediatR;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace MonitoringGrid.Api.CQRS.Queries.Contact;

/// <summary>
/// Query to get contacts with optional filtering
/// </summary>
public record GetContactsQuery(
    bool? IsActive = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<Result<IEnumerable<ContactDto>>>;

/// <summary>
/// Handler for getting contacts
/// </summary>
public class GetContactsQueryHandler : IRequestHandler<GetContactsQuery, Result<IEnumerable<ContactDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetContactsQueryHandler> _logger;

    public GetContactsQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetContactsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<ContactDto>>> Handle(GetContactsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var repository = _unitOfWork.Repository<Core.Entities.Contact>();
            var query = repository.GetQueryable();

            // Apply filters
            if (request.IsActive.HasValue)
            {
                query = query.Where(c => c.IsActive == request.IsActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchTerm = request.Search.Trim().ToLowerInvariant();
                query = query.Where(c => 
                    c.Name.ToLower().Contains(searchTerm) ||
                    (c.Email != null && c.Email.ToLower().Contains(searchTerm)) ||
                    (c.Phone != null && c.Phone.Contains(searchTerm)));
            }

            // Apply ordering
            query = query.OrderBy(c => c.Name);

            // Apply pagination
            var skip = (request.Page - 1) * request.PageSize;
            query = query.Skip(skip).Take(request.PageSize);

            // Include related data
            query = query.Include(c => c.IndicatorContacts)
                        .ThenInclude(ic => ic.Indicator);

            var contacts = await query.ToListAsync(cancellationToken);
            var contactDtos = _mapper.Map<IEnumerable<ContactDto>>(contacts);

            _logger.LogDebug("Retrieved {Count} contacts with filters - IsActive: {IsActive}, Search: {Search}", 
                contacts.Count, request.IsActive, request.Search);

            return Result<IEnumerable<ContactDto>>.Success(contactDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contacts with filters - IsActive: {IsActive}, Search: {Search}", 
                request.IsActive, request.Search);
            return Result<IEnumerable<ContactDto>>.Failure("Contact.RetrievalError", "Failed to retrieve contacts");
        }
    }
}

/// <summary>
/// Query to get a contact by ID
/// </summary>
public record GetContactByIdQuery(int ContactId) : IRequest<Result<ContactDto>>;

/// <summary>
/// Handler for getting a contact by ID
/// </summary>
public class GetContactByIdQueryHandler : IRequestHandler<GetContactByIdQuery, Result<ContactDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetContactByIdQueryHandler> _logger;

    public GetContactByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetContactByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<ContactDto>> Handle(GetContactByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var repository = _unitOfWork.Repository<Core.Entities.Contact>();
            var contact = await repository.GetQueryable()
                .Include(c => c.IndicatorContacts)
                    .ThenInclude(ic => ic.Indicator)
                .FirstOrDefaultAsync(c => c.ContactId == request.ContactId, cancellationToken);

            if (contact == null)
            {
                return Result<ContactDto>.Failure("Contact.NotFound", $"Contact with ID {request.ContactId} not found");
            }

            var contactDto = _mapper.Map<ContactDto>(contact);

            _logger.LogDebug("Retrieved contact {ContactId} with name {ContactName}", 
                contact.ContactId, contact.Name);

            return Result<ContactDto>.Success(contactDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contact {ContactId}", request.ContactId);
            return Result<ContactDto>.Failure("Failed to retrieve contact");
        }
    }
}

/// <summary>
/// Query to get paginated contacts
/// </summary>
public record GetPaginatedContactsQuery(
    bool? IsActive = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PaginatedContactsDto>>;

/// <summary>
/// Paginated contacts result
/// </summary>
public class PaginatedContactsDto
{
    public IEnumerable<ContactDto> Contacts { get; set; } = new List<ContactDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

/// <summary>
/// Handler for getting paginated contacts
/// </summary>
public class GetPaginatedContactsQueryHandler : IRequestHandler<GetPaginatedContactsQuery, Result<PaginatedContactsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetPaginatedContactsQueryHandler> _logger;

    public GetPaginatedContactsQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetPaginatedContactsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<PaginatedContactsDto>> Handle(GetPaginatedContactsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var repository = _unitOfWork.Repository<Core.Entities.Contact>();
            var query = repository.GetQueryable();

            // Apply filters
            if (request.IsActive.HasValue)
            {
                query = query.Where(c => c.IsActive == request.IsActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchTerm = request.Search.Trim().ToLowerInvariant();
                query = query.Where(c => 
                    c.Name.ToLower().Contains(searchTerm) ||
                    (c.Email != null && c.Email.ToLower().Contains(searchTerm)) ||
                    (c.Phone != null && c.Phone.Contains(searchTerm)));
            }

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply ordering and pagination
            query = query.OrderBy(c => c.Name);
            var skip = (request.Page - 1) * request.PageSize;
            query = query.Skip(skip).Take(request.PageSize);

            // Include related data
            query = query.Include(c => c.IndicatorContacts)
                        .ThenInclude(ic => ic.Indicator);

            var contacts = await query.ToListAsync(cancellationToken);
            var contactDtos = _mapper.Map<IEnumerable<ContactDto>>(contacts);

            var result = new PaginatedContactsDto
            {
                Contacts = contactDtos,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize),
                HasNextPage = request.Page * request.PageSize < totalCount,
                HasPreviousPage = request.Page > 1
            };

            _logger.LogDebug("Retrieved {Count} of {TotalCount} contacts (page {Page}/{TotalPages})", 
                contacts.Count, totalCount, request.Page, result.TotalPages);

            return Result<PaginatedContactsDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated contacts");
            return Result<PaginatedContactsDto>.Failure("Contact.RetrievalError", "Failed to retrieve contacts");
        }
    }
}
