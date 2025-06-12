using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Api.CQRS.Commands.Indicator;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Api.Common;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.CQRS.Handlers.Indicator;

/// <summary>
/// Handler for executing indicators
/// </summary>
public class ExecuteIndicatorCommandHandler : IRequestHandler<ExecuteIndicatorCommand, Result<IndicatorExecutionResultDto>>
{
    private readonly IIndicatorExecutionService _indicatorExecutionService;
    private readonly IMapper _mapper;
    private readonly ILogger<ExecuteIndicatorCommandHandler> _logger;

    public ExecuteIndicatorCommandHandler(
        IIndicatorExecutionService indicatorExecutionService,
        IMapper mapper,
        ILogger<ExecuteIndicatorCommandHandler> logger)
    {
        _indicatorExecutionService = indicatorExecutionService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<IndicatorExecutionResultDto>> Handle(ExecuteIndicatorCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Executing indicator {IndicatorId} with context {ExecutionContext}",
                request.IndicatorID, request.ExecutionContext);

            var executionResult = await _indicatorExecutionService.ExecuteIndicatorAsync(
                request.IndicatorID,
                request.ExecutionContext,
                request.SaveResults,
                cancellationToken);

            var resultDto = _mapper.Map<IndicatorExecutionResultDto>(executionResult);

            if (executionResult.WasSuccessful)
            {
                _logger.LogInformation("Successfully executed indicator {IndicatorId}: {IndicatorName}",
                    request.IndicatorID, executionResult.IndicatorName);
                return Result<IndicatorExecutionResultDto>.Success(resultDto);
            }
            else
            {
                _logger.LogWarning("Indicator execution failed for {IndicatorId}: {ErrorMessage}",
                    request.IndicatorID, executionResult.ErrorMessage);
                return Result.Failure<IndicatorExecutionResultDto>("EXECUTION_FAILED", executionResult.ErrorMessage ?? "Execution failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing indicator {IndicatorId}", request.IndicatorID);
            return Result.Failure<IndicatorExecutionResultDto>("EXECUTION_ERROR", $"Failed to execute indicator: {ex.Message}");
        }
    }
}
