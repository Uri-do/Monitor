import { useCallback, useRef, useState, useEffect, useMemo } from 'react';
import { useOptimizedCallback, useOptimizedMemo } from '@/utils/componentOptimization';

/**
 * Advanced State Management Hooks
 * Enterprise-grade state management patterns and utilities
 */

// Advanced state with history and undo/redo functionality
export interface StateHistory<T> {
  past: T[];
  present: T;
  future: T[];
}

export interface UseAdvancedStateReturn<T> {
  state: T;
  setState: (newState: T | ((prev: T) => T)) => void;
  undo: () => void;
  redo: () => void;
  canUndo: boolean;
  canRedo: boolean;
  clearHistory: () => void;
  history: StateHistory<T>;
}

export function useAdvancedState<T>(
  initialState: T,
  maxHistorySize: number = 50
): UseAdvancedStateReturn<T> {
  const [history, setHistory] = useState<StateHistory<T>>({
    past: [],
    present: initialState,
    future: [],
  });

  const setState = useOptimizedCallback((newState: T | ((prev: T) => T)) => {
    setHistory(prev => {
      const nextState = typeof newState === 'function' 
        ? (newState as (prev: T) => T)(prev.present)
        : newState;

      // Don't add to history if state hasn't changed
      if (Object.is(nextState, prev.present)) {
        return prev;
      }

      const newPast = [...prev.past, prev.present];
      
      // Limit history size
      if (newPast.length > maxHistorySize) {
        newPast.shift();
      }

      return {
        past: newPast,
        present: nextState,
        future: [], // Clear future when new state is set
      };
    });
  }, [maxHistorySize]);

  const undo = useOptimizedCallback(() => {
    setHistory(prev => {
      if (prev.past.length === 0) return prev;

      const previous = prev.past[prev.past.length - 1];
      const newPast = prev.past.slice(0, prev.past.length - 1);

      return {
        past: newPast,
        present: previous,
        future: [prev.present, ...prev.future],
      };
    });
  }, []);

  const redo = useOptimizedCallback(() => {
    setHistory(prev => {
      if (prev.future.length === 0) return prev;

      const next = prev.future[0];
      const newFuture = prev.future.slice(1);

      return {
        past: [...prev.past, prev.present],
        present: next,
        future: newFuture,
      };
    });
  }, []);

  const clearHistory = useOptimizedCallback(() => {
    setHistory(prev => ({
      past: [],
      present: prev.present,
      future: [],
    }));
  }, []);

  return {
    state: history.present,
    setState,
    undo,
    redo,
    canUndo: history.past.length > 0,
    canRedo: history.future.length > 0,
    clearHistory,
    history,
  };
}

// Advanced async state with loading, error, and retry functionality
export interface AsyncState<T> {
  data: T | null;
  loading: boolean;
  error: Error | null;
  lastFetch: Date | null;
}

export interface UseAsyncStateReturn<T> {
  state: AsyncState<T>;
  execute: (asyncFn: () => Promise<T>) => Promise<void>;
  retry: () => Promise<void>;
  reset: () => void;
  setData: (data: T) => void;
  setError: (error: Error) => void;
}

export function useAsyncState<T>(
  initialData: T | null = null
): UseAsyncStateReturn<T> {
  const [state, setState] = useState<AsyncState<T>>({
    data: initialData,
    loading: false,
    error: null,
    lastFetch: null,
  });

  const lastAsyncFn = useRef<(() => Promise<T>) | null>(null);

  const execute = useOptimizedCallback(async (asyncFn: () => Promise<T>) => {
    lastAsyncFn.current = asyncFn;
    
    setState(prev => ({
      ...prev,
      loading: true,
      error: null,
    }));

    try {
      const result = await asyncFn();
      setState(prev => ({
        ...prev,
        data: result,
        loading: false,
        lastFetch: new Date(),
      }));
    } catch (error) {
      setState(prev => ({
        ...prev,
        loading: false,
        error: error as Error,
      }));
    }
  }, []);

  const retry = useOptimizedCallback(async () => {
    if (lastAsyncFn.current) {
      await execute(lastAsyncFn.current);
    }
  }, [execute]);

  const reset = useOptimizedCallback(() => {
    setState({
      data: initialData,
      loading: false,
      error: null,
      lastFetch: null,
    });
    lastAsyncFn.current = null;
  }, [initialData]);

  const setData = useOptimizedCallback((data: T) => {
    setState(prev => ({
      ...prev,
      data,
      error: null,
    }));
  }, []);

  const setError = useOptimizedCallback((error: Error) => {
    setState(prev => ({
      ...prev,
      error,
      loading: false,
    }));
  }, []);

  return {
    state,
    execute,
    retry,
    reset,
    setData,
    setError,
  };
}

// Advanced form state with validation and field tracking
export interface FieldState<T> {
  value: T;
  error: string | null;
  touched: boolean;
  dirty: boolean;
}

export interface FormState<T extends Record<string, any>> {
  fields: { [K in keyof T]: FieldState<T[K]> };
  isValid: boolean;
  isDirty: boolean;
  isSubmitting: boolean;
  submitCount: number;
}

