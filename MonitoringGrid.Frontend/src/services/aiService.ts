import api from './api';

// AI/ML interfaces
export interface PredictionModel {
  id: string;
  name: string;
  type: 'regression' | 'classification' | 'time_series' | 'anomaly_detection';
  status: 'training' | 'ready' | 'error' | 'updating';
  accuracy: number;
  lastTrained: string;
  features: string[];
  targetVariable: string;
  description: string;
  version: string;
}

export interface Prediction {
  id: string;
  modelId: string;
  kpiId: number;
  predictedValue: number;
  confidence: number;
  predictionDate: string;
  actualValue?: number;
  accuracy?: number;
  factors: Array<{
    feature: string;
    importance: number;
    value: any;
  }>;
}

export interface AnomalyDetection {
  id: string;
  kpiId: number;
  timestamp: string;
  value: number;
  expectedValue: number;
  anomalyScore: number;
  severity: 'low' | 'medium' | 'high' | 'critical';
  type: 'point' | 'contextual' | 'collective';
  explanation: string;
  recommendations: string[];
}

export interface TrendAnalysis {
  kpiId: number;
  timeframe: string;
  trend: 'increasing' | 'decreasing' | 'stable' | 'volatile';
  trendStrength: number;
  seasonality: {
    detected: boolean;
    period?: number;
    strength?: number;
  };
  changePoints: Array<{
    timestamp: string;
    significance: number;
    description: string;
  }>;
  forecast: Array<{
    timestamp: string;
    value: number;
    confidence: number;
    upperBound: number;
    lowerBound: number;
  }>;
}

export interface InsightGeneration {
  id: string;
  type: 'correlation' | 'causation' | 'pattern' | 'recommendation';
  title: string;
  description: string;
  confidence: number;
  impact: 'low' | 'medium' | 'high';
  kpis: number[];
  evidence: Array<{
    type: string;
    description: string;
    data: any;
  }>;
  recommendations: string[];
  createdAt: string;
}

export interface ModelTrainingRequest {
  name: string;
  type: 'regression' | 'classification' | 'time_series' | 'anomaly_detection';
  kpiIds: number[];
  features: string[];
  targetVariable: string;
  trainingPeriod: {
    start: string;
    end: string;
  };
  hyperparameters?: Record<string, any>;
}

export interface ModelPerformance {
  modelId: string;
  metrics: {
    accuracy?: number;
    precision?: number;
    recall?: number;
    f1Score?: number;
    mse?: number;
    rmse?: number;
    mae?: number;
    r2Score?: number;
  };
  confusionMatrix?: number[][];
  featureImportance: Array<{
    feature: string;
    importance: number;
  }>;
  validationResults: {
    crossValidationScore: number;
    testSetScore: number;
  };
}

/**
 * Advanced AI/ML service for predictive analytics and insights
 */
class AIService {
  // Model Management
  async getModels(): Promise<PredictionModel[]> {
    try {
      const response = await api.get('/ai/models');
      return response.data;
    } catch (error) {
      console.warn('AI models endpoint not available, returning mock data');
      return this.getMockModels();
    }
  }

  async getModel(modelId: string): Promise<PredictionModel> {
    try {
      const response = await api.get(`/ai/models/${modelId}`);
      return response.data;
    } catch (error) {
      console.warn('AI model endpoint not available, returning mock data');
      return this.getMockModels()[0];
    }
  }

  async trainModel(request: ModelTrainingRequest): Promise<{ modelId: string; status: string }> {
    try {
      const response = await api.post('/ai/models/train', request);
      return response.data;
    } catch (error) {
      console.warn('AI training endpoint not available, returning mock response');
      return { modelId: 'mock-model-' + Date.now(), status: 'training' };
    }
  }

  async deleteModel(modelId: string): Promise<void> {
    try {
      await api.delete(`/ai/models/${modelId}`);
    } catch (error) {
      console.warn('AI model deletion endpoint not available');
    }
  }

  // Predictions
  async getPredictions(filters?: {
    kpiId?: number;
    modelId?: string;
    startDate?: string;
    endDate?: string;
    limit?: number;
  }): Promise<Prediction[]> {
    try {
      const response = await api.get('/ai/predictions', { params: filters });
      return response.data;
    } catch (error) {
      console.warn('AI predictions endpoint not available, returning mock data');
      return this.getMockPredictions();
    }
  }

  async generatePrediction(modelId: string, kpiId: number, inputData: Record<string, any>): Promise<Prediction> {
    try {
      const response = await api.post('/ai/predictions', { modelId, kpiId, inputData });
      return response.data;
    } catch (error) {
      console.warn('AI prediction generation endpoint not available, returning mock data');
      return this.getMockPredictions()[0];
    }
  }

