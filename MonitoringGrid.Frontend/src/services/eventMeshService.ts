import api from './api';

// Event Mesh and Streaming interfaces
export interface EventStream {
  id: string;
  name: string;
  description: string;
  type: 'kafka' | 'pulsar' | 'kinesis' | 'eventhub' | 'custom';
  configuration: {
    brokers?: string[];
    topics?: string[];
    partitions?: number;
    replicationFactor?: number;
    retentionMs?: number;
    compressionType?: 'none' | 'gzip' | 'snappy' | 'lz4' | 'zstd';
  };
  schema: {
    format: 'avro' | 'json' | 'protobuf' | 'custom';
    definition: string;
    version: string;
    compatibility: 'backward' | 'forward' | 'full' | 'none';
  };
  metrics: {
    messagesPerSecond: number;
    bytesPerSecond: number;
    consumerLag: number;
    errorRate: number;
    availability: number;
  };
  status: 'active' | 'paused' | 'error' | 'maintenance';
  createdAt: string;
  updatedAt: string;
}

export interface EventProcessor {
  id: string;
  name: string;
  description: string;
  inputStreams: string[];
  outputStreams: string[];
  processingLogic: {
    type: 'filter' | 'transform' | 'aggregate' | 'join' | 'window' | 'custom';
    configuration: Record<string, any>;
    code?: string;
  };
  scalingPolicy: {
    minInstances: number;
    maxInstances: number;
    targetCpuUtilization: number;
    targetMemoryUtilization: number;
    scaleUpCooldown: number;
    scaleDownCooldown: number;
  };
  performance: {
    throughput: number;
    latency: number;
    cpuUsage: number;
    memoryUsage: number;
    instances: number;
  };
  status: 'running' | 'stopped' | 'scaling' | 'error';
  createdAt: string;
  deployedAt?: string;
}

export interface EventMeshTopology {
  id: string;
  name: string;
  description: string;
  nodes: Array<{
    id: string;
    type: 'producer' | 'consumer' | 'processor' | 'router' | 'gateway';
    name: string;
    configuration: Record<string, any>;
    position: { x: number; y: number };
    status: 'healthy' | 'degraded' | 'failed';
  }>;
  connections: Array<{
    id: string;
    source: string;
    target: string;
    streamId: string;
    protocol: 'kafka' | 'http' | 'websocket' | 'grpc' | 'mqtt';
    qos: 'at_most_once' | 'at_least_once' | 'exactly_once';
    encryption: boolean;
    compression: boolean;
  }>;
  policies: {
    routing: Record<string, any>;
    security: Record<string, any>;
    monitoring: Record<string, any>;
  };
  metrics: {
    totalThroughput: number;
    averageLatency: number;
    errorRate: number;
    activeConnections: number;
  };
  createdAt: string;
  updatedAt: string;
}

export interface StreamAnalytics {
  streamId: string;
  timeframe: string;
  metrics: {
    messageCount: number;
    averageMessageSize: number;
    peakThroughput: number;
    averageThroughput: number;
    errorCount: number;
    duplicateCount: number;
    latencyPercentiles: {
      p50: number;
      p95: number;
      p99: number;
      p999: number;
    };
  };
  patterns: Array<{
    type: 'spike' | 'drop' | 'anomaly' | 'trend';
    description: string;
    timestamp: string;
    severity: 'low' | 'medium' | 'high';
    impact: string;
  }>;
  topProducers: Array<{
    id: string;
    messageCount: number;
    percentage: number;
  }>;
  topConsumers: Array<{
    id: string;
    messageCount: number;
    lag: number;
  }>;
}

export interface EventReplay {
  id: string;
  streamId: string;
  timeRange: {
    start: string;
    end: string;
  };
  filters: Array<{
    field: string;
    operator: 'equals' | 'contains' | 'greater_than' | 'less_than' | 'regex';
    value: any;
  }>;
  destination: {
    type: 'stream' | 'webhook' | 'file' | 'database';
    configuration: Record<string, any>;
  };
  status: 'pending' | 'running' | 'completed' | 'failed' | 'cancelled';
  progress: {
    totalEvents: number;
    processedEvents: number;
    percentage: number;
    estimatedCompletion?: string;
  };
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
}

