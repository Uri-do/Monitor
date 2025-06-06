using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Models;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Enums;

namespace MonitoringGrid.Core.Services;

/// <summary>
/// Interface for KPI analytics service
/// </summary>
public interface IKpiAnalyticsService
{
    Task<KpiTrendAnalysis> GetKpiTrendAsync(int kpiId, int daysBack = 30, CancellationToken cancellationToken = default);
    Task<List<KpiPerformanceMetrics>> GetKpiPerformanceMetricsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<KpiPrediction> PredictKpiValueAsync(int kpiId, int daysAhead = 7, CancellationToken cancellationToken = default);
    Task<List<KpiCorrelationAnalysis>> GetKpiCorrelationsAsync(CancellationToken cancellationToken = default);
    Task<KpiAnomalyDetection> DetectAnomaliesAsync(int kpiId, CancellationToken cancellationToken = default);
    Task<List<KpiSeasonalityAnalysis>> GetSeasonalityAnalysisAsync(int kpiId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of KPI analytics service for basic functionality
/// </summary>
public class KpiAnalyticsService : IKpiAnalyticsService
{
    private readonly IRepository<KPI> _kpiRepository;
    private readonly IRepository<HistoricalData> _historicalRepository;
    private readonly IRepository<AlertLog> _alertRepository;
    private readonly ILogger<KpiAnalyticsService> _logger;

    public KpiAnalyticsService(
        IRepository<KPI> kpiRepository,
        IRepository<HistoricalData> historicalRepository,
        IRepository<AlertLog> alertRepository,
        ILogger<KpiAnalyticsService> logger)
    {
        _kpiRepository = kpiRepository;
        _historicalRepository = historicalRepository;
        _alertRepository = alertRepository;
        _logger = logger;
    }

    public async Task<KpiTrendAnalysis> GetKpiTrendAsync(int kpiId, int daysBack = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Calculating trend analysis for KPI {KpiId} over {DaysBack} days", kpiId, daysBack);

            var startDate = DateTime.UtcNow.AddDays(-daysBack);
            
            var historicalData = (await _historicalRepository.GetAllAsync())
                .Where(h => h.KpiId == kpiId && h.Timestamp >= startDate)
                .OrderBy(h => h.Timestamp)
                .ToList();

            if (!historicalData.Any())
            {
                return new KpiTrendAnalysis
                {
                    KpiId = kpiId,
                    TrendDirection = TrendDirection.Unknown,
                    TrendStrength = 0,
                    Message = "Insufficient data for trend analysis"
                };
            }

            // Simple trend calculation
            var firstValue = historicalData.First().Value;
            var lastValue = historicalData.Last().Value;
            var change = lastValue - firstValue;
            var changePercent = firstValue != 0 ? (change / firstValue) * 100 : 0;

            var direction = change > 0 ? TrendDirection.Increasing : 
                           change < 0 ? TrendDirection.Decreasing : 
                           TrendDirection.Stable;

            return new KpiTrendAnalysis
            {
                KpiId = kpiId,
                AnalysisPeriodDays = daysBack,
                DataPoints = historicalData.Count,
                TrendDirection = direction,
                TrendStrength = Math.Abs((double)changePercent),
                LastValue = lastValue,
                AverageValue = historicalData.Average(h => h.Value),
                MinValue = historicalData.Min(h => h.Value),
                MaxValue = historicalData.Max(h => h.Value),
                Message = $"Trend analysis completed for {historicalData.Count} data points"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating trend analysis for KPI {KpiId}", kpiId);
            throw;
        }
    }

    public async Task<List<KpiPerformanceMetrics>> GetKpiPerformanceMetricsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Calculating performance metrics from {StartDate} to {EndDate}", startDate, endDate);

            var kpis = (await _kpiRepository.GetAllAsync()).Where(k => k.IsActive).ToList();
            var performanceMetrics = new List<KpiPerformanceMetrics>();

            foreach (var kpi in kpis)
            {
                var historicalData = (await _historicalRepository.GetAllAsync())
                    .Where(h => h.KpiId == kpi.KpiId && h.Timestamp >= startDate && h.Timestamp <= endDate)
                    .ToList();

                var alerts = (await _alertRepository.GetAllAsync())
                    .Where(a => a.KpiId == kpi.KpiId && a.TriggerTime >= startDate && a.TriggerTime <= endDate)
                    .ToList();

                var metrics = new KpiPerformanceMetrics
                {
                    KpiId = kpi.KpiId,
                    KpiName = kpi.Indicator,
                    ExecutionCount = historicalData.Count,
                    SuccessRate = historicalData.Count > 0 ? 100.0 : 0.0, // Simplified
                    AverageExecutionTime = 0, // Not available in current model
                    AlertCount = alerts.Count,
                    AverageValue = historicalData.Any() ? historicalData.Average(h => h.Value) : 0,
                    MinValue = historicalData.Any() ? historicalData.Min(h => h.Value) : 0,
                    MaxValue = historicalData.Any() ? historicalData.Max(h => h.Value) : 0
                };

                performanceMetrics.Add(metrics);
            }

            return performanceMetrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating performance metrics");
            throw;
        }
    }

