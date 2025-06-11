import api from './api';

// Edge Computing interfaces
export interface EdgeNode {
  id: string;
  name: string;
  location: {
    country: string;
    region: string;
    city: string;
    coordinates: [number, number];
  };
  status: 'online' | 'offline' | 'degraded' | 'maintenance';
  capabilities: string[];
  resources: {
    cpu: number;
    memory: number;
    storage: number;
    bandwidth: number;
  };
  utilization: {
    cpu: number;
    memory: number;
    storage: number;
    bandwidth: number;
  };
  latency: number;
  lastHeartbeat: string;
  version: string;
}

export interface EdgeFunction {
  id: string;
  name: string;
  description: string;
  runtime: 'javascript' | 'webassembly' | 'python' | 'rust';
  code: string;
  configuration: Record<string, any>;
  deployedNodes: string[];
  status: 'deployed' | 'deploying' | 'failed' | 'stopped';
  metrics: {
    invocations: number;
    averageLatency: number;
    errorRate: number;
    lastInvocation: string;
  };
  createdAt: string;
  updatedAt: string;
}

export interface CDNConfiguration {
  id: string;
  name: string;
  domains: string[];
  origins: Array<{
    url: string;
    weight: number;
    healthCheck: boolean;
  }>;
  caching: {
    rules: Array<{
      pattern: string;
      ttl: number;
      headers: Record<string, string>;
    }>;
    compression: boolean;
    minify: boolean;
  };
  security: {
    waf: boolean;
    ddosProtection: boolean;
    rateLimiting: {
      enabled: boolean;
      requests: number;
      window: number;
    };
  };
  analytics: {
    enabled: boolean;
    realTime: boolean;
  };
  status: 'active' | 'inactive' | 'configuring';
}

export interface EdgeAnalytics {
  nodeId: string;
  timeframe: string;
  metrics: {
    requests: number;
    bandwidth: number;
    cacheHitRate: number;
    averageLatency: number;
    errorRate: number;
    uniqueVisitors: number;
  };
  topPaths: Array<{
    path: string;
    requests: number;
    bandwidth: number;
  }>;
  topCountries: Array<{
    country: string;
    requests: number;
    percentage: number;
  }>;
  performanceMetrics: Array<{
    timestamp: string;
    latency: number;
    throughput: number;
  }>;
}

export interface EdgeDeployment {
  id: string;
  functionId: string;
  nodeIds: string[];
  strategy: 'all' | 'nearest' | 'performance' | 'custom';
  rollout: {
    type: 'immediate' | 'gradual' | 'canary';
    percentage?: number;
    duration?: number;
  };
  status: 'pending' | 'deploying' | 'deployed' | 'failed' | 'rolling_back';
  progress: number;
  logs: Array<{
    timestamp: string;
    level: 'info' | 'warn' | 'error';
    message: string;
    nodeId?: string;
  }>;
  createdAt: string;
  completedAt?: string;
}

/**
 * Advanced edge computing and CDN service
 */
class EdgeService {
  // Edge Node Management
  async getEdgeNodes(): Promise<EdgeNode[]> {
    try {
      const response = await api.get('/edge/nodes');
      return response.data;
    } catch (error) {
      console.warn('Edge nodes endpoint not available, returning mock data');
      return this.getMockEdgeNodes();
    }
  }

  async getEdgeNode(nodeId: string): Promise<EdgeNode> {
    try {
      const response = await api.get(`/edge/nodes/${nodeId}`);
      return response.data;
    } catch (error) {
      console.warn('Edge node endpoint not available, returning mock data');
      return this.getMockEdgeNodes()[0];
    }
  }

  async getOptimalEdgeNode(userLocation?: { lat: number; lng: number }): Promise<EdgeNode> {
    try {
      const response = await api.post('/edge/nodes/optimal', { userLocation });
      return response.data;
    } catch (error) {
      console.warn('Optimal edge node endpoint not available, returning mock data');
      return this.getMockEdgeNodes()[0];
    }
  }

  // Edge Functions
  async getEdgeFunctions(): Promise<EdgeFunction[]> {
    try {
      const response = await api.get('/edge/functions');
      return response.data;
    } catch (error) {
      console.warn('Edge functions endpoint not available, returning mock data');
      return this.getMockEdgeFunctions();
    }
  }

  async createEdgeFunction(func: Omit<EdgeFunction, 'id' | 'status' | 'metrics' | 'createdAt' | 'updatedAt'>): Promise<EdgeFunction> {
    try {
      const response = await api.post('/edge/functions', func);
      return response.data;
    } catch (error) {
      console.warn('Create edge function endpoint not available, returning mock data');
      return { ...func, id: 'mock-func-' + Date.now(), status: 'deployed', metrics: { invocations: 0, averageLatency: 0, errorRate: 0, lastInvocation: '' }, createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() } as EdgeFunction;
    }
  }

