namespace MonitoringGrid.Core.Models;

/// <summary>
/// KPI trend analysis result
/// </summary>
public class KpiTrendAnalysis
{
    public int KpiId { get; set; }
    public int AnalysisPeriodDays { get; set; }
    public int DataPoints { get; set; }
    public TrendDirection TrendDirection { get; set; }
    public double TrendStrength { get; set; }
    public double Slope { get; set; }
    public double RSquared { get; set; }
    public decimal Volatility { get; set; }
    public List<TrendChange> TrendChanges { get; set; } = new();
    public decimal LastValue { get; set; }
    public decimal AverageValue { get; set; }
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public decimal StandardDeviation { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Trend direction enumeration
/// </summary>
public enum TrendDirection
{
    Unknown = 0,
    Increasing = 1,
    Decreasing = 2,
    Stable = 3,
    Volatile = 4
}

/// <summary>
/// Trend change point
/// </summary>
public class TrendChange
{
    public DateTime Timestamp { get; set; }
    public TrendDirection FromDirection { get; set; }
    public TrendDirection ToDirection { get; set; }
    public decimal Magnitude { get; set; }
}

/// <summary>
/// KPI performance metrics
/// </summary>
public class KpiPerformanceMetrics
{
    public int KpiId { get; set; }
    public string KpiName { get; set; } = string.Empty;
    public int ExecutionCount { get; set; }
    public double SuccessRate { get; set; }
    public double AverageExecutionTime { get; set; }
    public int AlertCount { get; set; }
    public decimal AverageValue { get; set; }
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public decimal StandardDeviation { get; set; }
    public DateTime LastExecution { get; set; }
}

/// <summary>
/// KPI prediction result
/// </summary>
public class KpiPrediction
{
    public int KpiId { get; set; }
    public DateTime PredictionDate { get; set; }
    public decimal PredictedValue { get; set; }
    public double ConfidenceLevel { get; set; }
    public string Method { get; set; } = string.Empty;
    public decimal UpperBound { get; set; }
    public decimal LowerBound { get; set; }
    public List<PredictionDataPoint> PredictionSeries { get; set; } = new();
}

/// <summary>
/// Prediction data point
/// </summary>
public class PredictionDataPoint
{
    public DateTime Timestamp { get; set; }
    public decimal PredictedValue { get; set; }
    public decimal UpperBound { get; set; }
    public decimal LowerBound { get; set; }
}

/// <summary>
/// KPI correlation analysis
/// </summary>
public class KpiCorrelationAnalysis
{
    public int KpiId1 { get; set; }
    public int KpiId2 { get; set; }
    public string KpiName1 { get; set; } = string.Empty;
    public string KpiName2 { get; set; } = string.Empty;
    public double CorrelationCoefficient { get; set; }
    public CorrelationStrength Strength { get; set; }
    public int DataPoints { get; set; }
    public double PValue { get; set; }
    public bool IsSignificant { get; set; }
}

/// <summary>
/// Correlation strength enumeration
/// </summary>
public enum CorrelationStrength
{
    None = 0,
    Weak = 1,
    Moderate = 2,
    Strong = 3,
    VeryStrong = 4
}

/// <summary>
/// KPI anomaly detection result
/// </summary>
public class KpiAnomalyDetection
{
    public int KpiId { get; set; }
    public int AnomaliesDetected { get; set; }
    public string Method { get; set; } = string.Empty;
    public decimal Threshold { get; set; }
    public List<AnomalyPoint> Anomalies { get; set; } = new();
    public double AnomalyScore { get; set; }
    public DateTime AnalysisDate { get; set; }
}

/// <summary>
/// Anomaly point
/// </summary>
public class AnomalyPoint
{
    public DateTime Timestamp { get; set; }
    public decimal Value { get; set; }
    public decimal ExpectedValue { get; set; }
    public double AnomalyScore { get; set; }
    public AnomalyType Type { get; set; }
}

/// <summary>
/// Anomaly type enumeration
/// </summary>
public enum AnomalyType
{
    Outlier = 1,
    Spike = 2,
    Drop = 3,
    Trend = 4,
    Seasonal = 5
}

/// <summary>
/// KPI seasonality analysis
/// </summary>
public class KpiSeasonalityAnalysis
{
    public int KpiId { get; set; }
    public SeasonalityType Type { get; set; }
    public double Strength { get; set; }
    public int Period { get; set; }
    public List<SeasonalComponent> Components { get; set; } = new();
    public bool HasSeasonality { get; set; }
    public double ConfidenceLevel { get; set; }
}

/// <summary>
/// Seasonality type enumeration
/// </summary>
public enum SeasonalityType
{
    None = 0,
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
    Quarterly = 4,
    Yearly = 5
}

/// <summary>
/// Seasonal component
/// </summary>
public class SeasonalComponent
{
    public string Name { get; set; } = string.Empty;
    public double Amplitude { get; set; }
    public double Phase { get; set; }
    public int Period { get; set; }
}

/// <summary>
/// KPI data point for charts and analysis
/// </summary>
public class KpiDataPoint
{
    public DateTime Timestamp { get; set; }
    public decimal Value { get; set; }
    public bool IsAnomaly { get; set; }
    public decimal? PredictedValue { get; set; }
    public decimal? UpperBound { get; set; }
    public decimal? LowerBound { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Statistical summary
/// </summary>
public class StatisticalSummary
{
    public decimal Mean { get; set; }
    public decimal Median { get; set; }
    public decimal Mode { get; set; }
    public decimal StandardDeviation { get; set; }
    public decimal Variance { get; set; }
    public decimal Skewness { get; set; }
    public decimal Kurtosis { get; set; }
    public decimal Min { get; set; }
    public decimal Max { get; set; }
    public decimal Range { get; set; }
    public decimal Q1 { get; set; }
    public decimal Q3 { get; set; }
    public decimal IQR { get; set; }
}

/// <summary>
/// Time series decomposition
/// </summary>
public class TimeSeriesDecomposition
{
    public List<KpiDataPoint> Original { get; set; } = new();
    public List<KpiDataPoint> Trend { get; set; } = new();
    public List<KpiDataPoint> Seasonal { get; set; } = new();
    public List<KpiDataPoint> Residual { get; set; } = new();
    public DecompositionMethod Method { get; set; }
}

/// <summary>
/// Decomposition method enumeration
/// </summary>
public enum DecompositionMethod
{
    Additive = 1,
    Multiplicative = 2,
    STL = 3,
    X13 = 4
}

/// <summary>
/// Forecast result
/// </summary>
public class ForecastResult
{
    public int KpiId { get; set; }
    public List<PredictionDataPoint> Forecast { get; set; } = new();
    public string Method { get; set; } = string.Empty;
    public double Accuracy { get; set; }
    public Dictionary<string, double> Metrics { get; set; } = new();
    public DateTime ForecastDate { get; set; }
    public int HorizonDays { get; set; }
}

/// <summary>
/// Model performance metrics
/// </summary>
public class ModelPerformance
{
    public string ModelName { get; set; } = string.Empty;
    public double MAE { get; set; } // Mean Absolute Error
    public double RMSE { get; set; } // Root Mean Square Error
    public double MAPE { get; set; } // Mean Absolute Percentage Error
    public double R2 { get; set; } // R-squared
    public double AIC { get; set; } // Akaike Information Criterion
    public double BIC { get; set; } // Bayesian Information Criterion
    public DateTime EvaluationDate { get; set; }
}