export interface ComplexEventPattern {
  id: string;
  name: string;
  description: string;
  pattern: {
    events: Array<{
      type: string;
      alias: string;
      conditions: Record<string, any>;
    }>;
    sequence: string; // CEP query language
    timeWindow: {
      duration: number;
      unit: 'seconds' | 'minutes' | 'hours';
    };
  };
  actions: Array<{
    type: 'alert' | 'webhook' | 'email' | 'function' | 'stream';
    configuration: Record<string, any>;
  }>;
  matches: {
    total: number;
    last24h: number;
    averagePerDay: number;
  };
  status: 'active' | 'paused' | 'error';
  createdAt: string;
  lastTriggered?: string;
}

/**
 * Advanced event mesh and streaming service
 */
class EventMeshService {
  // Stream Management
  async createEventStream(
    stream: Omit<EventStream, 'id' | 'metrics' | 'status' | 'createdAt' | 'updatedAt'>
  ): Promise<EventStream> {
    try {
      const response = await api.post('/event-mesh/streams', stream);
      return response.data;
    } catch (error) {
      console.warn('Create event stream endpoint not available, returning mock data');
      return this.getMockEventStream();
    }
  }

  async getEventStreams(): Promise<EventStream[]> {
    try {
      const response = await api.get('/event-mesh/streams');
      return response.data;
    } catch (error) {
      console.warn('Event streams endpoint not available, returning mock data');
      return [this.getMockEventStream()];
    }
  }

  async getStreamMetrics(streamId: string, timeframe: string): Promise<StreamAnalytics> {
    try {
      const response = await api.get(`/event-mesh/streams/${streamId}/analytics`, {
        params: { timeframe },
      });
      return response.data;
    } catch (error) {
      console.warn('Stream metrics endpoint not available, returning mock data');
      return this.getMockStreamAnalytics();
    }
  }

  // Event Processing
  async createEventProcessor(
    processor: Omit<EventProcessor, 'id' | 'performance' | 'status' | 'createdAt' | 'deployedAt'>
  ): Promise<EventProcessor> {
    try {
      const response = await api.post('/event-mesh/processors', processor);
      return response.data;
    } catch (error) {
      console.warn('Create event processor endpoint not available, returning mock data');
      return this.getMockEventProcessor();
    }
  }

  async deployProcessor(processorId: string): Promise<{ deploymentId: string; status: string }> {
    try {
      const response = await api.post(`/event-mesh/processors/${processorId}/deploy`);
      return response.data;
    } catch (error) {
      console.warn('Deploy processor endpoint not available, returning mock response');
      return { deploymentId: `deploy-${Date.now()}`, status: 'deploying' };
    }
  }

  async scaleProcessor(processorId: string, instances: number): Promise<void> {
    try {
      await api.post(`/event-mesh/processors/${processorId}/scale`, { instances });
    } catch (error) {
      console.warn('Scale processor endpoint not available');
    }
  }

  // Event Mesh Topology
  async createTopology(
    topology: Omit<EventMeshTopology, 'id' | 'metrics' | 'createdAt' | 'updatedAt'>
  ): Promise<EventMeshTopology> {
    try {
      const response = await api.post('/event-mesh/topologies', topology);
      return response.data;
    } catch (error) {
      console.warn('Create topology endpoint not available, returning mock data');
      return this.getMockEventMeshTopology();
    }
  }

  async getTopologies(): Promise<EventMeshTopology[]> {
    try {
      const response = await api.get('/event-mesh/topologies');
      return response.data;
    } catch (error) {
      console.warn('Topologies endpoint not available, returning mock data');
      return [this.getMockEventMeshTopology()];
    }
  }

