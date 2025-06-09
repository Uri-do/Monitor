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
  useManualAlert,
} from '@/hooks/useEnhancedApi';

export { useKpis } from '@/hooks/useKpis';
export { useAlerts } from '@/hooks/useAlerts';
export { useSignalR } from '@/services/signalRService';

// Design tokens for consistent styling
export { designTokens, statusColors } from '../../theme/designTokens';
export type { DesignTokens } from '../../theme/designTokens';