  // Anomaly Detection
  async getAnomalies(filters?: {
    kpiId?: number;
    severity?: string;
    startDate?: string;
    endDate?: string;
    limit?: number;
  }): Promise<AnomalyDetection[]> {
    try {
      const response = await api.get('/ai/anomalies', { params: filters });
      return response.data;
    } catch (error) {
      console.warn('AI anomalies endpoint not available, returning mock data');
      return this.getMockAnomalies();
    }
  }

  async detectAnomalies(kpiId: number, timeframe: string): Promise<AnomalyDetection[]> {
    try {
      const response = await api.post('/ai/anomalies/detect', { kpiId, timeframe });
      return response.data;
    } catch (error) {
      console.warn('AI anomaly detection endpoint not available, returning mock data');
      return this.getMockAnomalies();
    }
  }

  // Trend Analysis
  async getTrendAnalysis(kpiId: number, timeframe: string): Promise<TrendAnalysis> {
    try {
      const response = await api.get(`/ai/trends/${kpiId}`, { params: { timeframe } });
      return response.data;
    } catch (error) {
      console.warn('AI trend analysis endpoint not available, returning mock data');
      return this.getMockTrendAnalysis();
    }
  }

  // Insight Generation
  async getInsights(filters?: {
    type?: string;
    kpiId?: number;
    impact?: string;
    limit?: number;
  }): Promise<InsightGeneration[]> {
    try {
      const response = await api.get('/ai/insights', { params: filters });
      return response.data;
    } catch (error) {
      console.warn('AI insights endpoint not available, returning mock data');
      return this.getMockInsights();
    }
  }

  async generateInsights(kpiIds: number[], timeframe: string): Promise<InsightGeneration[]> {
    try {
      const response = await api.post('/ai/insights/generate', { kpiIds, timeframe });
      return response.data;
    } catch (error) {
      console.warn('AI insight generation endpoint not available, returning mock data');
      return this.getMockInsights();
    }
  }

  // Model Performance
  async getModelPerformance(modelId: string): Promise<ModelPerformance> {
    try {
      const response = await api.get(`/ai/models/${modelId}/performance`);
      return response.data;
    } catch (error) {
      console.warn('AI model performance endpoint not available, returning mock data');
      return this.getMockModelPerformance();
    }
  }

  // Mock data methods for development/fallback
  private getMockModels(): PredictionModel[] {
    return [
      {
        id: 'model-1',
        name: 'KPI Trend Predictor',
        type: 'time_series',
        status: 'ready',
        accuracy: 0.87,
        lastTrained: new Date(Date.now() - 86400000).toISOString(),
        features: ['historical_values', 'day_of_week', 'month', 'external_factors'],
        targetVariable: 'kpi_value',
        description: 'Predicts KPI values based on historical trends and external factors',
        version: '1.2.0',
      },
      {
        id: 'model-2',
        name: 'Anomaly Detector',
        type: 'anomaly_detection',
        status: 'ready',
        accuracy: 0.92,
        lastTrained: new Date(Date.now() - 172800000).toISOString(),
        features: ['value', 'moving_average', 'standard_deviation', 'time_features'],
        targetVariable: 'is_anomaly',
        description: 'Detects anomalous patterns in KPI data',
        version: '2.1.0',
      },
    ];
  }

  private getMockPredictions(): Prediction[] {
    return [
      {
        id: 'pred-1',
        modelId: 'model-1',
        kpiId: 1,
        predictedValue: 85.6,
        confidence: 0.78,
        predictionDate: new Date(Date.now() + 86400000).toISOString(),
        factors: [
          { feature: 'historical_trend', importance: 0.45, value: 'increasing' },
          { feature: 'day_of_week', importance: 0.23, value: 'monday' },
          { feature: 'seasonal_factor', importance: 0.32, value: 1.15 },
        ],
      },
    ];
  }

  private getMockAnomalies(): AnomalyDetection[] {
    return [
      {
        id: 'anomaly-1',
        kpiId: 1,
        timestamp: new Date(Date.now() - 3600000).toISOString(),
        value: 120.5,
        expectedValue: 85.2,
        anomalyScore: 0.89,
        severity: 'high',
        type: 'point',
        explanation: 'Value significantly higher than expected based on historical patterns',
        recommendations: [
          'Investigate potential system issues',
          'Check for data quality problems',
          'Review recent configuration changes',
        ],
      },
    ];
  }

  private getMockTrendAnalysis(): TrendAnalysis {
    return {
      kpiId: 1,
      timeframe: '30d',
      trend: 'increasing',
      trendStrength: 0.75,
      seasonality: {
        detected: true,
        period: 7,
        strength: 0.45,
      },
      changePoints: [
        {
          timestamp: new Date(Date.now() - 1209600000).toISOString(),
          significance: 0.82,
          description: 'Significant increase in trend slope',
        },
      ],
      forecast: Array.from({ length: 7 }, (_, i) => ({
        timestamp: new Date(Date.now() + (i + 1) * 86400000).toISOString(),
        value: 85 + i * 2 + Math.random() * 5,
        confidence: 0.8 - i * 0.05,
        upperBound: 95 + i * 2,
        lowerBound: 75 + i * 2,
      })),
    };
  }

