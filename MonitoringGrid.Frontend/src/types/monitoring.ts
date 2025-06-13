// Monitoring-specific types for real-time updates

export interface AlertNotification {
  id: string;
  indicatorID: number;
  indicatorName: string;
  message: string;
  severity: 'Low' | 'Medium' | 'High' | 'Critical';
  timestamp: Date;
  currentValue?: number;
  historicalValue?: number;
  deviationPercent?: number;
  isResolved: boolean;
}

export interface IndicatorExecutionResult {
  indicatorID: number;
  key: string;
  currentValue: number;
  historicalValue: number;
  deviationPercent: number;
  shouldAlert: boolean;
  errorMessage?: string;
  executionTime: Date;
}

export interface SystemStatus {
  serviceId: string;
  serviceName: string;
  status: 'Running' | 'Stopped' | 'Error' | 'Warning';
  lastHeartbeat: Date;
  processedIndicators: number;
  alertsSent: number;
  errorMessage?: string;
  isHealthy: boolean;
}

export interface MonitoringEvent {
  id: string;
  type: 'alert' | 'indicator_update' | 'system_status' | 'dashboard_update';
  timestamp: Date;
  data: any;
}

export interface RealtimeMetric {
  id: string;
  name: string;
  value: number;
  trend: 'up' | 'down' | 'stable';
  status: 'healthy' | 'warning' | 'critical';
  lastUpdate: Date;
}

export interface DashboardStats {
  totalIndicators: number;
  activeAlerts: number;
  healthyIndicators: number;
  criticalIndicators: number;
  systemStatus: 'Healthy' | 'Warning' | 'Critical';
  lastUpdate: Date;
}