export interface UseAdvancedFormReturn<T extends Record<string, any>> {
  formState: FormState<T>;
  setFieldValue: <K extends keyof T>(field: K, value: T[K]) => void;
  setFieldError: <K extends keyof T>(field: K, error: string | null) => void;
  setFieldTouched: <K extends keyof T>(field: K, touched: boolean) => void;
  validateField: <K extends keyof T>(field: K) => void;
  validateForm: () => boolean;
  resetForm: () => void;
  resetField: <K extends keyof T>(field: K) => void;
  handleSubmit: (onSubmit: (values: T) => Promise<void> | void) => Promise<void>;
  getFieldProps: <K extends keyof T>(field: K) => {
    value: T[K];
    onChange: (value: T[K]) => void;
    onBlur: () => void;
    error: string | null;
    touched: boolean;
  };
}

export function useAdvancedForm<T extends Record<string, any>>(
  initialValues: T,
  validators?: { [K in keyof T]?: (value: T[K]) => string | null }
): UseAdvancedFormReturn<T> {
  const [formState, setFormState] = useState<FormState<T>>(() => {
    const fields = {} as { [K in keyof T]: FieldState<T[K]> };
    
    for (const key in initialValues) {
      fields[key] = {
        value: initialValues[key],
        error: null,
        touched: false,
        dirty: false,
      };
    }

    return {
      fields,
      isValid: true,
      isDirty: false,
      isSubmitting: false,
      submitCount: 0,
    };
  });

  const setFieldValue = useOptimizedCallback(<K extends keyof T>(field: K, value: T[K]) => {
    setFormState(prev => {
      const newFields = {
        ...prev.fields,
        [field]: {
          ...prev.fields[field],
          value,
          dirty: !Object.is(value, initialValues[field]),
        },
      };

      return {
        ...prev,
        fields: newFields,
        isDirty: Object.values(newFields).some(f => f.dirty),
      };
    });
  }, [initialValues]);

  const setFieldError = useOptimizedCallback(<K extends keyof T>(field: K, error: string | null) => {
    setFormState(prev => ({
      ...prev,
      fields: {
        ...prev.fields,
        [field]: {
          ...prev.fields[field],
          error,
        },
      },
    }));
  }, []);

  const setFieldTouched = useOptimizedCallback(<K extends keyof T>(field: K, touched: boolean) => {
    setFormState(prev => ({
      ...prev,
      fields: {
        ...prev.fields,
        [field]: {
          ...prev.fields[field],
          touched,
        },
      },
    }));
  }, []);

  const validateField = useOptimizedCallback(<K extends keyof T>(field: K) => {
    if (validators?.[field]) {
      const error = validators[field]!(formState.fields[field].value);
      setFieldError(field, error);
    }
  }, [formState.fields, validators]);

  const validateForm = useOptimizedCallback(() => {
    let isValid = true;
    
    if (validators) {
      for (const field in validators) {
        const validator = validators[field];
        if (validator) {
          const error = validator(formState.fields[field].value);
          setFieldError(field, error);
          if (error) isValid = false;
        }
      }
    }

    setFormState(prev => ({ ...prev, isValid }));
    return isValid;
  }, [formState.fields, validators]);

  const resetForm = useOptimizedCallback(() => {
    setFormState(prev => {
      const fields = {} as { [K in keyof T]: FieldState<T[K]> };
      
      for (const key in initialValues) {
        fields[key] = {
          value: initialValues[key],
          error: null,
          touched: false,
          dirty: false,
        };
      }

      return {
        fields,
        isValid: true,
        isDirty: false,
        isSubmitting: false,
        submitCount: 0,
      };
    });
  }, [initialValues]);

  const resetField = useOptimizedCallback(<K extends keyof T>(field: K) => {
    setFormState(prev => ({
      ...prev,
      fields: {
        ...prev.fields,
        [field]: {
          value: initialValues[field],
          error: null,
          touched: false,
          dirty: false,
        },
      },
    }));
  }, [initialValues]);

  const handleSubmit = useOptimizedCallback(async (onSubmit: (values: T) => Promise<void> | void) => {
    setFormState(prev => ({ ...prev, isSubmitting: true }));

    const isValid = validateForm();
    
    if (isValid) {
      try {
        const values = {} as T;
        for (const key in formState.fields) {
          values[key] = formState.fields[key].value;
        }
        
        await onSubmit(values);
      } catch (error) {
        console.error('Form submission error:', error);
      }
    }

    setFormState(prev => ({
      ...prev,
      isSubmitting: false,
      submitCount: prev.submitCount + 1,
    }));
  }, [formState.fields, validateForm]);

  const getFieldProps = useOptimizedCallback(<K extends keyof T>(field: K) => ({
    value: formState.fields[field].value,
    onChange: (value: T[K]) => setFieldValue(field, value),
    onBlur: () => {
      setFieldTouched(field, true);
      validateField(field);
    },
    error: formState.fields[field].error,
    touched: formState.fields[field].touched,
  }), [formState.fields, setFieldValue, setFieldTouched, validateField]);

  // Update isValid when fields change
  useEffect(() => {
    const hasErrors = Object.values(formState.fields).some(field => field.error !== null);
    setFormState(prev => ({ ...prev, isValid: !hasErrors }));
  }, [formState.fields]);

  return {
    formState,
    setFieldValue,
    setFieldError,
    setFieldTouched,
    validateField,
    validateForm,
    resetForm,
    resetField,
    handleSubmit,
    getFieldProps,
  };
}
