using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Models;
using MonitoringGrid.Infrastructure.Data;

namespace MonitoringGrid.Core.Services;

/// <summary>
/// Service for advanced KPI analytics and trending
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
/// Implementation of KPI analytics service
/// </summary>
public class KpiAnalyticsService : IKpiAnalyticsService
{
    private readonly MonitoringContext _context;
    private readonly ILogger<KpiAnalyticsService> _logger;

    public KpiAnalyticsService(MonitoringContext context, ILogger<KpiAnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<KpiTrendAnalysis> GetKpiTrendAsync(int kpiId, int daysBack = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Analyzing trend for KPI {KpiId} over {DaysBack} days", kpiId, daysBack);

            var startDate = DateTime.UtcNow.AddDays(-daysBack);
            
            var historicalData = await _context.HistoricalData
                .Where(h => h.KpiId == kpiId && h.ExecutionTime >= startDate)
                .OrderBy(h => h.ExecutionTime)
                .ToListAsync(cancellationToken);

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

            // Calculate trend using linear regression
            var trendAnalysis = CalculateLinearTrend(historicalData);
            
            // Calculate volatility
            var volatility = CalculateVolatility(historicalData);
            
            // Detect trend changes
            var trendChanges = DetectTrendChanges(historicalData);

            return new KpiTrendAnalysis
            {
                KpiId = kpiId,
                AnalysisPeriodDays = daysBack,
                DataPoints = historicalData.Count,
                TrendDirection = trendAnalysis.Direction,
                TrendStrength = trendAnalysis.Strength,
                Slope = trendAnalysis.Slope,
                RSquared = trendAnalysis.RSquared,
                Volatility = volatility,
                TrendChanges = trendChanges,
                LastValue = historicalData.Last().CurrentValue,
                AverageValue = historicalData.Average(h => h.CurrentValue),
                MinValue = historicalData.Min(h => h.CurrentValue),
                MaxValue = historicalData.Max(h => h.CurrentValue),
                StandardDeviation = CalculateStandardDeviation(historicalData.Select(h => h.CurrentValue)),
                AnalysisDate = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze trend for KPI {KpiId}", kpiId);
            throw;
        }
    }

    public async Task<List<KpiPerformanceMetrics>> GetKpiPerformanceMetricsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Calculating performance metrics from {StartDate} to {EndDate}", startDate, endDate);

            var kpis = await _context.KPIs.Where(k => k.IsActive).ToListAsync(cancellationToken);
            var performanceMetrics = new List<KpiPerformanceMetrics>();

            foreach (var kpi in kpis)
            {
                var historicalData = await _context.HistoricalData
                    .Where(h => h.KpiId == kpi.KpiId && h.ExecutionTime >= startDate && h.ExecutionTime <= endDate)
                    .ToListAsync(cancellationToken);

                var alerts = await _context.AlertLogs
                    .Where(a => a.KpiId == kpi.KpiId && a.TriggerTime >= startDate && a.TriggerTime <= endDate)
                    .ToListAsync(cancellationToken);

                if (historicalData.Any())
                {
                    var metrics = new KpiPerformanceMetrics
                    {
                        KpiId = kpi.KpiId,
                        Indicator = kpi.Indicator,
                        Owner = kpi.Owner,
                        ExecutionCount = historicalData.Count,
                        SuccessfulExecutions = historicalData.Count(h => h.IsSuccessful),
                        FailedExecutions = historicalData.Count(h => !h.IsSuccessful),
                        AlertCount = alerts.Count,
                        AverageExecutionTime = historicalData.Average(h => h.ExecutionTimeMs),
                        AverageValue = historicalData.Where(h => h.IsSuccessful).Average(h => h.CurrentValue),
                        MinValue = historicalData.Where(h => h.IsSuccessful).Min(h => h.CurrentValue),
                        MaxValue = historicalData.Where(h => h.IsSuccessful).Max(h => h.CurrentValue),
                        Reliability = (double)historicalData.Count(h => h.IsSuccessful) / historicalData.Count * 100,
                        AlertRate = alerts.Count > 0 ? (double)alerts.Count / historicalData.Count * 100 : 0,
                        LastExecution = historicalData.Max(h => h.ExecutionTime),
                        AnalysisPeriod = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}"
                    };

                    performanceMetrics.Add(metrics);
                }
            }

