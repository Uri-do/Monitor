using MediatR;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.DTOs;
using MonitoringGrid.Core.Interfaces;
using AutoMapper;

namespace MonitoringGrid.Api.CQRS.Commands.Contact;

/// <summary>
/// Command to create a new contact
/// </summary>
public record CreateContactCommand(
    string Name,
    string? Email,
    string? Phone,
    bool IsActive = true
) : IRequest<Result<ContactDto>>;

/// <summary>
/// Handler for creating contacts
/// </summary>
public class CreateContactCommandHandler : IRequestHandler<CreateContactCommand, Result<ContactDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateContactCommandHandler> _logger;

    public CreateContactCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreateContactCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<ContactDto>> Handle(CreateContactCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate that at least one contact method is provided
            if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Phone))
            {
                return Result.Failure<ContactDto>(Error.Validation("Contact.ValidationError", "At least one contact method (email or phone) is required"));
            }

            // Check for duplicate email if provided
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var contactRepository = _unitOfWork.Repository<Core.Entities.Contact>();
                var existingContact = await contactRepository.GetAsync(c => c.Email == request.Email);
                if (existingContact.Any())
                {
                    return Result.Failure<ContactDto>(Error.Conflict($"A contact with email '{request.Email}' already exists"));
                }
            }

            var contact = new Core.Entities.Contact
            {
                Name = request.Name.Trim(),
                Email = request.Email?.Trim(),
                Phone = request.Phone?.Trim(),
                IsActive = request.IsActive,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            var repository = _unitOfWork.Repository<Core.Entities.Contact>();
            await repository.AddAsync(contact);
            await _unitOfWork.SaveChangesAsync();

            var contactDto = _mapper.Map<ContactDto>(contact);

            _logger.LogInformation("Created contact {ContactId} with name {ContactName}", 
                contact.ContactId, contact.Name);

            return Result.Success(contactDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating contact with name {ContactName}", request.Name);
            return Result.Failure<ContactDto>(Error.Failure("Contact.CreateError", "Failed to create contact"));
        }
    }
}

/// <summary>
/// Command to update an existing contact
/// </summary>
public record UpdateContactCommand(
    int ContactId,
    string Name,
    string? Email,
    string? Phone,
    bool IsActive
) : IRequest<Result<ContactDto>>;

/// <summary>
/// Handler for updating contacts
/// </summary>
public class UpdateContactCommandHandler : IRequestHandler<UpdateContactCommand, Result<ContactDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateContactCommandHandler> _logger;

    public UpdateContactCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UpdateContactCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<ContactDto>> Handle(UpdateContactCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate that at least one contact method is provided
            if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Phone))
            {
                return Result.Failure<ContactDto>(Error.Validation("Contact.ValidationError", "At least one contact method (email or phone) is required"));
            }

            var repository = _unitOfWork.Repository<Core.Entities.Contact>();
            var contact = await repository.GetByIdAsync(request.ContactId);

            if (contact == null)
            {
                return Result.Failure<ContactDto>(Error.NotFound("Contact", request.ContactId));
            }

            // Check for duplicate email if email is being changed
            if (!string.IsNullOrWhiteSpace(request.Email) && 
                !string.Equals(contact.Email, request.Email, StringComparison.OrdinalIgnoreCase))
            {
                var existingContact = await repository.GetAsync(c => c.Email == request.Email && c.ContactId != request.ContactId);
                if (existingContact.Any())
                {
                    return Result.Failure<ContactDto>(Error.Conflict($"A contact with email '{request.Email}' already exists"));
                }
            }

            // Update contact properties
            contact.Name = request.Name.Trim();
            contact.Email = request.Email?.Trim();
            contact.Phone = request.Phone?.Trim();
            contact.IsActive = request.IsActive;
            contact.ModifiedDate = DateTime.UtcNow;

            await repository.UpdateAsync(contact);
            await _unitOfWork.SaveChangesAsync();

            var contactDto = _mapper.Map<ContactDto>(contact);

            _logger.LogInformation("Updated contact {ContactId} with name {ContactName}", 
                contact.ContactId, contact.Name);

            return Result.Success(contactDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating contact {ContactId}", request.ContactId);
            return Result.Failure<ContactDto>(Error.Failure("Contact.UpdateError", "Failed to update contact"));
        }
    }
}

/// <summary>
/// Command to delete a contact
/// </summary>
public record DeleteContactCommand(int ContactId) : IRequest<Result>;

/// <summary>
/// Handler for deleting contacts
/// </summary>
public class DeleteContactCommandHandler : IRequestHandler<DeleteContactCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteContactCommandHandler> _logger;

    public DeleteContactCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteContactCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteContactCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var repository = _unitOfWork.Repository<Core.Entities.Contact>();
            var contact = await repository.GetByIdAsync(request.ContactId);

            if (contact == null)
            {
                return Result.Failure(Error.NotFound("Contact", request.ContactId));
            }

            // Check if contact is associated with any indicators
            var indicatorContactRepository = _unitOfWork.Repository<Core.Entities.IndicatorContact>();
            var indicatorContacts = await indicatorContactRepository.GetAsync(ic => ic.ContactId == request.ContactId);

            if (indicatorContacts.Any())
            {
                return Result.Failure(Error.BusinessRule("Contact.HasIndicators", "Cannot delete contact that is associated with indicators. Remove indicator associations first."));
            }

            await repository.DeleteAsync(contact);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Deleted contact {ContactId} with name {ContactName}", 
                contact.ContactId, contact.Name);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting contact {ContactId}", request.ContactId);
            return Result.Failure(Error.Failure("Contact.DeleteError", "Failed to delete contact"));
        }
    }
}