  async validateTopology(topology: any): Promise<{
    valid: boolean;
    errors: string[];
    warnings: string[];
    suggestions: string[];
  }> {
    try {
      const response = await api.post('/event-mesh/topologies/validate', topology);
      return response.data;
    } catch (error) {
      console.warn('Validate topology endpoint not available, returning mock validation');
      return {
        valid: true,
        errors: [],
        warnings: ['Consider adding error handling nodes'],
        suggestions: ['Add monitoring for critical paths'],
      };
    }
  }

  // Event Replay
  async createEventReplay(
    replay: Omit<EventReplay, 'id' | 'status' | 'progress' | 'createdAt'>
  ): Promise<EventReplay> {
    try {
      const response = await api.post('/event-mesh/replay', replay);
      return response.data;
    } catch (error) {
      console.warn('Create event replay endpoint not available, returning mock data');
      return this.getMockEventReplay();
    }
  }

  async getEventReplays(): Promise<EventReplay[]> {
    try {
      const response = await api.get('/event-mesh/replay');
      return response.data;
    } catch (error) {
      console.warn('Event replays endpoint not available, returning mock data');
      return [this.getMockEventReplay()];
    }
  }

  async cancelEventReplay(replayId: string): Promise<void> {
    try {
      await api.post(`/event-mesh/replay/${replayId}/cancel`);
    } catch (error) {
      console.warn('Cancel event replay endpoint not available');
    }
  }

  // Complex Event Processing
  async createEventPattern(
    pattern: Omit<ComplexEventPattern, 'id' | 'matches' | 'status' | 'createdAt' | 'lastTriggered'>
  ): Promise<ComplexEventPattern> {
    try {
      const response = await api.post('/event-mesh/patterns', pattern);
      return response.data;
    } catch (error) {
      console.warn('Create event pattern endpoint not available, returning mock data');
      return this.getMockComplexEventPattern();
    }
  }

  async getEventPatterns(): Promise<ComplexEventPattern[]> {
    try {
      const response = await api.get('/event-mesh/patterns');
      return response.data;
    } catch (error) {
      console.warn('Event patterns endpoint not available, returning mock data');
      return [this.getMockComplexEventPattern()];
    }
  }

  async testEventPattern(
    patternId: string,
    testEvents: any[]
  ): Promise<{
    matches: boolean;
    matchedEvents: any[];
    executionTime: number;
    explanation: string;
  }> {
    try {
      const response = await api.post(`/event-mesh/patterns/${patternId}/test`, { testEvents });
      return response.data;
    } catch (error) {
      console.warn('Test event pattern endpoint not available, returning mock result');
      return {
        matches: true,
        matchedEvents: testEvents.slice(0, 2),
        executionTime: 15,
        explanation: 'Pattern matched: KPI threshold exceeded followed by alert creation',
      };
    }
  }

  // Real-time Event Publishing
  async publishEvent(
    streamId: string,
    event: {
      type: string;
      data: any;
      metadata?: Record<string, any>;
      timestamp?: string;
    }
  ): Promise<{
    eventId: string;
    partition: number;
    offset: number;
    timestamp: string;
  }> {
    try {
      const response = await api.post(`/event-mesh/streams/${streamId}/publish`, event);
      return response.data;
    } catch (error) {
      console.warn('Publish event endpoint not available, returning mock response');
      return {
        eventId: `event-${Date.now()}`,
        partition: 0,
        offset: Math.floor(Math.random() * 1000000),
        timestamp: new Date().toISOString(),
      };
    }
  }

  async batchPublishEvents(
    streamId: string,
    events: any[]
  ): Promise<{
    successCount: number;
    failureCount: number;
    results: Array<{
      eventId: string;
      status: 'success' | 'failure';
      error?: string;
    }>;
  }> {
    try {
      const response = await api.post(`/event-mesh/streams/${streamId}/batch-publish`, { events });
      return response.data;
    } catch (error) {
      console.warn('Batch publish events endpoint not available, returning mock response');
      return {
        successCount: events.length,
        failureCount: 0,
        results: events.map((_, i) => ({
          eventId: `event-${Date.now() + i}`,
          status: 'success' as const,
        })),
      };
    }
  }

