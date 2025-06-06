// Enhanced Components - Material-UI Implementation
// These components leverage our enhanced API hooks for advanced monitoring capabilities

export { EnhancedDashboard } from './EnhancedDashboard';
export { RealtimeMonitoring } from './RealtimeMonitoring';
export { EnhancedAlertManagement } from './EnhancedAlertManagement';
export { EnhancedKpiManagement } from './EnhancedKpiManagement';

// Re-export enhanced hooks for convenience
export {
  useSystemAnalytics,
  useKpiPerformanceAnalytics,
  useOwnerAnalytics,
  useSystemHealth,
  useRealtimeStatus,
  useLiveDashboard,
  useCriticalAlerts,
  useUnresolvedAlerts,
  useEnhancedAlertStatistics,
  useRealtimeKpiExecution,
  useManualAlert
} from '@/hooks/useEnhancedApi';

export { useKpis } from '@/hooks/useKpis';
export { useAlerts } from '@/hooks/useAlerts';
export { useSignalR } from '@/services/signalRService';
