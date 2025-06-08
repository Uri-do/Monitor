// Enhanced Components - Material-UI Implementation
// These components leverage our enhanced API hooks for advanced monitoring capabilities

// Page-level enhanced components
export { EnhancedDashboard } from './EnhancedDashboard';
export { RealtimeMonitoring } from './RealtimeMonitoring';
export { EnhancedAlertManagement } from './EnhancedAlertManagement';
export { EnhancedKpiManagement } from './EnhancedKpiManagement';

// UI Component Library - Enhanced versions with better styling and UX
// Enhanced components are showcased in ComponentShowcase with inline styled components

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

// Data Visualization Components
export { default as EnhancedChart } from './EnhancedCharts';
export { default as RealtimeDashboard } from './RealtimeDashboard';
export { default as InteractiveVisualization } from './InteractiveVisualizations';
export { default as KpiVisualization } from './KpiVisualization';

// Design tokens for consistent styling
export { designTokens, statusColors } from '../../theme/designTokens';
export type { DesignTokens } from '../../theme/designTokens';
