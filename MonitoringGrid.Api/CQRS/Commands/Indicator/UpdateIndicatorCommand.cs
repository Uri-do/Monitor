using System.ComponentModel.DataAnnotations;
using MediatR;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Api.DTOs.Indicators;

namespace MonitoringGrid.Api.CQRS.Commands.Indicator;

/// <summary>
/// Command to update an existing indicator
/// </summary>
public class UpdateIndicatorCommand : IRequest<Result<IndicatorResponse>>
{
    public int IndicatorID { get; set; }

    [Required]
    [MaxLength(255)]
    public string IndicatorName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string IndicatorCode { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? IndicatorDesc { get; set; }

    [Required]
    public int CollectorID { get; set; }

    [Required]
    [MaxLength(255)]
    public string CollectorItemName { get; set; } = string.Empty;

    public int? SchedulerID { get; set; }

    public bool IsActive { get; set; } = true;

    [Range(1, int.MaxValue)]
    public int LastMinutes { get; set; } = 60;

    [Required]
    [MaxLength(50)]
    public string ThresholdType { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ThresholdField { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string ThresholdComparison { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal ThresholdValue { get; set; }

    [Required]
    [MaxLength(20)]
    public string Priority { get; set; } = "medium";

    [Required]
    public int OwnerContactId { get; set; }

    public int? AverageLastDays { get; set; }

    public List<int> ContactIds { get; set; } = new();
}
