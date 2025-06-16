// MonitoringGrid Components - Main Export Index
// Organized by category for easy imports and maintenance

// ===== UI DESIGN SYSTEM =====
// Core design system components - clean, professional UI components
export {
  Button,
  Card,
  Dialog,
  InputField,
  Select,
  DataTable,
  PageHeader,
  StatusChip,
  LoadingSpinner,
  MetricCard,
  Snackbar,
  FilterPanel,
  FormLayout,
  FormSection,
  FormActions,
  InfoItem,
  ConfirmationDialog,
} from './UI';

// ===== BUSINESS LOGIC COMPONENTS =====
// Domain-specific components with business logic
export {
  // Indicator Management
  IndicatorFormDialog,
  ExecutionProgressDialog,
  IndicatorTypeSelector,
  RunningIndicatorsDisplay,
  // Contact Management
  ContactFormDialog,
  // Worker Management
  WorkerDashboardCard,
} from './Business';

// ===== LAYOUT COMPONENTS =====
// Application layout and structure
export { default as Layout } from './Layout/Layout';
// DemoLayout removed - using main Layout component

// ===== AUTHENTICATION COMPONENTS =====
// User authentication and authorization
export { default as ProtectedRoute } from './Auth/ProtectedRoute';
export { default as UserMenu } from './Auth/UserMenu';

// ===== CHART COMPONENTS =====
// Data visualization components
export { default as Chart } from './Charts/Chart';

// ===== MONITORING COMPONENTS =====
// Monitor statistics and collector components
export { CollectorSelector } from './CollectorSelector';
export { default as StatisticsBrowser } from './StatisticsBrowser';
export { default as StatisticsBrowserButton } from './StatisticsBrowserButton';
export { default as StatsExplorer } from './StatsExplorer';

// ===== COMMON UTILITIES =====
// Truly reusable utility components
export { ErrorBoundary, withErrorBoundary, ConfirmDialog } from './Common';

// ===== TYPE EXPORTS =====
// Re-export important types for convenience
export type {
  CustomButtonProps as ButtonProps,
  CustomCardProps as CardProps,
  CustomDialogProps as DialogProps,
  CustomInputFieldProps as InputFieldProps,
  CustomSelectProps as SelectProps,
  DataTableProps,
  DataTableColumn,
} from './UI';

export type { IndicatorFormData, ContactFormData, RunningIndicator } from './Business';
