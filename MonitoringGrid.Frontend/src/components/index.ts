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
} from './UI';

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

export type { KpiFormData, ContactFormData, RunningKpi } from './Business';
