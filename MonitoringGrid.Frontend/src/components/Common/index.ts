// Common Utility Components
// These are truly reusable components without business logic

// Error boundary utility
export { ErrorBoundary, withErrorBoundary } from './ErrorBoundary';

// Legacy ConfirmDialog - will be removed after migration to Dialog
export { default as ConfirmDialog } from './ConfirmDialog';

// Lazy loading wrapper
export { default as LazyWrapper } from './LazyWrapper';

// Virtualized data table
export { default as VirtualizedDataTable } from './VirtualizedDataTable';