  private getMockInsights(): InsightGeneration[] {
    return [
      {
        id: 'insight-1',
        type: 'correlation',
        title: 'Strong correlation between KPI 1 and KPI 3',
        description: 'Analysis shows a strong positive correlation (r=0.87) between these KPIs over the last 30 days',
        confidence: 0.87,
        impact: 'high',
        kpis: [1, 3],
        evidence: [
          {
            type: 'statistical',
            description: 'Pearson correlation coefficient',
            data: { correlation: 0.87, pValue: 0.001 },
          },
        ],
        recommendations: [
          'Consider using KPI 1 as a leading indicator for KPI 3',
          'Investigate the underlying business process connection',
        ],
        createdAt: new Date().toISOString(),
      },
    ];
  }

  private getMockModelPerformance(): ModelPerformance {
    return {
      modelId: 'model-1',
      metrics: {
        accuracy: 0.87,
        mse: 12.5,
        rmse: 3.54,
        mae: 2.8,
        r2Score: 0.82,
      },
      featureImportance: [
        { feature: 'historical_values', importance: 0.45 },
        { feature: 'seasonal_factor', importance: 0.32 },
        { feature: 'day_of_week', importance: 0.23 },
      ],
      validationResults: {
        crossValidationScore: 0.85,
        testSetScore: 0.87,
      },
    };
  }

  // Advanced ML Pipeline Features
  async createMLPipeline(pipeline: {
    name: string;
    description: string;
    stages: Array<{
      name: string;
      type: 'data_ingestion' | 'preprocessing' | 'feature_engineering' | 'training' | 'validation' | 'deployment';
      configuration: Record<string, any>;
    }>;
    schedule?: string;
    triggers?: string[];
  }): Promise<{ pipelineId: string; status: string }> {
    try {
      const response = await api.post('/ai/pipelines', pipeline);
      return response.data;
    } catch (error) {
      console.warn('ML pipeline creation endpoint not available, returning mock response');
      return { pipelineId: 'pipeline-' + Date.now(), status: 'created' };
    }
  }

  async getMLPipelines(): Promise<Array<{
    id: string;
    name: string;
    status: 'running' | 'completed' | 'failed' | 'paused';
    lastRun: string;
    nextRun?: string;
    accuracy: number;
    stages: number;
  }>> {
    try {
      const response = await api.get('/ai/pipelines');
      return response.data;
    } catch (error) {
      console.warn('ML pipelines endpoint not available, returning mock data');
      return [
        {
          id: 'pipeline-1',
          name: 'KPI Anomaly Detection Pipeline',
          status: 'running',
          lastRun: new Date(Date.now() - 3600000).toISOString(),
          nextRun: new Date(Date.now() + 3600000).toISOString(),
          accuracy: 0.94,
          stages: 6,
        },
      ];
    }
  }

  async runAutoML(config: {
    dataset: string;
    target: string;
    problemType: 'classification' | 'regression' | 'time_series';
    timeLimit: number;
    metricToOptimize: string;
  }): Promise<{
    experimentId: string;
    status: string;
    estimatedCompletion: string;
  }> {
    try {
      const response = await api.post('/ai/automl', config);
      return response.data;
    } catch (error) {
      console.warn('AutoML endpoint not available, returning mock response');
      return {
        experimentId: 'automl-' + Date.now(),
        status: 'running',
        estimatedCompletion: new Date(Date.now() + config.timeLimit * 1000).toISOString(),
      };
    }
  }

  async getFeatureImportance(modelId: string): Promise<Array<{
    feature: string;
    importance: number;
    type: 'numerical' | 'categorical' | 'temporal';
    correlation: number;
    description: string;
  }>> {
    try {
      const response = await api.get(`/ai/models/${modelId}/feature-importance`);
      return response.data;
    } catch (error) {
      console.warn('Feature importance endpoint not available, returning mock data');
      return [
        { feature: 'historical_average', importance: 0.35, type: 'numerical', correlation: 0.82, description: 'Historical average of KPI values' },
        { feature: 'time_of_day', importance: 0.28, type: 'temporal', correlation: 0.65, description: 'Hour of day when KPI was measured' },
        { feature: 'day_of_week', importance: 0.22, type: 'categorical', correlation: 0.58, description: 'Day of the week' },
        { feature: 'seasonal_factor', importance: 0.15, type: 'numerical', correlation: 0.45, description: 'Seasonal adjustment factor' },
      ];
    }
  }

