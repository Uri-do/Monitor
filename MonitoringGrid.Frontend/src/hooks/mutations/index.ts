// Custom mutation hooks with enhanced error handling and notifications

// Common mutation options and utilities
export type { MutationOptions, MutationResult } from './types';
export { createMutationHook, handleMutationError } from './utils';

// KPI mutations
export {
  useCreateKpi,
  useUpdateKpi,
  useDeleteKpi,
  useExecuteKpi,
  useBulkKpiOperation,
} from './useKpiMutations';

// Alert mutations
export { useResolveAlert, useBulkResolveAlerts, useCreateManualAlert } from './useAlertMutations';

// Contact mutations
export {
  useCreateContact,
  useUpdateContact,
  useDeleteContact,
  useBulkContactOperation,
} from './useContactMutations';

// User mutations
export {
  useCreateUser,
  useUpdateUser,
  useDeleteUser,
  useUpdateUserPassword,
} from './useUserMutations';

// Role mutations
export {
  useCreateRole,
  useUpdateRole,
  useDeleteRole,
  useAssignUserRoles,
} from './useRoleMutations';