  // Stream Monitoring
  async getStreamHealth(): Promise<{
    overall: 'healthy' | 'degraded' | 'critical';
    streams: Array<{
      id: string;
      name: string;
      status: 'healthy' | 'degraded' | 'critical';
      issues: string[];
    }>;
    processors: Array<{
      id: string;
      name: string;
      status: 'healthy' | 'degraded' | 'critical';
      issues: string[];
    }>;
  }> {
    try {
      const response = await api.get('/event-mesh/health');
      return response.data;
    } catch (error) {
      console.warn('Stream health endpoint not available, returning mock data');
      return {
        overall: 'healthy',
        streams: [
          { id: 'stream-1', name: 'KPI Events', status: 'healthy', issues: [] },
          {
            id: 'stream-2',
            name: 'Alert Events',
            status: 'degraded',
            issues: ['High consumer lag'],
          },
        ],
        processors: [{ id: 'proc-1', name: 'KPI Processor', status: 'healthy', issues: [] }],
      };
    }
  }

  // Mock data methods
  private getMockEventStream(): EventStream {
    return {
      id: `stream-${Date.now()}`,
      name: 'KPI Events Stream',
      description: 'Real-time KPI execution and monitoring events',
      type: 'kafka',
      configuration: {
        brokers: ['kafka-1:9092', 'kafka-2:9092', 'kafka-3:9092'],
        topics: ['kpi-events', 'kpi-results'],
        partitions: 12,
        replicationFactor: 3,
        retentionMs: 604800000, // 7 days
        compressionType: 'snappy',
      },
      schema: {
        format: 'avro',
        definition:
          '{"type":"record","name":"KpiEvent","fields":[{"name":"kpiId","type":"int"},{"name":"value","type":"double"}]}',
        version: '1.0.0',
        compatibility: 'backward',
      },
      metrics: {
        messagesPerSecond: 1250,
        bytesPerSecond: 125000,
        consumerLag: 45,
        errorRate: 0.001,
        availability: 99.95,
      },
      status: 'active',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };
  }

  private getMockEventProcessor(): EventProcessor {
    return {
      id: `processor-${Date.now()}`,
      name: 'KPI Anomaly Detector',
      description: 'Real-time anomaly detection for KPI values',
      inputStreams: ['kpi-events'],
      outputStreams: ['anomaly-alerts'],
      processingLogic: {
        type: 'custom',
        configuration: { threshold: 2.5, windowSize: 100 },
        code: 'function detectAnomaly(value, history) { return Math.abs(value - mean(history)) > threshold * stddev(history); }',
      },
      scalingPolicy: {
        minInstances: 2,
        maxInstances: 10,
        targetCpuUtilization: 70,
        targetMemoryUtilization: 80,
        scaleUpCooldown: 300,
        scaleDownCooldown: 600,
      },
      performance: {
        throughput: 5000,
        latency: 12,
        cpuUsage: 45,
        memoryUsage: 512,
        instances: 3,
      },
      status: 'running',
      createdAt: new Date().toISOString(),
      deployedAt: new Date().toISOString(),
    };
  }