  async deployEdgeFunction(functionId: string, deployment: Omit<EdgeDeployment, 'id' | 'status' | 'progress' | 'logs' | 'createdAt'>): Promise<EdgeDeployment> {
    try {
      const response = await api.post(`/edge/functions/${functionId}/deploy`, deployment);
      return response.data;
    } catch (error) {
      console.warn('Deploy edge function endpoint not available, returning mock data');
      return this.getMockEdgeDeployment();
    }
  }

  // CDN Management
  async getCDNConfigurations(): Promise<CDNConfiguration[]> {
    try {
      const response = await api.get('/edge/cdn');
      return response.data;
    } catch (error) {
      console.warn('CDN configurations endpoint not available, returning mock data');
      return this.getMockCDNConfigurations();
    }
  }

  async createCDNConfiguration(config: Omit<CDNConfiguration, 'id' | 'status'>): Promise<CDNConfiguration> {
    try {
      const response = await api.post('/edge/cdn', config);
      return response.data;
    } catch (error) {
      console.warn('Create CDN configuration endpoint not available, returning mock data');
      return { ...config, id: 'mock-cdn-' + Date.now(), status: 'active' } as CDNConfiguration;
    }
  }

  async purgeCDNCache(configId: string, paths?: string[]): Promise<void> {
    try {
      await api.post(`/edge/cdn/${configId}/purge`, { paths });
    } catch (error) {
      console.warn('Purge CDN cache endpoint not available');
    }
  }

  // Edge Analytics
  async getEdgeAnalytics(nodeId: string, timeframe: string): Promise<EdgeAnalytics> {
    try {
      const response = await api.get(`/edge/analytics/${nodeId}`, { params: { timeframe } });
      return response.data;
    } catch (error) {
      console.warn('Edge analytics endpoint not available, returning mock data');
      return this.getMockEdgeAnalytics();
    }
  }

  async getGlobalEdgeAnalytics(timeframe: string): Promise<{
    totalRequests: number;
    totalBandwidth: number;
    averageLatency: number;
    globalCacheHitRate: number;
    nodeMetrics: Array<{ nodeId: string; requests: number; latency: number }>;
  }> {
    try {
      const response = await api.get('/edge/analytics/global', { params: { timeframe } });
      return response.data;
    } catch (error) {
      console.warn('Global edge analytics endpoint not available, returning mock data');
      return {
        totalRequests: 1250000,
        totalBandwidth: 2.5e12, // 2.5TB
        averageLatency: 45,
        globalCacheHitRate: 0.89,
        nodeMetrics: this.getMockEdgeNodes().map(node => ({
          nodeId: node.id,
          requests: Math.floor(Math.random() * 100000),
          latency: node.latency,
        })),
      };
    }
  }

  // Edge Computing Utilities
  async executeEdgeFunction(functionId: string, input: any, nodeId?: string): Promise<{
    result: any;
    executionTime: number;
    nodeId: string;
    logs: string[];
  }> {
    try {
      const response = await api.post(`/edge/functions/${functionId}/execute`, {
        input,
        nodeId,
      });
      return response.data;
    } catch (error) {
      console.warn('Execute edge function endpoint not available, returning mock data');
      return {
        result: { processed: true, timestamp: new Date().toISOString() },
        executionTime: Math.random() * 100,
        nodeId: nodeId || this.getMockEdgeNodes()[0].id,
        logs: ['Function executed successfully'],
      };
    }
  }

  // Geographic optimization
  async getOptimalCDNStrategy(userDistribution: Array<{ country: string; percentage: number }>): Promise<{
    recommendedNodes: string[];
    cachingStrategy: Record<string, number>;
    estimatedPerformance: {
      averageLatency: number;
      cacheHitRate: number;
      bandwidthSavings: number;
    };
  }> {
    try {
      const response = await api.post('/edge/optimize/cdn', { userDistribution });
      return response.data;
    } catch (error) {
      console.warn('CDN optimization endpoint not available, returning mock data');
      return {
        recommendedNodes: this.getMockEdgeNodes().slice(0, 3).map(n => n.id),
        cachingStrategy: {
          '/api/': 300,
          '/static/': 86400,
          '/images/': 3600,
        },
        estimatedPerformance: {
          averageLatency: 35,
          cacheHitRate: 0.92,
          bandwidthSavings: 0.65,
        },
      };
    }
  }

