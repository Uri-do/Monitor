using FluentValidation;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Api.Validation;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.Validators;

/// <summary>
/// Validator for CreateKpiRequest with comprehensive validation rules
/// </summary>
public class CreateKpiRequestValidator : AbstractValidator<CreateKpiRequest>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateKpiRequestValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;

        RuleFor(x => x.Indicator)
            .NotEmpty().WithMessage("Indicator is required")
            .MaximumLength(255).WithMessage("Indicator must not exceed 255 characters")
            .MustAsync(BeUniqueIndicator).WithMessage("An indicator with this name already exists")
            .Matches(@"^[a-zA-Z0-9\s\-_\.]+$").WithMessage("Indicator can only contain letters, numbers, spaces, hyphens, underscores, and periods");

        RuleFor(x => x.DescriptionTemplate)
            .NotEmpty().WithMessage("Description template is required")
            .MaximumLength(1000).WithMessage("Description template must not exceed 1000 characters")
            .MustBeValidTemplate("Description");

        RuleFor(x => x.SpName)
            .NotEmpty().WithMessage("Stored procedure name is required")
            .MaximumLength(255).WithMessage("Stored procedure name must not exceed 255 characters")
            .Must(BeValidStoredProcedureName).WithMessage("Stored procedure name contains invalid characters")
            .MustBeSecureStoredProcedureName();

        RuleFor(x => x.Deviation)
            .InclusiveBetween(0, 100).WithMessage("Deviation must be between 0 and 100 percent");

        RuleFor(x => x.Frequency)
            .GreaterThan(0).WithMessage("Frequency must be greater than 0 minutes")
            .LessThanOrEqualTo(10080).WithMessage("Frequency cannot exceed 7 days (10080 minutes)") // 7 days max
            .MustHaveAppropriateFrequencyForPriority(x => x.Priority);

        RuleFor(x => x.Owner)
            .NotEmpty().WithMessage("Owner is required")
            .MaximumLength(100).WithMessage("Owner must not exceed 100 characters")
            .EmailAddress().When(x => x.Owner?.Contains('@') == true).WithMessage("Owner must be a valid email address when containing @");

        RuleFor(x => x.Priority)
            .InclusiveBetween((byte)1, (byte)2).WithMessage("Priority must be 1 (SMS + Email) or 2 (Email Only)");

        RuleFor(x => x.SubjectTemplate)
            .NotEmpty().WithMessage("Subject template is required")
            .MaximumLength(500).WithMessage("Subject template must not exceed 500 characters")
            .MustBeValidTemplate("Subject");

        RuleFor(x => x.CooldownMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Cooldown minutes cannot be negative")
            .LessThanOrEqualTo(10080).WithMessage("Cooldown cannot exceed 7 days (10080 minutes)")
            .MustHaveReasonableCooldownForFrequency(x => x.Frequency);

        RuleFor(x => x.LastMinutes)
            .GreaterThan(0).WithMessage("Data window (LastMinutes) must be greater than 0")
            .LessThanOrEqualTo(43200).WithMessage("Data window cannot exceed 30 days (43200 minutes)")
            .MustHaveAppropriateDataWindowForFrequency(x => x.Frequency);

        RuleFor(x => x.ContactIds)
            .NotEmpty().WithMessage("At least one contact must be assigned")
            .Must(HaveValidContactIds).WithMessage("All contact IDs must be valid");

        // Custom validation for related fields
        RuleFor(x => x)
            .Must(HaveReasonableFrequencyForPriority).WithMessage("Frequency should be appropriate for priority level");


    }

    private async Task<bool> BeUniqueIndicator(string indicator, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(indicator)) return true; // Let NotEmpty handle this

        var kpiRepository = _unitOfWork.Repository<MonitoringGrid.Core.Entities.KPI>();
        return !await kpiRepository.AnyAsync(k => k.Indicator.ToLower() == indicator.ToLower(), cancellationToken);
    }

    private static bool BeValidStoredProcedureName(string? spName)
    {
        if (string.IsNullOrEmpty(spName)) return true; // Let NotEmpty handle this

        // Check for valid stored procedure name format
        return spName.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '.');
    }

    private static bool HaveValidContactIds(List<int>? contactIds)
    {
        if (contactIds == null || !contactIds.Any()) return true; // Let NotEmpty handle this

        // Check for valid contact IDs (positive integers)
        return contactIds.All(id => id > 0);
    }

    private static bool HaveReasonableFrequencyForPriority(CreateKpiRequest request)
    {
        // High priority KPIs (SMS alerts) should have reasonable frequencies
        if (request.Priority == 1 && request.Frequency < 5)
        {
            return false; // SMS alerts shouldn't run more than every 5 minutes
        }

        return true;
    }



    private enum QueryComplexity
    {
        Simple,
        Medium,
        Complex,
        VeryComplex
    }
}