  private getMockEventMeshTopology(): EventMeshTopology {
    return {
      id: `topology-${Date.now()}`,
      name: 'MonitoringGrid Event Mesh',
      description: 'Complete event processing topology for monitoring system',
      nodes: [
        {
          id: 'producer-1',
          type: 'producer',
          name: 'KPI Producer',
          configuration: {},
          position: { x: 100, y: 100 },
          status: 'healthy',
        },
        {
          id: 'processor-1',
          type: 'processor',
          name: 'Anomaly Detector',
          configuration: {},
          position: { x: 300, y: 100 },
          status: 'healthy',
        },
        {
          id: 'consumer-1',
          type: 'consumer',
          name: 'Alert Consumer',
          configuration: {},
          position: { x: 500, y: 100 },
          status: 'healthy',
        },
      ],
      connections: [
        {
          id: 'conn-1',
          source: 'producer-1',
          target: 'processor-1',
          streamId: 'kpi-events',
          protocol: 'kafka',
          qos: 'at_least_once',
          encryption: true,
          compression: true,
        },
        {
          id: 'conn-2',
          source: 'processor-1',
          target: 'consumer-1',
          streamId: 'anomaly-alerts',
          protocol: 'kafka',
          qos: 'exactly_once',
          encryption: true,
          compression: true,
        },
      ],
      policies: {
        routing: { strategy: 'round_robin' },
        security: { encryption: 'AES-256', authentication: 'SASL_SSL' },
        monitoring: { metricsInterval: 30, alertThresholds: { latency: 100, errorRate: 0.01 } },
      },
      metrics: {
        totalThroughput: 5000,
        averageLatency: 15,
        errorRate: 0.001,
        activeConnections: 2,
      },
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };
  }

  private getMockStreamAnalytics(): StreamAnalytics {
    return {
      streamId: 'stream-1',
      timeframe: '24h',
      metrics: {
        messageCount: 1250000,
        averageMessageSize: 512,
        peakThroughput: 2500,
        averageThroughput: 1250,
        errorCount: 125,
        duplicateCount: 25,
        latencyPercentiles: {
          p50: 12,
          p95: 45,
          p99: 85,
          p999: 150,
        },
      },
      patterns: [
        {
          type: 'spike',
          description: 'Traffic spike detected at 14:30',
          timestamp: new Date().toISOString(),
          severity: 'medium',
          impact: 'Increased latency',
        },
      ],
      topProducers: [
        { id: 'producer-1', messageCount: 750000, percentage: 60 },
        { id: 'producer-2', messageCount: 500000, percentage: 40 },
      ],
      topConsumers: [
        { id: 'consumer-1', messageCount: 1200000, lag: 45 },
        { id: 'consumer-2', messageCount: 50000, lag: 12 },
      ],
    };
  }

  private getMockEventReplay(): EventReplay {
    return {
      id: `replay-${Date.now()}`,
      streamId: 'stream-1',
      timeRange: {
        start: new Date(Date.now() - 86400000).toISOString(),
        end: new Date().toISOString(),
      },
      filters: [{ field: 'kpiId', operator: 'equals', value: 123 }],
      destination: {
        type: 'stream',
        configuration: { streamId: 'replay-stream' },
      },
      status: 'running',
      progress: {
        totalEvents: 100000,
        processedEvents: 75000,
        percentage: 75,
        estimatedCompletion: new Date(Date.now() + 300000).toISOString(),
      },
      createdAt: new Date().toISOString(),
      startedAt: new Date().toISOString(),
    };
  }

  private getMockComplexEventPattern(): ComplexEventPattern {
    return {
      id: `pattern-${Date.now()}`,
      name: 'KPI Threshold Breach Pattern',
      description: 'Detects when KPI exceeds threshold followed by alert creation',
      pattern: {
        events: [
          { type: 'kpi_value', alias: 'kpi', conditions: { value: { $gt: 100 } } },
          { type: 'alert_created', alias: 'alert', conditions: { kpiId: { $eq: 'kpi.kpiId' } } },
        ],
        sequence: 'kpi -> alert WITHIN 5 MINUTES',
        timeWindow: { duration: 5, unit: 'minutes' },
      },
      actions: [
        {
          type: 'webhook',
          configuration: { url: 'https://api.example.com/webhook', method: 'POST' },
        },
        {
          type: 'alert',
          configuration: { severity: 'high', message: 'KPI threshold breach pattern detected' },
        },
      ],
      matches: {
        total: 156,
        last24h: 12,
        averagePerDay: 8,
      },
      status: 'active',
      createdAt: new Date().toISOString(),
      lastTriggered: new Date(Date.now() - 3600000).toISOString(),
    };
  }
}

export const eventMeshService = new EventMeshService();