            return performanceMetrics.OrderByDescending(m => m.AlertRate).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate performance metrics");
            throw;
        }
    }

    public async Task<KpiPrediction> PredictKpiValueAsync(int kpiId, int daysAhead = 7, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Predicting KPI {KpiId} value {DaysAhead} days ahead", kpiId, daysAhead);

            // Get historical data for prediction (last 90 days)
            var startDate = DateTime.UtcNow.AddDays(-90);
            var historicalData = await _context.HistoricalData
                .Where(h => h.KpiId == kpiId && h.ExecutionTime >= startDate && h.IsSuccessful)
                .OrderBy(h => h.ExecutionTime)
                .ToListAsync(cancellationToken);

            if (historicalData.Count < 10)
            {
                return new KpiPrediction
                {
                    KpiId = kpiId,
                    PredictionDate = DateTime.UtcNow.AddDays(daysAhead),
                    PredictedValue = null,
                    ConfidenceLevel = 0,
                    Message = "Insufficient data for prediction"
                };
            }

            // Simple linear regression prediction
            var prediction = PredictUsingLinearRegression(historicalData, daysAhead);

            return new KpiPrediction
            {
                KpiId = kpiId,
                PredictionDate = DateTime.UtcNow.AddDays(daysAhead),
                PredictedValue = prediction.Value,
                ConfidenceLevel = prediction.Confidence,
                PredictionMethod = "Linear Regression",
                DataPointsUsed = historicalData.Count,
                Message = $"Prediction based on {historicalData.Count} data points"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to predict KPI {KpiId} value", kpiId);
            throw;
        }
    }

    public async Task<List<KpiCorrelationAnalysis>> GetKpiCorrelationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Analyzing KPI correlations");

            var correlations = new List<KpiCorrelationAnalysis>();
            var kpis = await _context.KPIs.Where(k => k.IsActive).ToListAsync(cancellationToken);

            for (int i = 0; i < kpis.Count; i++)
            {
                for (int j = i + 1; j < kpis.Count; j++)
                {
                    var correlation = await CalculateKpiCorrelation(kpis[i], kpis[j], cancellationToken);
                    if (correlation != null)
                    {
                        correlations.Add(correlation);
                    }
                }
            }

            return correlations.OrderByDescending(c => Math.Abs(c.CorrelationCoefficient)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze KPI correlations");
            throw;
        }
    }

    public async Task<KpiAnomalyDetection> DetectAnomaliesAsync(int kpiId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Detecting anomalies for KPI {KpiId}", kpiId);

            var startDate = DateTime.UtcNow.AddDays(-30);
            var historicalData = await _context.HistoricalData
                .Where(h => h.KpiId == kpiId && h.ExecutionTime >= startDate && h.IsSuccessful)
                .OrderBy(h => h.ExecutionTime)
                .ToListAsync(cancellationToken);

            if (historicalData.Count < 10)
            {
                return new KpiAnomalyDetection
                {
                    KpiId = kpiId,
                    AnomaliesDetected = 0,
                    Message = "Insufficient data for anomaly detection"
                };
            }

            var anomalies = DetectAnomaliesUsingZScore(historicalData);

            return new KpiAnomalyDetection
            {
                KpiId = kpiId,
                AnalysisPeriodDays = 30,
                DataPointsAnalyzed = historicalData.Count,
                AnomaliesDetected = anomalies.Count,
                Anomalies = anomalies,
                AnomalyRate = (double)anomalies.Count / historicalData.Count * 100,
                AnalysisDate = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect anomalies for KPI {KpiId}", kpiId);
            throw;
        }
    }

    public async Task<List<KpiSeasonalityAnalysis>> GetSeasonalityAnalysisAsync(int kpiId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Analyzing seasonality for KPI {KpiId}", kpiId);

            // Get data for the last year
            var startDate = DateTime.UtcNow.AddYears(-1);
            var historicalData = await _context.HistoricalData
                .Where(h => h.KpiId == kpiId && h.ExecutionTime >= startDate && h.IsSuccessful)
                .OrderBy(h => h.ExecutionTime)
                .ToListAsync(cancellationToken);

            if (historicalData.Count < 50)
            {
                return new List<KpiSeasonalityAnalysis>
                {
                    new KpiSeasonalityAnalysis
                    {
                        KpiId = kpiId,
                        Pattern = "Insufficient Data",
                        Strength = 0,
                        Message = "Insufficient data for seasonality analysis"
                    }
                };
            }

            return AnalyzeSeasonalPatterns(kpiId, historicalData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze seasonality for KPI {KpiId}", kpiId);
            throw;
        }
    }

    // Private helper methods for calculations
    private TrendCalculation CalculateLinearTrend(List<HistoricalData> data)
    {
        // Implementation of linear regression
        var n = data.Count;
        var sumX = 0.0;
        var sumY = 0.0;
        var sumXY = 0.0;
        var sumXX = 0.0;

        for (int i = 0; i < n; i++)
        {
            var x = i;
            var y = (double)data[i].CurrentValue;
            
            sumX += x;
            sumY += y;
            sumXY += x * y;
            sumXX += x * x;
        }

        var slope = (n * sumXY - sumX * sumY) / (n * sumXX - sumX * sumX);
        var intercept = (sumY - slope * sumX) / n;

        // Calculate R-squared
        var meanY = sumY / n;
        var ssTotal = data.Sum(d => Math.Pow((double)d.CurrentValue - meanY, 2));
        var ssResidual = data.Select((d, i) => Math.Pow((double)d.CurrentValue - (slope * i + intercept), 2)).Sum();
        var rSquared = 1 - (ssResidual / ssTotal);

        var direction = slope > 0.1 ? TrendDirection.Increasing :
                       slope < -0.1 ? TrendDirection.Decreasing :
                       TrendDirection.Stable;

        return new TrendCalculation
        {
            Direction = direction,
            Strength = Math.Abs(slope),
            Slope = slope,
            RSquared = rSquared
        };
    }

    private double CalculateVolatility(List<HistoricalData> data)
    {
        if (data.Count < 2) return 0;

        var values = data.Select(d => (double)d.CurrentValue).ToList();
        var mean = values.Average();
        var variance = values.Sum(v => Math.Pow(v - mean, 2)) / (values.Count - 1);
        return Math.Sqrt(variance);
    }

    private double CalculateStandardDeviation(IEnumerable<decimal> values)
    {
        var doubleValues = values.Select(v => (double)v).ToList();
        var mean = doubleValues.Average();
        var variance = doubleValues.Sum(v => Math.Pow(v - mean, 2)) / doubleValues.Count;
        return Math.Sqrt(variance);
    }

    private List<TrendChange> DetectTrendChanges(List<HistoricalData> data)
    {
        // Implementation for detecting trend changes
        return new List<TrendChange>();
    }

    private PredictionResult PredictUsingLinearRegression(List<HistoricalData> data, int daysAhead)
    {
        var trend = CalculateLinearTrend(data);
        var predictedValue = (decimal)(trend.Slope * (data.Count + daysAhead) + data.Average(d => (double)d.CurrentValue));
        var confidence = Math.Min(trend.RSquared * 100, 95); // Cap at 95%

        return new PredictionResult
        {
            Value = predictedValue,
            Confidence = confidence
        };
    }

    private async Task<KpiCorrelationAnalysis?> CalculateKpiCorrelation(KPI kpi1, KPI kpi2, CancellationToken cancellationToken)
    {
        // Implementation for calculating correlation between two KPIs
        return null; // Placeholder
    }

    private List<AnomalyPoint> DetectAnomaliesUsingZScore(List<HistoricalData> data)
    {
        var anomalies = new List<AnomalyPoint>();
        var values = data.Select(d => (double)d.CurrentValue).ToList();
        var mean = values.Average();
        var stdDev = CalculateStandardDeviation(data.Select(d => d.CurrentValue));

        for (int i = 0; i < data.Count; i++)
        {
            var zScore = Math.Abs(((double)data[i].CurrentValue - mean) / (double)stdDev);
            if (zScore > 2.5) // Threshold for anomaly
            {
                anomalies.Add(new AnomalyPoint
                {
                    Timestamp = data[i].ExecutionTime,
                    Value = data[i].CurrentValue,
                    ZScore = zScore,
                    Severity = zScore > 3 ? "High" : "Medium"
                });
            }
        }

        return anomalies;
    }

    private List<KpiSeasonalityAnalysis> AnalyzeSeasonalPatterns(int kpiId, List<HistoricalData> data)
    {
        // Implementation for seasonality analysis
        return new List<KpiSeasonalityAnalysis>();
    }
}

// Supporting classes for analytics
public class TrendCalculation
{
    public TrendDirection Direction { get; set; }
    public double Strength { get; set; }
    public double Slope { get; set; }
    public double RSquared { get; set; }
}

public class PredictionResult
{
    public decimal Value { get; set; }
    public double Confidence { get; set; }
}
