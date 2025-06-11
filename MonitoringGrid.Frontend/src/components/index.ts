// MonitoringGrid Components - Main Export Index
// Organized by category for easy imports and maintenance

// ===== ULTIMATE ENTERPRISE DESIGN SYSTEM =====
// Core design system components - use these for all new development
export {
  UltimateCard,
  UltimateButton,
  UltimateDialog,
  UltimateInputField,
  UltimateSelect,
  UltimateDataTable,
  UltimatePageHeader,
  UltimateStatusChip,
  UltimateLoadingSpinner,
  UltimateMetricCard,
  UltimateSnackbar,
  UltimateFilterPanel,
} from './UltimateEnterprise';

// ===== BUSINESS LOGIC COMPONENTS =====
// Domain-specific components with business logic
export {
  // KPI Management
  KpiFormDialog,
  ExecutionProgressDialog,
  KpiTypeSelector,
  RunningKpisDisplay,
  SchedulerComponent,
  // Contact Management
  ContactFormDialog,
  // Worker Management
  WorkerDashboardCard,
  WorkerManagement,
} from './Business';

// ===== LAYOUT COMPONENTS =====
// Application layout and structure
export { default as Layout } from './Layout/Layout';
export { default as DemoLayout } from './Layout/DemoLayout';

// ===== AUTHENTICATION COMPONENTS =====
// User authentication and authorization
export { default as ProtectedRoute } from './Auth/ProtectedRoute';
export { default as UserMenu } from './Auth/UserMenu';

// ===== CHART COMPONENTS =====
// Data visualization components
export { default as AdvancedChart } from './Charts/AdvancedChart';

// ===== COMMON UTILITIES =====
// Truly reusable utility components
export { ErrorBoundary, withErrorBoundary, ConfirmDialog } from './Common';

// ===== DEMO COMPONENTS =====
// Demo and showcase components
export { default as AdvancedEnhancementDemo } from './Demo/AdvancedEnhancementDemo';
export { default as EnhancementDemo } from './Demo/EnhancementDemo';
export { default as EnterpriseFeaturesDemo } from './Demo/EnterpriseFeaturesDemo';
export { default as NextGenFeaturesDemo } from './Demo/NextGenFeaturesDemo';
export { default as UltimateComponentsDemo } from './Demo/UltimateComponentsDemo';
export { default as UltimateDataTableDemo } from './Demo/UltimateDataTableDemo';
export { default as UltimateEnterpriseDemo } from './Demo/UltimateEnterpriseDemo';

// ===== TYPE EXPORTS =====
// Re-export important types for convenience
export type {
  UltimateDataTableColumn,
  UltimateDataTableProps,
  UltimateCardProps,
  UltimateButtonProps,
  UltimateDialogProps,
  UltimateInputFieldProps,
  UltimateSelectProps,
  UltimateMetricCardProps,
  UltimateSnackbarProps,
} from './UltimateEnterprise';

export type { KpiFormData, ContactFormData, RunningKpi } from './Business';
