/**
 * Execution History Types
 * Shared types for execution history components
 */

export interface ExecutionHistoryFilters {
  search?: string;
  startDate?: Date | null;
  endDate?: Date | null;
  status?: 'all' | 'success' | 'failed';
  performanceCategory?: 'all' | 'fast' | 'normal' | 'slow' | 'very slow';
  executedBy?: string;
}

export interface ExecutionHistoryState {
  executions: any[];
  loading: boolean;
  selectedRows: any[];
  filters: ExecutionHistoryFilters;
  pagination: {
    page: number;
    rowsPerPage: number;
    totalCount: number;
  };
  selectedExecution: any | null;
  showDetailView: boolean;
}
