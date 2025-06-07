using MonitoringGrid.Api.CQRS.Commands;
using MonitoringGrid.Api.DTOs;
using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Api.CQRS.Commands.Kpi;

/// <summary>
/// Command to create a new KPI
/// </summary>
public class CreateKpiCommand : ICommand<KpiDto>
{
    [Required]
    [StringLength(255)]
    public string Indicator { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Owner { get; set; } = string.Empty;

    [Range(1, 2)]
    public byte Priority { get; set; }

    [Range(1, int.MaxValue)]
    public int Frequency { get; set; }

    [Range(1, int.MaxValue)]
    public int LastMinutes { get; set; } = 1440;

    [Range(0, 100)]
    public decimal Deviation { get; set; }

    [Required]
    [StringLength(255)]
    public string SpName { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string SubjectTemplate { get; set; } = string.Empty;

    [Required]
    public string DescriptionTemplate { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [Range(1, int.MaxValue)]
    public int CooldownMinutes { get; set; } = 30;

    [Range(0, double.MaxValue)]
    public decimal? MinimumThreshold { get; set; }

    public List<int> ContactIds { get; set; } = new();

    public string? KpiType { get; set; }
}