/// <summary>
/// Validator for UpdateKpiRequest
/// </summary>
public class UpdateKpiRequestValidator : AbstractValidator<UpdateKpiRequest>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateKpiRequestValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;

        RuleFor(x => x.Indicator)
            .NotEmpty().WithMessage("Indicator is required")
            .MaximumLength(255).WithMessage("Indicator must not exceed 255 characters")
            .MustAsync(BeUniqueIndicatorForUpdate).WithMessage("An indicator with this name already exists")
            .Matches(@"^[a-zA-Z0-9\s\-_\.]+$").WithMessage("Indicator can only contain letters, numbers, spaces, hyphens, underscores, and periods");

        RuleFor(x => x.DescriptionTemplate)
            .NotEmpty().WithMessage("Description template is required")
            .MaximumLength(1000).WithMessage("Description template must not exceed 1000 characters");

        RuleFor(x => x.SpName)
            .NotEmpty().WithMessage("Stored procedure name is required")
            .MaximumLength(255).WithMessage("Stored procedure name must not exceed 255 characters")
            .Must(BeValidStoredProcedureName).WithMessage("Stored procedure name contains invalid characters")
            .MustBeSecureStoredProcedureName();

        RuleFor(x => x.Deviation)
            .InclusiveBetween(0, 100).WithMessage("Deviation must be between 0 and 100 percent");

        RuleFor(x => x.Frequency)
            .GreaterThan(0).WithMessage("Frequency must be greater than 0 minutes")
            .LessThanOrEqualTo(10080).WithMessage("Frequency cannot exceed 7 days (10080 minutes)")
            .MustHaveAppropriateFrequencyForPriority(x => x.Priority);

        RuleFor(x => x.Owner)
            .NotEmpty().WithMessage("Owner is required")
            .MaximumLength(100).WithMessage("Owner must not exceed 100 characters")
            .EmailAddress().When(x => x.Owner?.Contains('@') == true).WithMessage("Owner must be a valid email address when containing @");

        RuleFor(x => x.Priority)
            .InclusiveBetween((byte)1, (byte)2).WithMessage("Priority must be 1 (SMS + Email) or 2 (Email Only)");

        RuleFor(x => x.SubjectTemplate)
            .NotEmpty().WithMessage("Subject template is required")
            .MaximumLength(500).WithMessage("Subject template must not exceed 500 characters")
            .MustBeValidTemplate("Subject");

        RuleFor(x => x.CooldownMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Cooldown minutes cannot be negative")
            .LessThanOrEqualTo(10080).WithMessage("Cooldown cannot exceed 7 days (10080 minutes)")
            .MustHaveReasonableCooldownForFrequency(x => x.Frequency);
    }

    private async Task<bool> BeUniqueIndicatorForUpdate(UpdateKpiRequest request, string indicator, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(indicator)) return true;

        var kpiRepository = _unitOfWork.Repository<MonitoringGrid.Core.Entities.KPI>();
        var existingKpi = await kpiRepository.GetByIdAsync(request.KpiId, cancellationToken);
        
        if (existingKpi == null) return false; // KPI doesn't exist

        // Allow same indicator for the same KPI
        if (existingKpi.Indicator.Equals(indicator, StringComparison.OrdinalIgnoreCase))
            return true;

        // Check if another KPI has this indicator
        return !await kpiRepository.AnyAsync(k => k.KpiId != request.KpiId && k.Indicator.ToLower() == indicator.ToLower(), cancellationToken);
    }

    private static bool BeValidStoredProcedureName(string? spName)
    {
        if (string.IsNullOrEmpty(spName)) return true;
        return spName.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '.');
    }
}
