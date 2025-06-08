// Common types for mutation hooks

export interface MutationOptions {
  showSuccessToast?: boolean;
  showErrorToast?: boolean;
  successMessage?: string;
  errorMessage?: string;
  onSuccess?: () => void;
  onError?: (error: Error) => void;
  invalidateQueries?: string[][];
}

export interface MutationResult<TData = any, TError = Error, TVariables = any> {
  mutate: (variables: TVariables) => Promise<TData>;
  mutateAsync: (variables: TVariables) => Promise<TData>;
  isLoading: boolean;
  isError: boolean;
  isSuccess: boolean;
  error: TError | null;
  data: TData | undefined;
  reset: () => void;
}

export interface CreateMutationOptions<TData, TVariables> extends MutationOptions {
  mutationFn: (variables: TVariables) => Promise<TData>;
  mutationKey?: string[];
}

export interface ApiError {
  message: string;
  status?: number;
  code?: string;
  details?: any;
}