  async explainPrediction(predictionId: string): Promise<{
    prediction: number;
    confidence: number;
    explanation: {
      topFactors: Array<{
        factor: string;
        contribution: number;
        value: any;
        impact: 'positive' | 'negative';
      }>;
      baselineValue: number;
      adjustments: Array<{
        factor: string;
        adjustment: number;
        reason: string;
      }>;
    };
    alternatives: Array<{
      scenario: string;
      prediction: number;
      probability: number;
    }>;
  }> {
    try {
      const response = await api.get(`/ai/predictions/${predictionId}/explain`);
      return response.data;
    } catch (error) {
      console.warn('Prediction explanation endpoint not available, returning mock data');
      return {
        prediction: 85.6,
        confidence: 0.78,
        explanation: {
          topFactors: [
            { factor: 'historical_trend', contribution: 0.45, value: 'increasing', impact: 'positive' },
            { factor: 'seasonal_factor', contribution: 0.32, value: 1.15, impact: 'positive' },
            { factor: 'day_of_week', contribution: -0.12, value: 'monday', impact: 'negative' },
          ],
          baselineValue: 80.0,
          adjustments: [
            { factor: 'trend', adjustment: 4.2, reason: 'Strong upward trend detected' },
            { factor: 'seasonality', adjustment: 2.8, reason: 'Peak season adjustment' },
            { factor: 'volatility', adjustment: -1.4, reason: 'Higher than normal volatility' },
          ],
        },
        alternatives: [
          { scenario: 'pessimistic', prediction: 78.2, probability: 0.15 },
          { scenario: 'optimistic', prediction: 92.1, probability: 0.20 },
        ],
      };
    }
  }

  async performHyperparameterTuning(modelId: string, config: {
    algorithm: 'grid_search' | 'random_search' | 'bayesian_optimization' | 'genetic_algorithm';
    parameters: Record<string, any>;
    maxIterations: number;
    crossValidationFolds: number;
  }): Promise<{
    tuningId: string;
    status: string;
    bestParameters?: Record<string, any>;
    bestScore?: number;
    progress: number;
  }> {
    try {
      const response = await api.post(`/ai/models/${modelId}/tune`, config);
      return response.data;
    } catch (error) {
      console.warn('Hyperparameter tuning endpoint not available, returning mock response');
      return {
        tuningId: 'tune-' + Date.now(),
        status: 'running',
        progress: 25,
      };
    }
  }

  async getModelDrift(modelId: string, timeframe: string): Promise<{
    driftDetected: boolean;
    driftScore: number;
    driftType: 'data_drift' | 'concept_drift' | 'prediction_drift';
    affectedFeatures: string[];
    recommendations: string[];
    timeline: Array<{
      timestamp: string;
      driftScore: number;
      threshold: number;
    }>;
  }> {
    try {
      const response = await api.get(`/ai/models/${modelId}/drift`, { params: { timeframe } });
      return response.data;
    } catch (error) {
      console.warn('Model drift endpoint not available, returning mock data');
      return {
        driftDetected: false,
        driftScore: 0.15,
        driftType: 'data_drift',
        affectedFeatures: [],
        recommendations: ['Continue monitoring', 'No action required'],
        timeline: Array.from({ length: 30 }, (_, i) => ({
          timestamp: new Date(Date.now() - (29 - i) * 86400000).toISOString(),
          driftScore: 0.1 + Math.random() * 0.2,
          threshold: 0.3,
        })),
      };
    }
  }

  async createEnsembleModel(config: {
    name: string;
    baseModels: string[];
    ensembleMethod: 'voting' | 'bagging' | 'boosting' | 'stacking';
    weights?: number[];
    metaLearner?: string;
  }): Promise<{
    ensembleId: string;
    status: string;
    expectedAccuracy: number;
  }> {
    try {
      const response = await api.post('/ai/ensemble', config);
      return response.data;
    } catch (error) {
      console.warn('Ensemble model creation endpoint not available, returning mock response');
      return {
        ensembleId: 'ensemble-' + Date.now(),
        status: 'training',
        expectedAccuracy: 0.91,
      };
    }
  }

  async getRealtimeInference(modelId: string, features: Record<string, any>): Promise<{
    prediction: any;
    confidence: number;
    latency: number;
    modelVersion: string;
    explanation?: any;
  }> {
    try {
      const response = await api.post(`/ai/models/${modelId}/infer`, { features });
      return response.data;
    } catch (error) {
      console.warn('Real-time inference endpoint not available, returning mock data');
      return {
        prediction: 85.6,
        confidence: 0.78,
        latency: 12,
        modelVersion: '1.2.0',
        explanation: { topFeature: 'historical_average', contribution: 0.45 },
      };
    }
  }
}

export const aiService = new AIService();