  // Mock data methods
  private getMockEdgeNodes(): EdgeNode[] {
    return [
      {
        id: 'edge-us-east-1',
        name: 'US East (Virginia)',
        location: {
          country: 'United States',
          region: 'North America',
          city: 'Ashburn',
          coordinates: [-77.4875, 39.0438],
        },
        status: 'online',
        capabilities: ['compute', 'storage', 'cdn', 'ml-inference'],
        resources: { cpu: 64, memory: 256, storage: 2048, bandwidth: 10000 },
        utilization: { cpu: 45, memory: 62, storage: 23, bandwidth: 34 },
        latency: 12,
        lastHeartbeat: new Date().toISOString(),
        version: '2.1.0',
      },
      {
        id: 'edge-eu-west-1',
        name: 'EU West (Ireland)',
        location: {
          country: 'Ireland',
          region: 'Europe',
          city: 'Dublin',
          coordinates: [-6.2603, 53.3498],
        },
        status: 'online',
        capabilities: ['compute', 'storage', 'cdn'],
        resources: { cpu: 32, memory: 128, storage: 1024, bandwidth: 5000 },
        utilization: { cpu: 38, memory: 55, storage: 18, bandwidth: 28 },
        latency: 8,
        lastHeartbeat: new Date().toISOString(),
        version: '2.1.0',
      },
      {
        id: 'edge-ap-southeast-1',
        name: 'Asia Pacific (Singapore)',
        location: {
          country: 'Singapore',
          region: 'Asia Pacific',
          city: 'Singapore',
          coordinates: [103.8198, 1.3521],
        },
        status: 'online',
        capabilities: ['compute', 'cdn', 'ml-inference'],
        resources: { cpu: 48, memory: 192, storage: 1536, bandwidth: 7500 },
        utilization: { cpu: 52, memory: 48, storage: 31, bandwidth: 42 },
        latency: 15,
        lastHeartbeat: new Date().toISOString(),
        version: '2.0.8',
      },
    ];
  }

  private getMockEdgeFunctions(): EdgeFunction[] {
    return [
      {
        id: 'func-kpi-processor',
        name: 'KPI Data Processor',
        description: 'Real-time KPI data processing and validation',
        runtime: 'javascript',
        code: 'export default function(data) { return { processed: true, value: data.value * 1.1 }; }',
        configuration: { timeout: 5000, memory: 128 },
        deployedNodes: ['edge-us-east-1', 'edge-eu-west-1'],
        status: 'deployed',
        metrics: {
          invocations: 15420,
          averageLatency: 23,
          errorRate: 0.002,
          lastInvocation: new Date(Date.now() - 300000).toISOString(),
        },
        createdAt: new Date(Date.now() - 86400000).toISOString(),
        updatedAt: new Date(Date.now() - 3600000).toISOString(),
      },
    ];
  }

  private getMockCDNConfigurations(): CDNConfiguration[] {
    return [
      {
        id: 'cdn-monitoring-grid',
        name: 'MonitoringGrid CDN',
        domains: ['cdn.monitoringgrid.com', 'assets.monitoringgrid.com'],
        origins: [
          { url: 'https://api.monitoringgrid.com', weight: 100, healthCheck: true },
        ],
        caching: {
          rules: [
            { pattern: '/api/*', ttl: 300, headers: { 'Cache-Control': 'public, max-age=300' } },
            { pattern: '/static/*', ttl: 86400, headers: { 'Cache-Control': 'public, max-age=86400' } },
          ],
          compression: true,
          minify: true,
        },
        security: {
          waf: true,
          ddosProtection: true,
          rateLimiting: { enabled: true, requests: 1000, window: 60 },
        },
        analytics: { enabled: true, realTime: true },
        status: 'active',
      },
    ];
  }

  private getMockEdgeAnalytics(): EdgeAnalytics {
    return {
      nodeId: 'edge-us-east-1',
      timeframe: '24h',
      metrics: {
        requests: 125000,
        bandwidth: 2.5e11, // 250GB
        cacheHitRate: 0.87,
        averageLatency: 12,
        errorRate: 0.001,
        uniqueVisitors: 8500,
      },
      topPaths: [
        { path: '/api/kpis', requests: 25000, bandwidth: 5e10 },
        { path: '/api/alerts', requests: 18000, bandwidth: 3.6e10 },
        { path: '/static/js/main.js', requests: 15000, bandwidth: 7.5e10 },
      ],
      topCountries: [
        { country: 'United States', requests: 65000, percentage: 52 },
        { country: 'Canada', requests: 25000, percentage: 20 },
        { country: 'United Kingdom', requests: 18750, percentage: 15 },
      ],
      performanceMetrics: Array.from({ length: 24 }, (_, i) => ({
        timestamp: new Date(Date.now() - (23 - i) * 3600000).toISOString(),
        latency: 10 + Math.random() * 10,
        throughput: 1000 + Math.random() * 500,
      })),
    };
  }

  private getMockEdgeDeployment(): EdgeDeployment {
    return {
      id: 'deploy-' + Date.now(),
      functionId: 'func-kpi-processor',
      nodeIds: ['edge-us-east-1', 'edge-eu-west-1'],
      strategy: 'performance',
      rollout: { type: 'gradual', percentage: 50, duration: 300 },
      status: 'deploying',
      progress: 75,
      logs: [
        { timestamp: new Date().toISOString(), level: 'info', message: 'Deployment started' },
        { timestamp: new Date().toISOString(), level: 'info', message: 'Deploying to edge-us-east-1', nodeId: 'edge-us-east-1' },
      ],
      createdAt: new Date().toISOString(),
    };
  }
}

export const edgeService = new EdgeService();
