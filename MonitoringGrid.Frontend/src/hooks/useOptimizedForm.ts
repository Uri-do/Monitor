import { useForm, UseFormProps, FieldValues, DefaultValues } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import { useCallback, useTransition } from 'react';
import { toast } from 'react-hot-toast';
import * as yup from 'yup';

interface OptimizedFormOptions<T extends FieldValues> extends UseFormProps<T> {
  validationSchema?: yup.ObjectSchema<T>;
  onSubmit: (data: T) => Promise<void> | void;
  onSuccess?: (data: T) => void;
  onError?: (error: Error) => void;
  successMessage?: string;
  errorMessage?: string;
  resetOnSuccess?: boolean;
  enableConcurrentFeatures?: boolean;
}

/**
 * Optimized form hook that consolidates all form patterns with performance optimizations
 */
export const useOptimizedForm = <T extends FieldValues>({
  validationSchema,
  onSubmit,
  onSuccess,
  onError,
  successMessage,
  errorMessage,
  resetOnSuccess = false,
  enableConcurrentFeatures = true,
  defaultValues,
  ...formOptions
}: OptimizedFormOptions<T>) => {
  const [isPending, startTransition] = useTransition();

  // Setup form with validation
  const form = useForm<T>({
    resolver: validationSchema ? yupResolver(validationSchema) : undefined,
    defaultValues,
    mode: 'onChange', // Optimized for better UX
    ...formOptions,
  });

  const { handleSubmit, reset, formState } = form;

  // Optimized submit handler with concurrent features
  const handleOptimizedSubmit = useCallback(
    async (data: T) => {
      try {
        if (enableConcurrentFeatures) {
          startTransition(async () => {
            await onSubmit(data);
          });
        } else {
          await onSubmit(data);
        }

        // Success handling
        if (successMessage) {
          toast.success(successMessage);
        }

        if (resetOnSuccess) {
          reset();
        }

        onSuccess?.(data);
      } catch (error) {
        const errorMsg = errorMessage || 
          (error instanceof Error ? error.message : 'An error occurred');
        
        toast.error(errorMsg);
        onError?.(error as Error);
      }
    },
    [onSubmit, successMessage, errorMessage, resetOnSuccess, onSuccess, onError, reset, enableConcurrentFeatures, startTransition]
  );

  // Enhanced form state
  const enhancedFormState = {
    ...formState,
    isPending: enableConcurrentFeatures ? isPending : false,
    isLoading: formState.isSubmitting || (enableConcurrentFeatures ? isPending : false),
  };

  return {
    ...form,
    formState: enhancedFormState,
    handleOptimizedSubmit: handleSubmit(handleOptimizedSubmit),
    resetForm: () => reset(defaultValues),
  };
};

// Specialized hooks for common form patterns
export const useCreateForm = <T extends FieldValues>(
  entityName: string,
  validationSchema: yup.ObjectSchema<T>,
  createFn: (data: T) => Promise<void>,
  defaultValues: DefaultValues<T>,
  options?: Partial<OptimizedFormOptions<T>>
) => {
  return useOptimizedForm({
    validationSchema,
    defaultValues,
    onSubmit: createFn,
    successMessage: `${entityName} created successfully`,
    errorMessage: `Failed to create ${entityName.toLowerCase()}`,
    resetOnSuccess: true,
    ...options,
  });
};

export const useEditForm = <T extends FieldValues>(
  entityName: string,
  validationSchema: yup.ObjectSchema<T>,
  updateFn: (data: T) => Promise<void>,
  initialData: T,
  options?: Partial<OptimizedFormOptions<T>>
) => {
  return useOptimizedForm({
    validationSchema,
    defaultValues: initialData,
    onSubmit: updateFn,
    successMessage: `${entityName} updated successfully`,
    errorMessage: `Failed to update ${entityName.toLowerCase()}`,
    resetOnSuccess: false,
    ...options,
  });
};

export const useSearchForm = <T extends FieldValues>(
  onSearch: (data: T) => void,
  defaultValues: DefaultValues<T>,
  options?: Partial<OptimizedFormOptions<T>>
) => {
  return useOptimizedForm({
    defaultValues,
    onSubmit: onSearch,
    mode: 'onChange',
    enableConcurrentFeatures: true,
    ...options,
  });
};

export default useOptimizedForm;