    public async Task<KpiPrediction> PredictKpiValueAsync(int kpiId, int daysAhead = 7, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Predicting KPI value for {KpiId} {DaysAhead} days ahead", kpiId, daysAhead);

            // Simple prediction based on recent trend
            var recentData = (await _historicalRepository.GetAllAsync())
                .Where(h => h.KpiId == kpiId && h.Timestamp >= DateTime.UtcNow.AddDays(-30))
                .OrderBy(h => h.Timestamp)
                .ToList();

            if (recentData.Count < 2)
            {
                return new KpiPrediction
                {
                    KpiId = kpiId,
                    PredictionDate = DateTime.UtcNow.AddDays(daysAhead),
                    PredictedValue = 0,
                    ConfidenceLevel = 0,
                    Method = "Insufficient Data"
                };
            }

            // Simple linear prediction
            var avgValue = recentData.Average(h => h.Value);
            var trend = (recentData.Last().Value - recentData.First().Value) / recentData.Count;
            var predictedValue = avgValue + (trend * daysAhead);

            return new KpiPrediction
            {
                KpiId = kpiId,
                PredictionDate = DateTime.UtcNow.AddDays(daysAhead),
                PredictedValue = predictedValue,
                ConfidenceLevel = 0.7, // Simplified confidence
                Method = "Linear Trend"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting KPI value for {KpiId}", kpiId);
            throw;
        }
    }

    public async Task<List<KpiCorrelationAnalysis>> GetKpiCorrelationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Calculating KPI correlations");

            // Simplified correlation analysis
            var correlations = new List<KpiCorrelationAnalysis>();
            var kpis = (await _kpiRepository.GetAllAsync()).Where(k => k.IsActive).ToList();

            // For now, return empty list - correlation analysis is complex
            return correlations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating KPI correlations");
            throw;
        }
    }

    public async Task<KpiAnomalyDetection> DetectAnomaliesAsync(int kpiId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Detecting anomalies for KPI {KpiId}", kpiId);

            var recentData = (await _historicalRepository.GetAllAsync())
                .Where(h => h.KpiId == kpiId && h.Timestamp >= DateTime.UtcNow.AddDays(-30))
                .OrderBy(h => h.Timestamp)
                .ToList();

            if (recentData.Count < 10)
            {
                return new KpiAnomalyDetection
                {
                    KpiId = kpiId,
                    AnomaliesDetected = 0,
                    Method = "Insufficient Data"
                };
            }

            // Simple anomaly detection using standard deviation
            var mean = recentData.Average(h => (double)h.Value);
            var stdDev = Math.Sqrt(recentData.Average(h => Math.Pow((double)h.Value - mean, 2)));
            var threshold = 2 * stdDev;

            var anomalies = recentData.Where(h => Math.Abs((double)h.Value - mean) > threshold).Count();

            return new KpiAnomalyDetection
            {
                KpiId = kpiId,
                AnomaliesDetected = anomalies,
                Method = "Standard Deviation",
                Threshold = (decimal)threshold
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting anomalies for KPI {KpiId}", kpiId);
            throw;
        }
    }

    public Task<List<KpiSeasonalityAnalysis>> GetSeasonalityAnalysisAsync(int kpiId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Analyzing seasonality for KPI {KpiId}", kpiId);

            // Simplified seasonality analysis
            var seasonalityAnalysis = new List<KpiSeasonalityAnalysis>();

            // For now, return empty list - seasonality analysis is complex
            // TODO: Implement actual seasonality analysis algorithm
            return Task.FromResult(seasonalityAnalysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing seasonality for KPI {KpiId}", kpiId);
            throw;
        }
    }
}
