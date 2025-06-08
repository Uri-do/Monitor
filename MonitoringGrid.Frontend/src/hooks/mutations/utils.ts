import { useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { CreateMutationOptions, MutationResult, ApiError } from './types';

// Enhanced error handling utility
export const handleMutationError = (error: any): string => {
  if (error?.response?.data?.message) {
    return error.response.data.message;
  }

  if (error?.response?.data?.errors) {
    const errors = error.response.data.errors;
    if (Array.isArray(errors)) {
      return errors.join(', ');
    }
    if (typeof errors === 'object') {
      return Object.values(errors).flat().join(', ');
    }
  }

  if (error?.message) {
    return error.message;
  }

  return 'An unexpected error occurred';
};

// Generic mutation hook creator
export const createMutationHook = <TData, TVariables>(
  options: CreateMutationOptions<TData, TVariables>
) => {
  return (customOptions?: Partial<CreateMutationOptions<TData, TVariables>>) => {
    const queryClient = useQueryClient();
    const mergedOptions = { ...options, ...customOptions };

    const mutation = useMutation({
      mutationFn: mergedOptions.mutationFn,
      mutationKey: mergedOptions.mutationKey,
      onSuccess: (data, variables) => {
        // Show success toast if enabled
        if (mergedOptions.showSuccessToast !== false) {
          toast.success(mergedOptions.successMessage || 'Operation completed successfully');
        }

        // Invalidate specified queries
        if (mergedOptions.invalidateQueries) {
          mergedOptions.invalidateQueries.forEach(queryKey => {
            queryClient.invalidateQueries({ queryKey });
          });
        }

        // Call custom success handler
        mergedOptions.onSuccess?.();
      },
      onError: (error: any) => {
        const errorMessage = handleMutationError(error);

        // Show error toast if enabled
        if (mergedOptions.showErrorToast !== false) {
          toast.error(mergedOptions.errorMessage || errorMessage);
        }

        // Call custom error handler
        mergedOptions.onError?.(error);
      },
    });

    return {
      mutate: mutation.mutate,
      mutateAsync: mutation.mutateAsync,
      isLoading: mutation.isPending,
      isError: mutation.isError,
      isSuccess: mutation.isSuccess,
      error: mutation.error,
      data: mutation.data,
      reset: mutation.reset,
    } as MutationResult<TData, ApiError, TVariables>;
  };
};

// Common mutation configurations
export const defaultMutationOptions = {
  showSuccessToast: true,
  showErrorToast: true,
};

// Retry configuration for mutations
export const retryConfig = {
  retry: (failureCount: number, error: any) => {
    // Don't retry on client errors (4xx)
    if (error?.response?.status >= 400 && error?.response?.status < 500) {
      return false;
    }

    // Retry up to 3 times for server errors
    return failureCount < 3;
  },
  retryDelay: (attemptIndex: number) => Math.min(1000 * 2 ** attemptIndex, 30000),
};
